using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace GameLogic
{
    
    public class UnityServer : UnityNetworkMember
    {
        struct PlayerData
        {
            public bool Filled => Connection != default && Connection.IsCreated;
            public NetworkConnection Connection;
            public string PlayerName;
            public int ClientID;

            public PlayerData(NetworkConnection connection, int clientID, string playerName)
            {
                if (playerName.Length > 1000) throw new ArgumentException("Player name above character limit");
                PlayerName = playerName;
                Connection = connection;
                ClientID = clientID;
            }
        }
        
        const int MaxConnections = 8;
        const int Timeout = 600;
        private int _maxConnections;
        NativeList<NetworkConnection> _connections;
        NativeList<int> _connectionTimeouts;
        private NetworkDriver _networkDriver;
        private bool[] _connectionWaitingOnConfirmation;
        private NetworkConnection _resyncingTarget;
        private Queue<GameEntity> _ongoingResync;
        private Queue<NetworkConnection> needsSync = new();
        private HashSet<NetworkConnection> _approvedConnections = new();
        private IReadOnlyCollection<NetworkConnection> ApprovedConnections => _approvedConnections;
        private PlayerData[] PlayerSlots;
        public int NumApprovedConnections => ApprovedConnections.Count;
        public HashSet<int> FilledPlayerSlots = new();
        public override bool WaitingOnReply => _ongoingResync != null;

        public int PlayerSlot(NetworkConnection connection)
        {
            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (PlayerSlots[i].Connection == connection) return i;
            }

            return -1;
        }
        private ushort _port;
        private string _password;
        protected override NetworkDriver NetworkDriver => _networkDriver;

        protected override IReadOnlyCollection<NetworkConnection> Connections
        {
            get
            {
                NetworkConnection[] connections = new NetworkConnection[_connections.Length];
                for (int i = 0; i < _connections.Length; i++) connections[i] = _connections[i];
                return connections;
            }
        }

        public bool IsPlayerSyncedOrBeingResynced(int iPlayer)
        {
            if (!FilledPlayerSlots.Contains(iPlayer)) return false;
            NetworkConnection connection = PlayerSlots[iPlayer].Connection;
            if (!_approvedConnections.Contains(connection)) return false;
            if (needsSync.Contains(connection)) return false;
            return true;
        }

        
        public UnityServer(GameState gameState, ushort port = NetProtocol.DefaultPort, string password = "") : base (gameState)
        {
            _maxConnections = MaxConnections;
            _connections = new NativeList<NetworkConnection>(initialCapacity:_maxConnections, allocator:Allocator.Persistent);
            _connectionTimeouts = new NativeList<int>(initialCapacity:_maxConnections, allocator:Allocator.Persistent);
            _connectionWaitingOnConfirmation = new bool[_maxConnections];
            _ongoingResync = new Queue<GameEntity>();
            _resyncingTarget = default;
            NetworkSettings networkSettings = new NetworkSettings();
            networkSettings.WithNetworkConfigParameters(
                heartbeatTimeoutMS:int.MaxValue, // No timeout
                reconnectionTimeoutMS:int.MaxValue // No timeout
            );
            _networkDriver = NetworkDriver.Create(networkSettings);
            _port = port;
            _password = password;

        }
        
        public void SetNumberOfPlayerSlots(int slots) => PlayerSlots = new PlayerData[slots];

        public void Start()
        {
            NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4.WithPort(_port);
            if (_networkDriver.Bind(endpoint) != 0)
            {
                Debug.LogError($"Failed to bind to port {_port}.");
                return;
            }
            _networkDriver.Listen();
            _networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        public override void Dispose()
        {
            NetworkingLog("Disposing");
            for (int i = 0; i < _connections.Length; i++)
            {
                if (_connections[i].IsCreated)
                    _connections[i].Disconnect(_networkDriver);
            }
            

            if (_networkDriver.IsCreated)
            {
                _connections.Dispose();
                _connectionTimeouts.Dispose();
                _networkDriver.Dispose();
            }

            if (Controller.ActiveServer == this) Controller.ActiveServer = null;
            Disposed = true;
        }

        private List<NetworkConnection> _closeQueue = new List<NetworkConnection>();
        private int syncCheckTimer = 300;
        protected override void Monitor()
        {
            _networkDriver.ScheduleUpdate().Complete();

            NetworkConnection newConnection;
            while ((newConnection = _networkDriver.Accept()) != default)
            {
                _connections.Add(newConnection);
                _connectionTimeouts.Add(Timeout);
                NetworkingLog($"Unapproved client {newConnection} attempting connection");
            }

            if ((_resyncingTarget == default || !_resyncingTarget.IsCreated) && needsSync.Count > 0)
            {
                NetworkConnection connection = needsSync.Dequeue();
                if (_connections.Contains(connection))
                {
                    _resyncingTarget = connection;
                    int iPlayer = PlayerSlot(connection);
                    //if (iPlayer == -1) CloseConnection(connection);
                    _ongoingResync = GameState.StartSync(iPlayer);
                }
            }
            
            NetworkEvent.Type networkEventType;
            DataStreamReader incomingMessage;
            
            for (int i = 0; i < _connections.Length; i++)
            {
                NetworkConnection connection = _connections[i];
                if (_connectionTimeouts[i] > 0)
                {
                    _connectionTimeouts[i] -= 1;
                    if (_connectionTimeouts[i] == 0)
                    {
                        _closeQueue.Add(connection);
                        NetworkingLog($"Connection {connection} timed out - disconnecting");
                    }
                }
                bool isApproved = _approvedConnections.Contains(connection);
                bool disconnectRecieved = false;
                while ((networkEventType = _networkDriver.PopEventForConnection(connection, out incomingMessage)) != NetworkEvent.Type.Empty)
                {
                    //NetworkingLog($"{GetType().Name} {networkEventType} event received");
                    switch (networkEventType)
                    {
                        case NetworkEvent.Type.Connect:
                            NetworkingLog($"received connect event", DebuggingLevel.StatusChanges);
                            break;
                        case NetworkEvent.Type.Disconnect:
                            disconnectRecieved = true;
                            break;
                        case NetworkEvent.Type.Data:
                            if (isApproved) RouteIncomingData(ref incomingMessage, i);
                            else HandleUnapprovedData(connection, ref incomingMessage, i);
                            break;
                    }
                }

                if (disconnectRecieved)
                {
                    CloseConnection(i);
                    NetworkingLog($"Player #{i} disconnected", DebuggingLevel.StatusChanges);
                }
            }
            
            GameState.RunRecalculations();

            while ((networkEventType = _networkDriver.PopEvent(out NetworkConnection unknownConnection, out incomingMessage)) !=
                   NetworkEvent.Type.Empty)
            {
                NetworkingLog("Handling message from unknown sender");
            }

            // Clean up connections
            /* I dont think this is the way to do this. This method was used in the example when _connections was a List
            for (int i = 0; i < _connections.Length; i++)
            {
                if (!_connections[i].IsCreated)
                {
                    CloseConnection(i);
                }
            }
            */

            syncCheckTimer -= 1;
            if (syncCheckTimer < 0)
            {
                syncCheckTimer = 300;
                for (int i = 0; i < _connections.Length; i++)
                {
                    NetworkConnection connection = _connections[i];
                    if (!connection.IsCreated)
                    {
                        _closeQueue.Add(connection);
                        continue;
                    }

                    _connectionTimeouts[i] = Timeout;
                    try
                    {
                        _networkDriver.BeginSend(connection, out DataStreamWriter message);
                        message.WriteByte(NetProtocol.SyncCheck);
                        _networkDriver.EndSend(message);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Unable to send ping to {connection}. Closing incomplete connection");
                        _closeQueue.Add(connection);
                    }
                }

                if (_closeQueue.Count > 0)
                {
                    foreach (var connection in _closeQueue)
                    {
                        CloseConnection(connection);
                    }
                    _closeQueue.Clear();
                }
            }
        }

        void CloseConnection(int iConnection)
        {
            NetworkConnection connection = _connections[iConnection];
            NetworkingLog($"Closing connection {connection}");
            int playerSlot = PlayerSlot(connection);
            if (playerSlot != -1)
            {
                PlayerSlots[playerSlot] = default;
                FilledPlayerSlots.Remove(playerSlot);
            }
            _approvedConnections.Remove(connection);
            if (_resyncingTarget == connection)
            {
                _resyncingTarget = default;
                _ongoingResync = null;
            }
            
            connection.Close(_networkDriver);
            //_connectionWaitingOnConfirmation[playerSlot] = false;
            _connections.RemoveAtSwapBack(iConnection);
            _connectionTimeouts.RemoveAtSwapBack(iConnection);
            ResendPlayerSlotAssignments();
        }
        void CloseConnection(NetworkConnection connection)
        {
            for (int iConnection = 0; iConnection < _connections.Length; iConnection++)
            {
                NetworkConnection c = _connections[iConnection];
                if (c == connection)
                {
                    CloseConnection(iConnection);
                }
            }
        }

        void HandleUnapprovedData(NetworkConnection unapprovedConnection, ref DataStreamReader incomingMessage, int i)
        {
            (int assignedPlayerSlot, byte denialCode, string playerName, int clientID) = ValidateNewConnection(ref incomingMessage);
            if (assignedPlayerSlot >= 0)
            {
                NetworkingLog($"approving connection into {assignedPlayerSlot}", DebuggingLevel.StatusChanges);
                _networkDriver.BeginSend(unapprovedConnection, out var outgoingMessage);
                outgoingMessage.WriteByte(NetProtocol.ConnectionApprovedHeader);
                outgoingMessage.WriteInt(assignedPlayerSlot);
                outgoingMessage.WriteInt(clientID);
                _networkDriver.EndSend(outgoingMessage);
                _approvedConnections.Add(unapprovedConnection);
                PlayerSlots[assignedPlayerSlot] = new PlayerData(unapprovedConnection, clientID, playerName);
                FilledPlayerSlots.Add(assignedPlayerSlot);
                if (GameStarted) StartGameForClient(unapprovedConnection);
                ResendPlayerSlotAssignments();
            }
            else
            {
                _networkDriver.BeginSend(unapprovedConnection, out var outgoingMessage);
                outgoingMessage.WriteByte(NetProtocol.ConnectionDeniedHeader);
                outgoingMessage.WriteByte(denialCode);
                _networkDriver.EndSend(outgoingMessage);
                CloseConnection(i);
            }
        }

        public void RouteIncomingData(ref DataStreamReader incomingMessage, int source)
        {
            byte routingHeader = incomingMessage.ReadByte();
            int playerSlot = PlayerSlot(_connections[source]);
            switch (routingHeader)
            {
                case NetProtocol.SyncCheckResponse:
                    _connectionTimeouts[source] = -1; // Disable the timeout for the connection (until the next sync check)
                    bool clientThinksItIsInSync = incomingMessage.ReadByte() == 1;
                    int clientHash = incomingMessage.ReadInt();
                    int expectedHash = GameState.GetStateHash(playerSlot, "ServerHashLog.txt");
                    if (!clientThinksItIsInSync || clientHash != expectedHash)
                    {
                        NetworkingLog($"Client {source} is out of sync ({clientHash}/{expectedHash}). Resyncing...", DebuggingLevel.Events);
                        Debug.LogWarning("Out of sync event triggered");
                        QueueSync(_connections[source]);
                    }
                    break;
                case NetProtocol.PlayerActionRoutingHeader:
                    byte playerActionTypeID = incomingMessage.ReadByte();
                    int playerActionCallbackID = incomingMessage.ReadInt();
                    PlayerAction playerAction =
                        PlayerAction.RecreatePlayerActionServerside(
                            this.GameState, 
                            playerActionTypeID, 
                            iPlayerFaction:playerSlot, 
                            incomingMessage);
                    (bool success, string reason) actionValidation = playerAction.Validate();
                    if (actionValidation.success)
                    {
                        playerAction.Execute();
                        SendPlayerActionCallback(_connections[source], callbackHeader:playerActionCallbackID, actionSuccess:true);
                    }
                    else
                    {
                        Debug.LogWarning($"Attempted action is not valid\nAction type = {playerAction.GetType().Name}\nReason = {actionValidation.reason}");
                        SendPlayerActionCallback(_connections[source], callbackHeader:playerActionCallbackID, actionSuccess:false, actionValidation.reason);
                    }
                    
                    break;
                case NetProtocol.MessagesHandledHeader:
                    if (_resyncingTarget != default)
                    {
                        NetworkingLog($"Sending sync packet to {_resyncingTarget}", DebuggingLevel.IndividualMessages);
                        ContinueResync(_resyncingTarget);
                    }
                    break;
                case NetProtocol.RequestPlayerSlotHeader:
                    int desiredSlot = incomingMessage.ReadInt();
                    if (!PlayerSlots[desiredSlot].Filled)
                    {
                        NetworkingLog($"Assigning {_connections[source]} to player slot {desiredSlot}");
                        AssignPlayer(iConnection:source, playerSlot:desiredSlot);
                    }
                    else
                    {
                        NetworkingLog($"Cannot assign {_connections[source]} to player slot {desiredSlot}");
                    }
                    break;
            }
        }

        public void ContinueResync(NetworkConnection connection)
        {
            int iPlayer = PlayerSlot(connection);
            NetworkingLog($"Sending resync packet to player {iPlayer}", DebuggingLevel.IndividualMessages);
            for (int i = 0; i < 25; i++)
            {
                if (_ongoingResync.TryDequeue(out GameEntity entity))
                {
                    entity.PushFullState(iPlayer);
                }
                else
                {
                    _connectionWaitingOnConfirmation[iPlayer] = false;
                    _ongoingResync = null;
                    _resyncingTarget = default;
                    CreateNewGameStateMessage(iPlayer, out DataStreamWriter endResyncMessage);
                    endResyncMessage.WriteByte(GameLogic.GameState.EndResyncHeader);
                    PushMessage(ref endResyncMessage, iPlayer);
                    NetworkingLog($"Server finished resync to {iPlayer}--------------------!!!");
                    break;
                }
            }
        }
        
        void SendPlayerActionCallback(NetworkConnection connection, int callbackHeader, bool actionSuccess, string failureReason = null)
        {
            _networkDriver.BeginSend(connection, out DataStreamWriter outgoingMessage);
            outgoingMessage.WriteByte(NetProtocol.PlayerActionReplyHeader);
            outgoingMessage.WriteInt(callbackHeader);
            outgoingMessage.WriteByte((byte)(actionSuccess == true ? 1 : 0));
            if (!actionSuccess) outgoingMessage.WriteFixedString128(failureReason);
            _networkDriver.EndSend(outgoingMessage);
        }

        public void CreateNewGameStateMessage(int targetPlayer, out DataStreamWriter writer)
        {
            NetworkingLog("Sending game state packet", DebuggingLevel.IndividualMessages);
            NetworkConnection targetPlayerConnection = PlayerSlots[targetPlayer].Connection;
            if (targetPlayerConnection == default)
            {
                Debug.LogError("Target player slot is not filled");
                writer = new DataStreamWriter();
            }

            _networkDriver.BeginSend(targetPlayerConnection, out writer);
            try
            {
                writer.WriteByte(NetProtocol.GameStateTargetingRoutingHeader);
            }
            catch (Exception e)
            {
                NetworkingLog($"Error sending game state packet to {targetPlayer}");
            }
        }

        public void PushMessage(ref DataStreamWriter outgoingMessage, int targetPlayer)
        {
            _connectionWaitingOnConfirmation[targetPlayer] = true;
            _networkDriver.EndSend(outgoingMessage);
        }

        private int _clientIDTicker = 1; // Starts at 1 to avoid overlap with 0 as the default value (just me being safe. It probably doesn't matter)
        public (int playerSlow, byte denialCode, string playerName, int clientID) ValidateNewConnection(ref DataStreamReader initiationMessage)
        {
            int hashedPassword = HashPassword(_password);
            
            int sentPassword = initiationMessage.ReadInt();
            int typesHash = initiationMessage.ReadInt();
            if (sentPassword != hashedPassword)
            {
                NetworkingLog("Server: Denying connection - wrong password");
                return (-1, NetProtocol.DenialCode_WrongPassword, null, -1);
            }
            if (typesHash != GameState.TypesHash)
            {
                NetworkingLog("Server: Denying connection - type map mismatch. This probably indicates a version mismatch");
            }

            int clientID = initiationMessage.ReadInt();
            string playerName = initiationMessage.ReadFixedString512().ToString();


            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (!PlayerSlots[i].Filled)
                {
                    NetworkingLog($"Server: Approving connection");
                    if (clientID == -1)
                    {
                        clientID = _clientIDTicker;
                        _clientIDTicker++;
                    }
                    return (i, 0, playerName, clientID); // Assign to the first available
                }
            }

            NetworkingLog("Server: Denying connection - no available player slots");
            return (-1, NetProtocol.DenialCode_UnavailablePlayerSlot, playerName, clientID);
        }

        void QueueSync(NetworkConnection connection)
        {
            if (!needsSync.Contains(connection)) needsSync.Enqueue(connection);
        }
        void StartGameForClient(NetworkConnection connection)
        {
            _networkDriver.BeginSend(connection, out DataStreamWriter outgoingMessage);
            outgoingMessage.WriteByte(NetProtocol.StartGameHeader);
            outgoingMessage.WriteFixedString64(GameState.MapName);
            outgoingMessage.WriteFixedString64(GameState.RulesetName);
            _networkDriver.EndSend(outgoingMessage);
            QueueSync(connection);
        }
        public void StartGame()
        {
            if (GameState.MapName == null) throw new InvalidOperationException("No map set for the gamestate");
            if (GameState.RulesetName == null) throw new InvalidOperationException("No ruleset set for the gamestate");
            GameState.StartGame();
            foreach (NetworkConnection connection in _approvedConnections)
            {
                StartGameForClient(connection);
            }
            NetworkingLog("Started Game");
            GameStarted = true;

            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (!PlayerSlots[i].Filled) AddBot("Passive", i);
            }
        }

        public void AddBot(string botType, int playerSlot)
        {
            UnityClient botClient = new UnityClient(Controller.GetFirstAvailablePort(), botType: botType);
            Controller.LocalClients.Add(botClient);
            botClient.Connect("127.0.0.1", _port, _password, playerSlot, "AI");
            Debug.Log($"Adding bot to slot {playerSlot}");
        }

        void ResendPlayerSlotAssignments()
        {
            for (int iConnection = 0; iConnection < _connections.Length; iConnection++)
            {
                _networkDriver.BeginSend(_connections[iConnection], out var message);
                message.WriteByte(NetProtocol.NotifyPlayerSlotHeader);
                message.WriteUShort((ushort)PlayerSlots.Length);
                for (int iPlayerSlot = 0; iPlayerSlot < PlayerSlots.Length; iPlayerSlot++)
                {
                    if (!PlayerSlots[iPlayerSlot].Filled)
                    {
                        message.WriteByte(0);
                        // 1 reserved (ai vs totally open)
                    }
                    else
                    {
                        message.WriteByte(2); // Filled
                        message.WriteInt(PlayerSlots[iPlayerSlot].ClientID);
                        message.WriteFixedString512(PlayerSlots[iPlayerSlot].PlayerName);
                    }
                    
                }
                _networkDriver.EndSend(message);
            }
        }
        
        void AssignPlayer(int iConnection, int playerSlot)
        {
            PlayerData prevPlayerData = default;
            NetworkConnection connection = _connections[iConnection];
            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (PlayerSlots[i].Connection == connection)
                {
                    prevPlayerData = new PlayerData(connection:PlayerSlots[i].Connection, clientID:PlayerSlots[i].ClientID, playerName:PlayerSlots[i].PlayerName);
                    FilledPlayerSlots.Remove(i);
                    PlayerSlots[i] = default;
                }
            }

            FilledPlayerSlots.Add(playerSlot);
            PlayerSlots[playerSlot] = new PlayerData(connection:connection, clientID:prevPlayerData.ClientID, playerName:prevPlayerData.PlayerName);
            ResendPlayerSlotAssignments();
        }
    }
}