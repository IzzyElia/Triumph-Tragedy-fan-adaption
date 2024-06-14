using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace GameLogic
{
    public enum DebuggingLevel
    {
        Always,
        StatusChanges,
        Events,
        IndividualMessageSends,
        IndividualMessages
    }
    public static class NetProtocol
    {
        public const string AppID = "sliu2od8jlwie7rty2oas8yo1lo20iu90";
        public const int DefaultPort = 8888;
        public const ushort DefaultBroadcastingPort = 8889;
        //public const byte SyncComplete = 0;
        public const byte EntityFullUpdateRoutingHeader = 1;
        public const byte GameStateTargetingRoutingHeader = 2;
        public const byte EntityCustomUpdateRoutingHeader = 3;
        public const byte ConnectionApprovedHeader = 4;
        public const byte ConnectionDeniedHeader = 5;
        public const byte PlayerActionRoutingHeader = 6;
        public const byte MessagesHandledHeader = 7;
        public const byte StartGameHeader = 8;
        public const byte PlayerActionReplyHeader = 9;
        public const byte SyncCheck = 10;
        public const byte SyncCheckResponse = 11;

        public const byte DenialCode_Approved = 0;
        public const byte DenialCode_WrongPassword = 1;
        public const byte DenialCode_NoPlayerSlots = 2;
        public const byte DenialCode_UnavailablePlayerSlot = 3;
    }
    public abstract class UnityNetworkMember : IDisposable
    {
        public GameState GameState;
        protected abstract NetworkDriver NetworkDriver { get; }
        protected abstract IReadOnlyCollection<NetworkConnection> Connections { get; }
        public abstract bool WaitingOnReply { get; }
        
        protected UnityNetworkMember(GameState gameState, ushort port = NetProtocol.DefaultPort)
        {
            Disposer.Register(this);
            this.GameState = gameState;
            gameState.NetworkMember = this;
        }

        public int HashPassword(string password)
        {
            int hash = 17;
            foreach (char c in password)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }

        public abstract void Dispose();

        private ulong monitorTickCount = 0;
        public void DoMonitor()
        {
            Monitor();
            monitorTickCount++;
        }
        protected abstract void Monitor();
        
        // Network Logging

        public int DebuggingID = -1;
        public static DebuggingLevel DebuggingLevel = DebuggingLevel.Events;

        public void NetworkingLog(string message, DebuggingLevel requiredDebuggingLevel = 0, bool isError = false)
        {
            string name = this is UnityServer ? "Server" : "Client";
            if ((int)DebuggingLevel >= (int)requiredDebuggingLevel)
            {
                string extraDebuggingInfo = $"\nMonitor #{monitorTickCount}";
                int iConnection = 0;
                foreach (NetworkConnection connection in Connections)
                {
                    extraDebuggingInfo += $"\nEvent Queue {iConnection}: {NetworkDriver.GetEventQueueSizeForConnection(connection)}";
                    iConnection++;
                }

                if (isError)
                {
                    if (DebuggingID == -1) Debug.LogError($"{name}: {message}" + extraDebuggingInfo);
                    else Debug.LogError($"{name} #{DebuggingID}: {message}" + extraDebuggingInfo);
                }
                else
                {
                    if (DebuggingID == -1) Debug.Log($"{name}: {message}" + extraDebuggingInfo);
                    else Debug.Log($"{name} #{DebuggingID}: {message}" + extraDebuggingInfo);
                }
            }
        }

    }
}