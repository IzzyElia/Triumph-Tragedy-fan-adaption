using System;
using System.Collections.Generic;
using System.Net;
using Lidgren.Network;
using Unity.VisualScripting;
using UnityEngine;
using GameLogic;
/*
namespace LidgrenServer
{
    public class Client : NetworkMember
    {
        private NetClient _client;
        private string _serverAddress;
        public List<IPEndPoint> knownServers = new List<IPEndPoint>();
        public List<Action<IPEndPoint>> onServerFound = new List<Action<IPEndPoint>>();
        
        
        public Client(int listeningPort = NetProtocol.DefaultPort)
        {
            GameState = new GameState(this);
            NetPeerConfiguration config = new NetPeerConfiguration(NetProtocol.AppID);
            config.Port = listeningPort;
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            _client = new NetClient(config);
            NetPeer = _client;
            StartMonitoring();
        }

        public void TryConnect(IPEndPoint serverAddress, int preferredPlayer = -1, string passkey = "")
        {
            byte preferredPlayerByte = preferredPlayer == -1 ? byte.MaxValue : (byte)preferredPlayer;
            NetOutgoingMessage hailMessage = NetPeer.CreateMessage();
            hailMessage.Write(preferredPlayerByte);
            hailMessage.Write(passkey);
            _client.Start();
            _client.Connect(serverAddress, hailMessage);
            Debug.Log($"Attempting to connect to {serverAddress}");
        }
        
        public void TryConnect(string serverAddress, int port = NetProtocol.DefaultPort, int preferredPlayer = -1, string passkey = "")
        {
            byte preferredPlayerByte = preferredPlayer == -1 ? byte.MaxValue : (byte)preferredPlayer;
            NetOutgoingMessage hailMessage = NetPeer.CreateMessage();
            hailMessage.Write(preferredPlayerByte);
            hailMessage.Write(passkey);
            _client.Start();
            _client.Connect(serverAddress, port, hailMessage);
            Debug.Log($"Attempting to connect to {serverAddress}");
        }

        // TODO This is currently local network only
        public void DiscoverServers(int port = NetProtocol.DefaultPort)
        {
            Debug.Log("Discovering...");
            knownServers.Clear();
            _client.DiscoverLocalPeers(port);
        }

        protected override void HandleMessage(NetIncomingMessage incomingMessage)
        {
            switch (incomingMessage.MessageType)
            {
                case NetIncomingMessageType.DiscoveryResponse:
                    Debug.Log($"Discovered server at {incomingMessage.SenderEndPoint.Address}");
                    knownServers.Add(incomingMessage.SenderEndPoint);
                    foreach (var action in onServerFound)
                    {
                        action.Invoke(incomingMessage.SenderEndPoint);
                    }
                    break;

                case NetIncomingMessageType.Data:
                    byte dataType = incomingMessage.ReadByte();
                    switch (dataType)
                    {
                        case NetProtocol.GameStateTargetingRoutingHeader:
                            GameState.ReceiveMessage(incomingMessage);
                            break;
                        case NetProtocol.EntityFullUpdateRoutingHeader:
                            RouteEntityUpdate(incomingMessage, true);
                            break;
                        case NetProtocol.EntityCustomUpdateRoutingHeader:
                            RouteEntityUpdate(incomingMessage, false);
                            break;
                    }
                    break;
            }
        }

        void RouteEntityUpdate(NetIncomingMessage incomingMessage, bool isFullStateUpdate)
        {
            byte typeId = incomingMessage.ReadByte();
            int entityId = incomingMessage.ReadInt32();
            Type type = typeMap[typeId];
            GameEntity entity = GameState.GetEntity(type, entityId);
            if ((entity) != null)
            {
                if (isFullStateUpdate)
                    entity.RecieveFullStateUpdate(incomingMessage);
                else
                    entity.RecieveCustomUpdate(incomingMessage);
            }
        }
    }
}
*/