using System;
using System.Collections.Generic;
using Game_Logic.TriumphAndTragedy;
using Game_Logic.TriumphAndTragedy.AI;
using GameBoard;
using GameBoard.UI;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public struct ClientsidePlayerData
    {
        public int ClientID;
        public string PlayerName;

        public ClientsidePlayerData(int clientID, string playerName)
        {
            ClientID = clientID;
            PlayerName = playerName;
        }
    }
    public class UnityClient : UnityNetworkMember
    {
        
        private NetworkDriver _networkDriver;
        NetworkConnection _connection;
        public bool IsBot => GameState.Bot != null;
        public bool Connected { get; private set; } = false;
        public bool AttemptingConnenction { get; private set; } = false;
        private int _desiredPlayerSlot;
        private string _address;
        private ushort _port;
        private int _lastPlayerSlot;
        private string _password;
        private string _playerName;
        private int _clientID = -1;
        public int PlayerSlot { get; private set; }
        private ClientsidePlayerData[] _playerSlots = Array.Empty<ClientsidePlayerData>();
        public IReadOnlyList<ClientsidePlayerData> PlayerSlots => _playerSlots;
        protected override NetworkDriver NetworkDriver => _networkDriver;
        private Dictionary<int, Action<bool>> callbacks = new Dictionary<int, Action<bool>>();
        private int callbackTimeoutTime;
        private bool _waitingOnCallback;
        public override bool WaitingOnReply => _waitingOnCallback;

        protected override IReadOnlyCollection<NetworkConnection> Connections =>
            new NetworkConnection[] { _connection };

        public UnityClient(ushort port = NetProtocol.DefaultPort, string botType = null) : base (new TTGameState(), port)
        {
            if (botType != null)
            {
                switch (botType)
                {
                    case "Chamberlain":
                        GameState.Bot = new ChamberlainBot((TTGameState)GameState);
                        break;
                    case "Passive":
                        GameState.Bot = new PassiveBot((TTGameState)GameState);
                        break;
                    default: 
                        Debug.LogError($"Invalid bot type {botType}");
                        break;
                }
            }
            NetworkSettings networkSettings = new NetworkSettings();
            networkSettings.WithNetworkConfigParameters(
                heartbeatTimeoutMS:int.MaxValue, // No timeout
                reconnectionTimeoutMS:int.MaxValue
            );
            _networkDriver = NetworkDriver.Create(networkSettings);
            NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
            if (_networkDriver.Bind(endpoint) != 0)
            {
                Debug.LogError($"Failed to bind to port {port}.");
                return;
            }
            _networkDriver.Listen();
            _networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerAction">The action to attempt</param>
        /// <param name="callback">The method to call when the action is processed by the server, the parameter being whether it was approved or not</param>
        public void AttemptAction(PlayerAction playerAction, Action<bool> callback)
        {
            if (Connected == false)
            {
                Debug.LogError("Not connected to the server, trying to reconnect");
                Reconnect();
                callback.Invoke(false);
                return;
            }
            int callbackHash = Time.time.GetHashCode(); // We watch for this hashcode signalling the response from the server
            callbacks.Add(callbackHash, callback);
            callbackTimeoutTime = 500;
            _waitingOnCallback = true;
            NetworkingLog($"Sending action request with callback id {callbackHash}", DebuggingLevel.IndividualMessageSends);
            
            _networkDriver.BeginSend(_connection, out DataStreamWriter outgoingMessage);
            outgoingMessage.WriteByte(NetProtocol.PlayerActionRoutingHeader);
            outgoingMessage.WriteByte(playerAction.TypeID);
            outgoingMessage.WriteInt(callbackHash);
            playerAction.Write(ref outgoingMessage);
            _networkDriver.EndSend(outgoingMessage);

        }
        
        public void Connect(string address, ushort port, string password, int desiredPlayerSlot, string playerName)
        {
            NetworkingLog($"Attempting connection to {address}:{port}");
            if (Connected || AttemptingConnenction) throw new InvalidOperationException();
            this._address = address;
            this._port = port;
            this._password = password;
            this._playerName = playerName;
            this._desiredPlayerSlot = desiredPlayerSlot;
            NetworkEndpoint endpoint = NetworkEndpoint.Parse(address, port);
            _connection = _networkDriver.Connect(endpoint);
            NetworkingLog($"Connection = {_connection}");
            AttemptingConnenction = true;
        }

        public void Reconnect()
        {
            Connect(address:_address, port:_port, password:_password, desiredPlayerSlot:_desiredPlayerSlot, playerName:_playerName);
        }

        public void AttemptChangePlayerSlot(int slot)
        {
            if (!Connected) throw new InvalidOperationException();
            _networkDriver.BeginSend(_connection, out var message);
            message.WriteByte(NetProtocol.RequestPlayerSlotHeader);
            message.WriteInt(slot);
            _networkDriver.EndSend(message);
        }

        public void GetApproval()
        {
            if (Connected) throw new InvalidOperationException();
            NetworkingLog($"Getting approval and connection = {_connection}");
            int passwordHash = HashPassword(_password);
            _networkDriver.BeginSend(_connection, out var message);
            message.WriteInt(passwordHash);
            message.WriteInt(GameState.TypesHash);
            message.WriteInt(_clientID);
            message.WriteFixedString512(_playerName);
            _networkDriver.EndSend(message);
        }

        public void SendSyncCheck()
        {
            if (Connected)
            {
                _networkDriver.BeginSend(_connection, out DataStreamWriter outgoingMessage);
                outgoingMessage.WriteByte(NetProtocol.SyncCheckResponse);
                outgoingMessage.WriteByte(GameState.IsSynced ? (byte)1 : (byte)0);
                outgoingMessage.WriteInt(GameState.GetStateHash(GameState.iPlayer, "ClientHashLog.txt"));
                NetworkingLog("Checking whether in sync...", DebuggingLevel.IndividualMessages);
                _networkDriver.EndSend(outgoingMessage);
            }
        }

        void HandleApprovedConnection(NetworkConnection connection, ref DataStreamReader incomingMessage)
        {
            int playerSlot = incomingMessage.ReadInt();
            int clientID = incomingMessage.ReadInt();
            if (clientID == -1 || clientID == 0) throw new ArgumentException();
            PlayerSlot = playerSlot;
            _clientID = clientID;
            _lastPlayerSlot = PlayerSlot;
            Connected = true;
            AttemptingConnenction = false;
            NetworkingLog($"connected to server");
            NetworkingLog($"Connection = {_connection}");
        }

        void Cleanup()
        {
            if (Controller.ActiveLocalClient == this) Controller.ActiveLocalClient = null;
            
            if (GameState.UIController is not null)
            {
                GameState.UIController.Dispose();
            }

            if (GameState.MapRenderer is not null)
            {
                GameState.MapRenderer.Dispose();
            }
        }

        void HandleGameStart(NetworkConnection connection, ref DataStreamReader incomingMessage)
        {
            Cleanup();
            GameStarted = true;
            string mapName = incomingMessage.ReadFixedString64().ToString();
            string rulesetName = incomingMessage.ReadFixedString64().ToString();
            GameObject mapPrefab = Map.LoadMap(mapName);
            Ruleset ruleset = Ruleset.LoadRuleset(rulesetName);
            if (mapPrefab is null) Debug.LogError($"Could not find map {mapName}");
            if (mapPrefab is null) Debug.LogError($"Could not find ruleset {rulesetName}");
            GameObject mapObj = Object.Instantiate(mapPrefab);
            Map map = mapObj.GetComponent<Map>();
            map.GameState = (ITTGameState)GameState;
            GameState.MapRenderer = map;
            GameState.Ruleset = ruleset;
            UIController uiController = UIController.Create(map);
            GameState.UIController = uiController;
            map.UIController = uiController;
            if (GameState.IsSynced)
            {
                GameState.FullyRefreshMapRenderer();
                GameState.FlagStateChange(true);
                GameState.UIController.SetActive(Controller.ActiveLocalClient == this);
            }
        }

        void HandleActionReply(NetworkConnection connection, ref DataStreamReader incomingMessage)
        {
            int callbackHeader = incomingMessage.ReadInt();
            bool actionSuccess = incomingMessage.ReadByte() == 1;
            NetworkingLog($"Action reply with callback id {callbackHeader}\nSuccess: {actionSuccess}", DebuggingLevel.IndividualMessages);

            if (!actionSuccess)
            {
                Debug.LogWarning($"Action failed - {incomingMessage.ReadFixedString128().ToString()}");
            }
            if (callbacks.TryGetValue(callbackHeader, out Action<bool> callback))
            {
                callback.Invoke(actionSuccess);
                callbacks.Remove(callbackHeader);
                if (callbacks.Keys.Count == 0) _waitingOnCallback = false;
            }
            else
            {
                NetworkingLog($"Received action reply for an action with invalid callback header ({callbacks.Keys.Count} actions waiting on replies)", DebuggingLevel.Events, true);
            }
        }
        
        void HandleDeniedConnection(byte denialCode) // and provide emotional support lol
        {
            AttemptingConnenction = false;
            switch (denialCode)
            {
                case NetProtocol.DenialCode_WrongPassword:
                    NetworkingLog($"denied connection due to wrong password");
                    break;
                case NetProtocol.DenialCode_UnavailablePlayerSlot:
                    NetworkingLog($"denied connection - requested player slot unavailable");
                    break;
            }
        }

        void HandlePlayerMetadata(NetworkConnection connection, ref DataStreamReader incomingMessage)
        {
            int myPlayerSlot = -1;
            _playerSlots = new ClientsidePlayerData[incomingMessage.ReadUShort()];
            for (int i = 0; i < _playerSlots.Length; i++)
            {
                byte playerStatus = incomingMessage.ReadByte();
                switch (playerStatus)
                {
                    case 0:
                        _playerSlots[i] = new ClientsidePlayerData(-1, "AI");
                        break;
                    case 1:
                        throw new NotImplementedException();
                    case 2:
                        int clientID = incomingMessage.ReadInt();
                        string playerName = incomingMessage.ReadFixedString512().ToString();
                        if (clientID == _clientID) PlayerSlot = i;
                        _playerSlots[i] = new ClientsidePlayerData(clientID, playerName);
                        break;
                }
                Controller.UnresolvedStateChange = true;
                GameState.FlagStateChange();
            }
                    
            _lastPlayerSlot = PlayerSlot;
        }

        public override void Dispose()
        {
            NetworkingLog("Disposing Client");
            if (_connection != default)
                _networkDriver.Disconnect(_connection);
            _networkDriver.Dispose();
            if (GameState.MapRenderer is not null) GameState.MapRenderer.Dispose();
            if (GameState.UIController is not null) GameState.UIController.Dispose();
            Controller.LocalClients.Remove(this);
            callbacks.Clear();
            Disposed = true;
        }

        protected override void Monitor()
        {
            
            if (Disposed) return;
            
            _networkDriver.ScheduleUpdate().Complete();

            NetworkEvent.Type networkEventType;
            DataStreamReader incomingMessage;
            bool eventHappened = false;
            bool sentConfirmation = false;
            while ((networkEventType = _networkDriver.PopEventForConnection(_connection, out incomingMessage)) != NetworkEvent.Type.Empty)
            {
                eventHappened = true;
                //NetworkingLog($"{GetType().Name} {networkEventType} event received");
                switch (networkEventType)
                {
                    case NetworkEvent.Type.Connect:
                        NetworkingLog($"connected to server. Requesting approval");
                        GetApproval();
                        break;
                    case NetworkEvent.Type.Disconnect:
                        Connected = false;
                        AttemptingConnenction = false;
                        GameState.IsSynced = false;
                        NetworkingLog($"disconnected by server");
                        Reconnect();
                        break;
                    case NetworkEvent.Type.Data:
                        RouteIncomingData(ref incomingMessage, out bool needsConfirmation);
                        if (needsConfirmation && !sentConfirmation)
                        {
                            sentConfirmation = true;
                            ReplyToData();
                        }
                        break;
                }
            }
            
            if (GameState.IsSynced) GameState.RunRecalculations();

            while ((networkEventType = _networkDriver.PopEvent(out NetworkConnection unknownConnection, out incomingMessage)) !=
                   NetworkEvent.Type.Empty)
            {
                NetworkingLog("Message from unknown sender"); // TODO adjust this in production
            }

            // Action callback timeout (this is mainly to avoid a memory leak if there's a connection issue. Dunno if it's really needed)
            if (_waitingOnCallback)
            {
                callbackTimeoutTime -= 1;
                if (callbackTimeoutTime <= 0)
                {
                    Debug.LogError("Server timeout in replying to attempted action");
                    callbacks.Clear();
                    _waitingOnCallback = false;
                } 
            }
        }

        void RouteIncomingData(ref DataStreamReader incomingMessage, out bool needsConfirmation)
        {
            needsConfirmation = false;
            byte routingHeader = incomingMessage.ReadByte();
            switch (routingHeader)
            {
                case NetProtocol.ConnectionApprovedHeader:
                    HandleApprovedConnection(_connection, ref incomingMessage);
                    needsConfirmation = false;
                    break;
                case NetProtocol.ConnectionDeniedHeader:
                    byte denialCode = incomingMessage.ReadByte();
                    HandleDeniedConnection(denialCode);
                    needsConfirmation = false;
                    break;
                case NetProtocol.StartGameHeader:
                    HandleGameStart(_connection, ref incomingMessage);
                    needsConfirmation = false;
                    break;
                case NetProtocol.PlayerActionReplyHeader:
                    HandleActionReply(_connection, ref incomingMessage);
                    break;
                case NetProtocol.SyncCheck:
                    SendSyncCheck();
                    break;
                case NetProtocol.GameStateTargetingRoutingHeader:
                    NetworkingLog("Received game state update", DebuggingLevel.IndividualMessages);
                    this.GameState.ReceiveAndRouteMessage(ref incomingMessage);
                    needsConfirmation = true;
                    break;
                case NetProtocol.NotifyPlayerSlotHeader:
                    HandlePlayerMetadata(_connection, ref incomingMessage);
                    break;
            }
        }
        
        void ReplyToData()
        {
            _networkDriver.BeginSend(_connection, out var confirmationMessage);
            confirmationMessage.WriteByte(NetProtocol.MessagesHandledHeader);
            _networkDriver.EndSend(confirmationMessage);
        }
    }
}