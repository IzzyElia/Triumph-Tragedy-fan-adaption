using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;
using GameLogic;

namespace LidgrenServer
{
    public static class NetProtocol
    {
        public const string AppID = "sliu2od8jlwie7rty2oas8yo1lo20iu90";
        public const int DefaultPort = 8888;
        //public const byte SyncComplete = 0;
        public const byte EntityFullUpdateRoutingHeader = 1;
        public const byte GameStateTargetingRoutingHeader = 2;
        public const byte EntityCustomUpdateRoutingHeader = 3;
    }
    
    public abstract class NetworkMember : IDisposable
    {
        protected Dictionary<byte, Type> typeMap = new Dictionary<byte, Type>();
        protected Dictionary<Type, byte> reverseTypeMap = new Dictionary<Type, byte>();
        public GameState GameState;
        protected NetPeer NetPeer;
        bool _stopSignalled;
        Thread _monitorThread;
        public bool TEST_pingThread = false;

        protected NetworkMember()
        {
            Controller.ThreadRisks.Add(this);
        }

        public void SetTypeID(Type type, byte id)
        {
            if (typeMap.ContainsKey(id) || reverseTypeMap.ContainsKey(type))
            {
                typeMap.Remove(id);
                reverseTypeMap.Remove(type);
            }
            typeMap.Add(id, type);
            reverseTypeMap.Add(type, id);
        }
        public Type GetTypeFromID(byte id)
        {
            try
            {
                return typeMap[id];
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError($"No type associated with ID {id}");
                return null;
            } 
        }
        public byte GetTypeID(Type type)
        {
            try
            {
                return reverseTypeMap[type];
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError($"Type not mapped {type.FullName}");
                return 0;
            }
        }
        public void Monitor (object UNUSED)
        {
            NetIncomingMessage incomingMessage;
            if (TEST_pingThread)
            {
                Debug.Log($"{GetType().Name} thread running");
                TEST_pingThread = false;
            }
            while ((incomingMessage = NetPeer.ReadMessage()) != null)
            {
                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.ErrorMessage:
                        Debug.LogWarning($"{this.GetType().Name} error: {incomingMessage.ReadString()}");
                        break;
                    case NetIncomingMessageType.DebugMessage:
                        Debug.Log($"{this.GetType().Name} debug: {incomingMessage.ReadString()}");
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        Debug.LogWarning($"{this.GetType().Name} warning: {incomingMessage.ReadString()}");
                        break;
                    case NetIncomingMessageType.VerboseDebugMessage:
                        Debug.Log($"{this.GetType().Name} verbose debug: {incomingMessage.ReadString()}");
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)incomingMessage.PeekByte();
                        switch (status)
                        {
                            case NetConnectionStatus.Connected:
                                Debug.Log($"{GetType().Name} connected to {incomingMessage.SenderConnection.RemoteEndPoint.Address}");
                                break;
                            default:
                                Debug.Log($"{GetType().Name} {Enum.GetName(typeof(NetConnectionStatus), status)}");
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        Debug.Log($"{GetType().Name} received {incomingMessage.LengthBytes}b of data");
                        break;
                    default:
                        Debug.Log($"Messaged received by {this.GetType().Name} - of size {incomingMessage.LengthBytes}b of type {incomingMessage.MessageType.ToString()}");
                        break;
                }
                
                
                HandleMessage(incomingMessage);
                NetPeer.Recycle(incomingMessage);
            }
        }

        protected void StartMonitoring()
        {
            NetPeer.RegisterReceivedCallback(Monitor);
            //_monitorThread = new Thread(new ThreadStart(Monitor));
            //_monitorThread.Priority = ThreadPriority.Normal;
            //_monitorThread.Start();
        }

        protected void StopMonitoring()
        {
            _stopSignalled = true;
            NetPeer.UnregisterReceivedCallback(Monitor);
            NetPeer.Shutdown($"{GetType().Name} stopped");
            /*
            _monitorThread.Join(1000);
            if (_monitorThread.IsAlive)
            {
                Debug.LogError($"{this.GetType().Name} monitoring thread failed to shutdown");
                _monitorThread.Abort();
                if (_monitorThread.IsAlive)
                {
                    Debug.LogError($"!!!THREAD NOT STOPPED!!! {this.GetType().Name} monitoring thread failed to shutdown after an abort attempt");
                }
            }
            */
            Debug.Log($"{this.GetType().Name} stopped");
        }
        protected abstract void HandleMessage(NetIncomingMessage incomingMessage);
        public void Dispose()
        {
            StopMonitoring();
            Controller.ThreadRisks.Remove(this);
        }
    }
}