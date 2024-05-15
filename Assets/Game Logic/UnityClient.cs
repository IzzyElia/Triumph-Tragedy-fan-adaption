using GameBoard;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace GameLogic
{
    public class UnityClient : UnityNetworkMember
    {
        private NetworkDriver _networkDriver;
        NetworkConnection _connection;
        private bool _connected = false;
        private int _desiredPlayerSlot;
        private string _password;
        
        public UnityClient(ushort port = NetProtocol.DefaultPort)
        {
            NetworkSettings networkSettings = new NetworkSettings();
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

        
        public void Connect(string address, ushort port, string password, int desiredPlayerSlot)
        {
            this._password = password;
            this._desiredPlayerSlot = desiredPlayerSlot;
            NetworkEndpoint endpoint = NetworkEndpoint.Parse(address, port);
            _connection = _networkDriver.Connect(endpoint);
        }

        public void GetApproval()
        {
            int passwordHash = HashPassword(_password);
            if (_desiredPlayerSlot == -1) _desiredPlayerSlot = byte.MaxValue;
            _networkDriver.BeginSend(_connection, out var message);
            message.WriteInt(GameState.TypesHash);
            message.WriteInt(passwordHash);
            message.WriteByte((byte)_desiredPlayerSlot);
            _networkDriver.EndSend(message);
        }
        
        void HandleApprovedConnection(NetworkConnection connection)
        {
            GameObject mapObj = Object.Instantiate(Controller.MapRendererPrefab);
            Map map = mapObj.GetComponent<Map>();
            GameState.MapRenderer = map;
        }
        
        void HandleDeniedConnection(byte denialCode) // and provide emotional support lol
        {
            switch (denialCode)
            {
                case NetProtocol.DenialCode_WrongPassword:
                    Debug.Log($"{GetType().Name}: denied connection due to wrong password");
                    break;
                case NetProtocol.DenialCode_UnavailablePlayerSlot:
                    Debug.Log($"{GetType().Name}: denied connection - requested player slot unavailable");
                    break;
            }
        }

        public override void Dispose()
        {
            Debug.Log("Disposing Client");
            if (_connection != default)
                _networkDriver.Disconnect(_connection);
            _networkDriver.Dispose();
            if (GameState.MapRenderer != null)
                Object.Destroy(GameState.MapRenderer);
        }

        public override void Monitor()
        {
            _networkDriver.ScheduleUpdate().Complete();

            NetworkEvent.Type networkEventType;
            DataStreamReader incomingMessage;
            while ((networkEventType = _networkDriver.PopEvent(out _connection, out incomingMessage)) != NetworkEvent.Type.Empty)
            {
                Debug.Log($"{GetType().Name} {networkEventType} event received");
                switch (networkEventType)
                {
                    case NetworkEvent.Type.Connect:
                        Debug.Log($"Client connected to server. Requesting approval");
                        GetApproval();
                        break;
                    case NetworkEvent.Type.Disconnect:
                        _connected = false;
                        Debug.Log($"Client disconnected by server");
                        break;
                    case NetworkEvent.Type.Data:
                        RouteIncomingData(incomingMessage);
                        break;
                }
            }
        }

        void RouteIncomingData(DataStreamReader incomingMessage)
        {
            byte routingHeader = incomingMessage.ReadByte();
            switch (routingHeader)
            {
                case NetProtocol.ConnectionApprovedHeader:
                    HandleApprovedConnection(_connection);
                    break;
                case NetProtocol.ConnectionDeniedHeader:
                    byte denialCode = incomingMessage.ReadByte();
                    HandleDeniedConnection(denialCode);
                    break;
                case NetProtocol.GameStateTargetingRoutingHeader:
                    this.GameState.ReceiveAndRouteMessage(incomingMessage);
                    break;
            }
        }
        
    }
}