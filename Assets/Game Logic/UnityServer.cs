using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using GameBoard;
using Izzy;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine.Assertions;
using UnityEngine;

namespace GameLogic
{
    
    public class UnityServer : UnityNetworkMember
    {
        const int MaxConnections = 8;
        private int _maxConnections;
        NativeList<NetworkConnection> _connections;
        private NetworkDriver _networkDriver;
        private bool[] _connectionWaitingOnConfirmation;
        private NetworkConnection _resyncingTarget;
        private Queue<GameEntity> _ongoingResync;
        private Queue<NetworkConnection> needsSync = new();
        private HashSet<NetworkConnection> _approvedConnections = new();
        public IReadOnlyCollection<NetworkConnection> ApprovedConnections => _approvedConnections;
        public NetworkConnection[] PlayerSlots;
        public HashSet<int> FilledPlayerSlots = new();
        public override bool WaitingOnReply => _ongoingResync != null;

        public int PlayerSlot(NetworkConnection connection)
        {
            for (int i = 0; i < PlayerSlots.Length; i++)
            {
                if (PlayerSlots[i] == connection) return i;
            }

            return -1;
        }
        private ushort _port;
        private string _password;
        public bool GameStarted = false;
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
            NetworkConnection connection = PlayerSlots[iPlayer];
            if (!_approvedConnections.Contains(connection)) return false;
            if (needsSync.Contains(connection)) return false;
            return true;
        }

        
        public UnityServer(GameState gameState, ushort port = NetProtocol.DefaultPort, string password = "") : base (gameState)
        {
            _maxConnections = MaxConnections;
            _connections = new NativeList<NetworkConnection>(_maxConnections, Allocator.Persistent);
            _connectionWaitingOnConfirmation = new bool[_maxConnections];
            PlayerSlots = new NetworkConnection[gameState.PlayerCount];
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
                _networkDriver.Dispose();
            }
        }

        private int syncCheckTimer = 300;
        protected override void Monitor()
        {
            _networkDriver.ScheduleUpdate().Complete();

            NetworkConnection newConnection;
            while ((newConnection = _networkDriver.Accept()) != default)
            {
                _connections.Add(newConnection);
                // TODO Add a timeout system so that new connections that fail to provide follow up data are dropped
            }

            if (_resyncingTarget == default && needsSync.Count > 0)
            {
                NetworkConnection connection = needsSync.Dequeue();
                _resyncingTarget = connection;
                int iConnection = PlayerSlot(connection);
                _ongoingResync = GameState.StartSync(iConnection);
            }
            
            NetworkEvent.Type networkEventType;
            DataStreamReader incomingMessage;
            
            for (int i = 0; i < _connections.Length; i++)
            {
                NetworkConnection connection = _connections[i];
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
                    _approvedConnections.Remove(connection);
                    if (_resyncingTarget == connection)
                    {
                        _ongoingResync = null;
                        _resyncingTarget = default;
                    }

                    // Not sure if I need to do this
                    CloseConnection(i);
                    NetworkingLog($"Player #{i} disconnected", DebuggingLevel.StatusChanges);
                }
            }

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
                foreach (var connection in _approvedConnections)
                {
                    _networkDriver.BeginSend(connection, out DataStreamWriter message);
                    message.WriteByte(NetProtocol.SyncCheck);
                    _networkDriver.EndSend(message);
                }
            }
        }

        void CloseConnection(int iConnection)
        {
            NetworkConnection connection = _connections[iConnection];
            int playerSlot = PlayerSlot(connection);
            connection.Close(_networkDriver);
            //_connectionWaitingOnConfirmation[playerSlot] = false;
            _connections.RemoveAtSwapBack(iConnection);
        }
        
        void HandleUnapprovedData(NetworkConnection unapprovedConnection, ref DataStreamReader incomingMessage, int i)
        {
            (int assignedPlayerSlot, byte denialCode) = ValidateNewConnection(ref incomingMessage);
            if (assignedPlayerSlot >= 0)
            {
                NetworkingLog($"approving connection into {assignedPlayerSlot}", DebuggingLevel.StatusChanges);
                _networkDriver.BeginSend(unapprovedConnection, out var outgoingMessage);
                outgoingMessage.WriteByte(NetProtocol.ConnectionApprovedHeader);
                outgoingMessage.WriteInt(assignedPlayerSlot);
                _networkDriver.EndSend(outgoingMessage);
                _approvedConnections.Add(unapprovedConnection);
                PlayerSlots[assignedPlayerSlot] = unapprovedConnection;
                FilledPlayerSlots.Add(assignedPlayerSlot);
                if (GameStarted) StartGameForClient(unapprovedConnection);
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
            switch (routingHeader)
            {
                case NetProtocol.SyncCheckResponse:
                    int playerSlot = PlayerSlot(_connections[source]);
                    int clientHash = incomingMessage.ReadInt();
                    int expectedHash = GameState.GetStateHash(playerSlot, "ServerHashLog.txt");
                    if (clientHash != expectedHash)
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
                            iPlayerFaction:source, 
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
            }
        }

        public void ContinueResync(NetworkConnection connection)
        {
            int iPlayer = PlayerSlot(connection);
            for (int i = 0; i < 25; i++)
            {
                if (_ongoingResync.TryDequeue(out GameEntity entity))
                {
                    entity.RecalculateDerivedValuesAndPushFullState(iPlayer);
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
            NetworkConnection targetPlayerConnection = PlayerSlots[targetPlayer];
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
                throw e;
            }
        }

        public void PushMessage(ref DataStreamWriter outgoingMessage, int targetPlayer)
        {
            _connectionWaitingOnConfirmation[targetPlayer] = true;
            _networkDriver.EndSend(outgoingMessage);
        }

        public (int, byte) ValidateNewConnection(ref DataStreamReader initiationMessage)
        {
            int hashedPassword = HashPassword(_password);
            int typesHash = initiationMessage.ReadInt();
            int sentPassword = initiationMessage.ReadInt();
            byte desiredPlayerSlot = initiationMessage.ReadByte();
            if (typesHash != GameState.TypesHash)
            {
                NetworkingLog("Server: Denying connection - type map mismatch. This probably indicates a version mismatch");
            }
            if (sentPassword != hashedPassword)
            {
                NetworkingLog("Server: Denying connection - wrong password");
                return (-1, NetProtocol.DenialCode_WrongPassword);
            }

            if (desiredPlayerSlot == byte.MaxValue)
            {
                for (int i = 0; i < PlayerSlots.Length; i++)
                {
                    if (PlayerSlots[i] == default)
                    {
                        return (i, 0); // Assign to the first available
                    }
                }

                NetworkingLog("Server: Denying connection - no available player slots");
                return (-1, NetProtocol.DenialCode_UnavailablePlayerSlot);
            }
            else
            {
                if (PlayerSlots[desiredPlayerSlot] == default)
                {
                    return (desiredPlayerSlot, 0);
                }
                else
                {
                    NetworkingLog("Server: Denying connection - desired player slot not available");
                    return (-1, NetProtocol.DenialCode_UnavailablePlayerSlot);
                }
            }
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
        }
    }
}