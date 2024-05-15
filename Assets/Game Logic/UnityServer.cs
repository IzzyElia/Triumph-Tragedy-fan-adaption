using System.Collections.Generic;
using System.Net;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine.Assertions;
using UnityEngine;

namespace GameLogic
{
    
    
    public class UnityServer : UnityNetworkMember
    {
        private NetworkDriver _networkDriver;
        NativeArray<NetworkConnection> _connections;
        private HashSet<int> _connectionsFilled = new();
        public IReadOnlyCollection<int> ConnectionsFilled => _connectionsFilled;
        private NativeList<NetworkConnection> _unapprovedConnections;
        private ushort _port;
        private string _password;
        
        public UnityServer(ushort port = NetProtocol.DefaultPort, string password = "")
        {
            _connections = new NativeArray<NetworkConnection>(3, Allocator.Persistent);
            _unapprovedConnections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
            NetworkSettings networkSettings = new NetworkSettings();
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
            Debug.Log("Disposing Server");
            _networkDriver.Dispose();
            _connections.Dispose();
            _unapprovedConnections.Dispose();
        }

        public override void Monitor()
        {
            _networkDriver.ScheduleUpdate().Complete();
            
            // Clean up connections
            for (int i = 0; i < _connections.Length; i++)
            {
                if (!_connections[i].IsCreated)
                {
                    _connections[i].Close(_networkDriver);
                    _connections[i] = default;
                }
            }
            for (int i = 0; i < _unapprovedConnections.Length; i++)
            {
                if (!_unapprovedConnections[i].IsCreated)
                {
                    _unapprovedConnections[i].Close(_networkDriver);
                    _unapprovedConnections.RemoveAtSwapBack(i);
                    i--;
                }
            }

            NetworkConnection newConnection;
            while ((newConnection = _networkDriver.Accept()) != default)
            {
                _unapprovedConnections.Add(newConnection);
                Debug.Log($"Connection from {newConnection.ToString()} waiting approval");
                // TODO Add a timeout system so that new connections that fail to provide follow up data are droppede
            }
            
            NetworkEvent.Type networkEventType;
            DataStreamReader incomingMessage;
            DataStreamWriter outgoingMessage;
            
            // Approved connections
            for (int i = 0; i < _connections.Length; i++)
            {
                NetworkConnection connection = _connections[i];
                while ((networkEventType = _networkDriver.PopEventForConnection(connection, out incomingMessage)) != NetworkEvent.Type.Empty)
                {
                    Debug.Log($"{GetType().Name} {networkEventType} event received");
                    switch (networkEventType)
                    {
                        case NetworkEvent.Type.Connect:
                            Debug.Log($"Client attempting connection");
                            break;
                        case NetworkEvent.Type.Disconnect:
                            _connectionsFilled.Remove(i);
                            Debug.Log($"Player #{i} disconnected");
                            break;
                        case NetworkEvent.Type.Data:
                            RouteIncomingData(incomingMessage);
                            break;
                    }
                }
            }
            
            // Unapproved connections
            for (int i = 0; i < _unapprovedConnections.Length; i++)
            {
                NetworkConnection unapprovedConnection = _unapprovedConnections[i];
                while ((networkEventType = _networkDriver.PopEventForConnection(unapprovedConnection, out incomingMessage)) != NetworkEvent.Type.Empty)
                {
                    Debug.Log($"{GetType().Name} {networkEventType} event received");
                    switch (networkEventType)
                    {
                        case NetworkEvent.Type.Connect:
                            break;
                        case NetworkEvent.Type.Disconnect:
                            break;
                        case NetworkEvent.Type.Data:
                            (int assignedPlayerSlot, byte denialCode) = ValidateNewConnection(incomingMessage);
                            if (assignedPlayerSlot >= 0)
                            {
                                _networkDriver.BeginSend(unapprovedConnection, out outgoingMessage);
                                outgoingMessage.WriteByte(NetProtocol.ConnectionApprovedHeader);
                                _networkDriver.EndSend(outgoingMessage);
                                _connections[assignedPlayerSlot] = unapprovedConnection;
                                _connectionsFilled.Add(assignedPlayerSlot);
                                GameState.SendSync(assignedPlayerSlot);
                                _unapprovedConnections.RemoveAtSwapBack(i);
                                i--;
                            }
                            else
                            {
                                _networkDriver.BeginSend(unapprovedConnection, out outgoingMessage);
                                outgoingMessage.WriteByte(NetProtocol.ConnectionDeniedHeader);
                                outgoingMessage.WriteByte(denialCode);
                                _networkDriver.EndSend(outgoingMessage);
                                _unapprovedConnections.RemoveAtSwapBack(i);
                                i--;
                            }

                            break;
                    }
                }
            }
        }

        public void RouteIncomingData(DataStreamReader incomingMessage)
        {
            
        }

        public DataStreamWriter CreateNewGameStateMessage(int targetPlayer)
        {
            NetworkConnection targetPlayerConnection = _connections[targetPlayer];
            DataStreamWriter outgoingMessage = new DataStreamWriter();
            if (targetPlayerConnection == default)
            {
                Debug.LogWarning("Target player is not connected");
                return outgoingMessage;
            }
            _networkDriver.BeginSend(targetPlayerConnection, out outgoingMessage);
            outgoingMessage.WriteByte(NetProtocol.GameStateTargetingRoutingHeader);
            return outgoingMessage;
        }

        public void PushMessage(DataStreamWriter outgoingMessage)
        {
            Debug.Log("Pushing Message...");
            _networkDriver.EndSend(outgoingMessage);
        }

        public (int, byte) ValidateNewConnection(DataStreamReader initiationMessage)
        {
            int hashedPassword = HashPassword(_password);
            int typesHash = initiationMessage.ReadInt();
            int sentPassword = initiationMessage.ReadInt();
            byte desiredPlayerSlot = initiationMessage.ReadByte();
            if (typesHash != GameState.TypesHash)
            {
                Debug.Log("Server: Denying connection - type map mismatch. This probably indicates a version mismatch");
            }
            if (sentPassword != hashedPassword)
            {
                Debug.Log("Server: Denying connection - wrong password");
                return (-1, NetProtocol.DenialCode_WrongPassword);
            }

            if (desiredPlayerSlot == byte.MaxValue)
            {
                for (int i = 0; i < _connections.Length; i++)
                {
                    if (_connections[i] == default)
                    {
                        return (i, 0); // Assign to the first available
                    }
                }

                Debug.Log("Server: Denying connection - no available player slots");
                return (-1, NetProtocol.DenialCode_UnavailablePlayerSlot);
            }
            else
            {
                if (_connections[desiredPlayerSlot] == default)
                {
                    return (desiredPlayerSlot, 0);
                }
                else
                {
                    Debug.Log("Server: Denying connection - desired player slot not available");
                    return (-1, NetProtocol.DenialCode_UnavailablePlayerSlot);
                }
            }
        }
        
    }
}