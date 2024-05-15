using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace GameLogic
{
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

        public const byte DenialCode_Approved = 0;
        public const byte DenialCode_WrongPassword = 1;
        public const byte DenialCode_NoPlayerSlots = 2;
        public const byte DenialCode_UnavailablePlayerSlot = 3;
    }
    public abstract class UnityNetworkMember : IDisposable
    {

        public GameState GameState;
        
        protected UnityNetworkMember(ushort port = NetProtocol.DefaultPort)
        {
            Controller.ThreadRisks.Add(this);
            this.GameState = new GameState(this);
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
        public abstract void Monitor();
    }
}