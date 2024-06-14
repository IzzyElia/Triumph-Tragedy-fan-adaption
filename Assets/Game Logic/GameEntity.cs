using System;
using System.Collections.Generic;
using GameBoard;
using GameSharedInterfaces.Triumph_and_Tragedy;
using IzzysConsole;
using Unity.Collections;
using UnityEngine;

namespace GameLogic
{
    public abstract class GameEntity
    {
        
        const byte FullStateUpdateRoutingHeader = 0;
        private const byte CustomUpdateRoutingHeader = 1;
        
        protected delegate void UpdateWriter(ref DataStreamWriter outgoingMessage);
        public int ID;
        public GameState GameState;
        public Map MapRenderer => GameState.MapRenderer;
        protected UnityServer Server => GameState.NetworkMember as UnityServer;
        protected UnityClient Client => GameState.NetworkMember as UnityClient;
        public bool Active = false;

        public static GameEntity Create(Type type, GameState gameState)
        {
            if (!typeof(GameEntity).IsAssignableFrom(type))
                throw new ArgumentException($"{type.FullName} does not derive from GameEntity");
            GameEntity entity = (GameEntity)Activator.CreateInstance(type);
            entity.GameState = gameState;
            entity.Init();
            return entity;
        }

        public static T Create<T>(GameState gameState) where T : GameEntity => (T)Create(typeof(T), gameState);
        public GameEntity() {}
        
        public void PushFullState()
        {
            if (GameState.NetworkMember is UnityServer server)
            {
                foreach (int iPlayer in GameState.Players)
                {
                    PushFullState(iPlayer);
                }
            }
            else
                Debug.LogError("Not a serverside entity");
        }
        public void PushFullState(int targetPlayer)
        {
            GameState.NetworkMember.NetworkingLog($"Pushing full state of {GetType().Name} #{ID} to player {targetPlayer}", DebuggingLevel.IndividualMessageSends);
            if (GameState.NetworkMember is UnityServer server)
            {
                DataStreamWriter outgoingMessage = GameState.CreateEntityUpdateMessage(targetPlayer, GetType(), ID);
                outgoingMessage.WriteByte(FullStateUpdateRoutingHeader);
                outgoingMessage.WriteByte(Active ? (byte)1 : (byte)0);
                WriteFullState(targetPlayer, ref outgoingMessage);
                server.PushMessage(ref outgoingMessage, targetPlayer);
            }
            else Debug.LogError("Not a serverside entity");
        }

        protected ICollection<(int iPlayer, DataStreamWriter message)> StartCustomUpdates(byte customHeader)
        {
            if (GameState.NetworkMember is UnityServer server)
            {
                (int iPlayer, DataStreamWriter message)[] messages = new (int iPlayer, DataStreamWriter message)[server.ApprovedConnections.Count];
                int i = 0;
                foreach (int iPlayer in GameState.Players)
                {
                    DataStreamWriter message = StartCustomUpdate(customHeader, iPlayer);
                    messages[i] = (iPlayer, message);
                    i++;
                }
                return messages;
            }
            
            Debug.LogError("Not a serverside entity");
            return Array.Empty<(int iPlayer, DataStreamWriter message)>();
        }
        protected DataStreamWriter StartCustomUpdate(byte customHeader, int targetPlayer)
        {
            DataStreamWriter outgoingMessage = GameState.CreateEntityUpdateMessage(targetPlayer, GetType(), ID);
            outgoingMessage.WriteByte(CustomUpdateRoutingHeader); // This tags the message as being a custom update
            outgoingMessage.WriteByte(customHeader);
            return outgoingMessage;
        }

        
        protected void PushCustomUpdate(int targetPlayer, ref DataStreamWriter update)
        {
            if (GameState.NetworkMember is UnityServer server)
            {
                server.PushMessage(ref update, targetPlayer);
            }
            else throw new InvalidOperationException("Not a serverside entity");
        }


        public void ReceiveUpdate(ref DataStreamReader incomingMessage)
        {
            byte header = incomingMessage.ReadByte();
            switch (header)
            {
                case FullStateUpdateRoutingHeader:
                    _ReceiveFullState(ref incomingMessage);
                    break;
                case CustomUpdateRoutingHeader:
                    byte customUpdateHeader = incomingMessage.ReadByte();
                    ReceiveCustomUpdate(ref incomingMessage, customUpdateHeader);
                    break;
            }
        }

        protected virtual void OnDeactivated() {}
        protected virtual void Init() {}
        
        protected abstract void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header);

        private void _ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            bool wasActive = Active;
            Active = incomingMessage.ReadByte() == 1;
            if (Active) ReceiveFullState(ref incomingMessage);
            if (wasActive && !Active) OnDeactivated();
        }
        protected abstract void ReceiveFullState(ref DataStreamReader incomingMessage);
        protected abstract void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage);
        public abstract int HashFullState(int asPlayer);
        /*
        public virtual object GetExposedData(string key)
        {
            throw new InvalidOperationException($"This method exists only to be overwritten by derived types and should never be called. There is either no override in {this.GetType().Name}, or base.GetExposedData() is accidentally being called.");
        }
        */
    }
}