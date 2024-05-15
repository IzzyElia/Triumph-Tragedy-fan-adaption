using System;
using System.Collections.Generic;
using Lidgren.Network;
using UnityEngine;
/*
namespace LidgrenServer
{
    public class Server : NetworkMember
    {
        // TODO Hash the passkey instead of sending it over the network in plain text
        
        private NetConnection[] _connections;
        private int MaxPlayers => _connections.Length;
        private bool _stopSignalled;
        private NetServer _server;
        private string _passkey;
        private bool _discoverable;
        private string _name;
        public Server(string name = "server", int port = NetProtocol.DefaultPort, int playerCap = 3, string passkey = "", bool discoverable = true)
        {
            _name = name;
            _passkey = passkey;
            _connections = new NetConnection[playerCap];
            NetPeerConfiguration config = new NetPeerConfiguration(NetProtocol.AppID);
            config.Port = port;
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            _server = new NetServer(config);
            base.NetPeer = _server;
        }
        public void Start()
        {
            _server.Start();
            base.StartMonitoring();
            Debug.Log("Started server: "+_server.Status.ToString());
        }

        public void Stop()
        {
            foreach (NetConnection connection in _connections)
            {
                // TODO Do something with each connected player?
            }
            base.StopMonitoring();
            Dispose();
        }
        protected override void HandleMessage(NetIncomingMessage incomingMessage)
        {
            switch (incomingMessage.MessageType)
            {
                case NetIncomingMessageType.DiscoveryRequest:
                    Debug.Log($"Discovery request from {incomingMessage.SenderEndPoint.Address}");
                    if (!_discoverable)
                    {
                        Debug.Log($"Ignoring discovery request (not discoverable)");
                            break;
                    }
                    _server.SendDiscoveryResponse(_server.CreateMessage(_name), incomingMessage.SenderEndPoint);
                    break;
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus status = (NetConnectionStatus)incomingMessage.ReadByte();
                    switch (status)
                    {
                        case NetConnectionStatus.RespondedConnect:
                            Debug.Log("New player joining");
                            HandleNewPlayerConnection(incomingMessage);
                            break;
                    }
                    break;
                // Add more message handling here
                case NetIncomingMessageType.Data:
                    switch (incomingMessage.ReadByte())
                    {
                        // TODO
                        // The server should only be receiving player actions and chats.
                    }
                    break;
            }
        }
        void HandleNewPlayerConnection(NetIncomingMessage incomingMessage)
        {
            Debug.Log($"Attempted connection from {incomingMessage.SenderConnection.RemoteEndPoint.Address}");
            // Check the desired player slot
            byte desiredPlayer = incomingMessage.ReadByte();
            int assignedPlayer = 0;
            if (desiredPlayer == byte.MaxValue)
            {
                for (int i = 0; i < MaxPlayers; i++)
                {
                    if (_connections[i] != null)
                    {
                        assignedPlayer = i;
                        break;
                    }
                }
            }
            else if (desiredPlayer >= MaxPlayers)
            {
                Debug.Log($"Denying connection from {incomingMessage.SenderConnection.RemoteEndPoint.Address}: Invalid player slot requested");
                incomingMessage.SenderConnection.Deny("Invalid player slot requested");
                return;
            }
            else if (_connections[desiredPlayer] == null)
            {
                Debug.Log($"Denying connection from {incomingMessage.SenderConnection.RemoteEndPoint.Address}: Unavailable player slot requested");
                incomingMessage.SenderConnection.Deny("Unavailable player slot requested");
                return;
            }
            else assignedPlayer = desiredPlayer;
                    
            // Check the passkey
            string passkey = incomingMessage.ReadString();
            if (passkey != this._passkey)
            {
                Debug.Log($"Denying connection from {incomingMessage.SenderConnection.RemoteEndPoint.Address}: Invalid Passkey");
                incomingMessage.SenderConnection.Deny("Invalid passkey");
                return;
            }
                    
            // A valid connection :)
            Debug.Log($"Connection approved to {incomingMessage.SenderConnection.RemoteEndPoint.Address}");
            incomingMessage.SenderConnection.Approve();
            _connections[assignedPlayer] = incomingMessage.SenderConnection;
            GameState.SendSync(this, assignedPlayer);
        }
        public NetOutgoingMessage CreateNewGameStateMessage()
        {
            NetOutgoingMessage message = _server.CreateMessage();
            message.Write(NetProtocol.GameStateTargetingRoutingHeader);
            return message;
        }
        public NetOutgoingMessage CreateNewEntityMessage(Type type, int id, bool isFullStateUpdate)
        {
            NetOutgoingMessage message = _server.CreateMessage();
            byte typeID;
            try
            {
                typeID = reverseTypeMap[type];
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError($"Type {type.FullName} not mapped");
                typeID = 0;
            }
            if (isFullStateUpdate)
                message.Write(NetProtocol.EntityFullUpdateRoutingHeader);
            else
                message.Write(NetProtocol.EntityCustomUpdateRoutingHeader);
            message.Write(typeID);
            message.Write(id);
            return message;
        }
        public void PushTo(NetOutgoingMessage message, params int[] playerFactions)
        {
            for (int i = 0; i < playerFactions.Length; i++)
            {
                int playerId = playerFactions[i];
                _server.SendMessage(message, _connections[playerId], NetDeliveryMethod.ReliableOrdered);
            }
        }
    }
}
*/