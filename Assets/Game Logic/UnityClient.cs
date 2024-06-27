using System;
using System.Collections.Generic;
using Game_Logic.TriumphAndTragedy;
using GameBoard;
using GameBoard.UI;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    public class UnityClient : UnityNetworkMember
    {
        public int DebuggingClientID;

        private NetworkDriver _networkDriver;
        NetworkConnection _connection;
        public bool Connected { get; private set; } = false;
        public bool AttemptingConnenction { get; private set; } = false;
        private int _desiredPlayerSlot;
        private string _password;
        private bool _disposed = false;
        public int PlayerSlot { get; private set; }
        protected override NetworkDriver NetworkDriver => _networkDriver;
        private Dictionary<int, Action<bool>> callbacks = new Dictionary<int, Action<bool>>();
        private int callbackTimeoutTime;
        private bool _waitingOnCallback;
        public override bool WaitingOnReply => _waitingOnCallback;

        protected override IReadOnlyCollection<NetworkConnection> Connections =>
            new NetworkConnection[] { _connection };

        public UnityClient(ushort port = NetProtocol.DefaultPort) : base (new TTGameState(), port)
        {
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
            if (Connected == false) Debug.LogError("Not connected to a server");
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

        
        public void Connect(string address, ushort port, string password, int desiredPlayerSlot)
        {
            NetworkingLog("Attempting connection");
            if (Connected || AttemptingConnenction) throw new InvalidOperationException();
            this._password = password;
            this._desiredPlayerSlot = desiredPlayerSlot;
            NetworkEndpoint endpoint = NetworkEndpoint.Parse(address, port);
            _connection = _networkDriver.Connect(endpoint);
            NetworkingLog($"Connection = {_connection}");
            AttemptingConnenction = true;
        }

        public void GetApproval()
        {
            if (Connected) throw new InvalidOperationException();
            NetworkingLog($"Getting approval and connection = {_connection}");
            int passwordHash = HashPassword(_password);
            if (_desiredPlayerSlot == -1) _desiredPlayerSlot = byte.MaxValue;
            _networkDriver.BeginSend(_connection, out var message);
            message.WriteInt(GameState.TypesHash);
            message.WriteInt(passwordHash);
            message.WriteByte((byte)_desiredPlayerSlot);
            _networkDriver.EndSend(message);
        }

        public void SendSyncCheck()
        {
            if (Connected)
            {
                _networkDriver.BeginSend(_connection, out DataStreamWriter outgoingMessage);
                outgoingMessage.WriteByte(NetProtocol.SyncCheckResponse);
                outgoingMessage.WriteInt(GameState.GetStateHash(GameState.iPlayer, "ClientHashLog.txt"));
                NetworkingLog("Checking whether in sync...", DebuggingLevel.IndividualMessages);
                _networkDriver.EndSend(outgoingMessage);
            }
        }

        void HandleApprovedConnection(NetworkConnection connection, ref DataStreamReader incomingMessage)
        {
            PlayerSlot = incomingMessage.ReadInt();
            Connected = true;
            AttemptingConnenction = false;
            NetworkingLog($"connected to server");
            NetworkingLog($"Connection = {_connection}");
        }

        void HandleGameStart(NetworkConnection connection, ref DataStreamReader incomingMessage)
        {
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
            NetworkingLog("initialized for game start and ready for sync");
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

        public override void Dispose()
        {
            NetworkingLog("Disposing Client");
            if (_connection != default)
                _networkDriver.Disconnect(_connection);
            _networkDriver.Dispose();
            if (GameState.MapRenderer != null)
                Object.Destroy(GameState.MapRenderer);
            if (GameState.UIController is not null) GameState.UIController.Dispose();
            callbacks.Clear();
            _disposed = true;
        }

        protected override void Monitor()
        {
            
            if (_disposed) return;
            
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
                        NetworkingLog($"disconnected by server");
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