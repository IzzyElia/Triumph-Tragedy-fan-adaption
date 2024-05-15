using System;
using System.Collections.Generic;
using Lidgren.Network;
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
        protected UnityServer Server => GameState.NetworkMember as UnityServer;
        protected UnityClient Client => GameState.NetworkMember as UnityClient;

        public static GameEntity Create(Type type, GameState gameState)
        {
            if (!typeof(GameEntity).IsAssignableFrom(type))
                throw new ArgumentException($"{type.FullName} does not derive from GameEntity");
            GameEntity entity = (GameEntity)Activator.CreateInstance(type);
            entity.GameState = gameState;
            entity.Init();
            return entity;
        }
        public GameEntity() {}

        public void PushFullState()
        {
            if (GameState.NetworkMember is UnityServer server)
            {
                foreach (int iPlayer in server.ConnectionsFilled)
                {
                    DataStreamWriter outgoingMessage = GameState.CreateEntityUpdateMessage(iPlayer, GetType(), ID);
                    outgoingMessage.WriteByte(FullStateUpdateRoutingHeader);
                    WriteFullState(iPlayer, ref outgoingMessage);
                    server.PushMessage(outgoingMessage);
                }
            }
            else
                Debug.LogError("Not a serverside entity");
        }
        public void PushFullState(int targetPlayer)
        {
            if (GameState.NetworkMember is UnityServer server)
            {
                DataStreamWriter outgoingMessage = GameState.CreateEntityUpdateMessage(targetPlayer, GetType(), ID);
                outgoingMessage.WriteByte(FullStateUpdateRoutingHeader);
                WriteFullState(targetPlayer, ref outgoingMessage);
                server.PushMessage(outgoingMessage);
            }
            else Debug.LogError("Not a serverside entity");
        }

        protected void PushCustomUpdate(int targetPlayer, UpdateWriter updateWriter)
        {
            if (GameState.NetworkMember is UnityServer server)
            {
                DataStreamWriter outgoingMessage = GameState.CreateEntityUpdateMessage(targetPlayer, GetType(), ID);
                outgoingMessage.WriteByte(CustomUpdateRoutingHeader); // This tags the message as being a custom update
                updateWriter.Invoke(ref outgoingMessage);
                server.PushMessage(outgoingMessage);
            }
            else throw new InvalidOperationException("Not a serverside entity");
        }


        public void ReceiveUpdate(DataStreamReader incomingMessage)
        {
            byte header = incomingMessage.ReadByte();
            switch (header)
            {
                case FullStateUpdateRoutingHeader:
                    ReceiveFullState(incomingMessage);
                    break;
                case CustomUpdateRoutingHeader:
                    ReceiveCustomUpdate(incomingMessage);
                    break;
            }
        }

        protected virtual void Init() {}
        protected abstract void ReceiveCustomUpdate(DataStreamReader incomingMessage);
        protected abstract void ReceiveFullState(DataStreamReader incomingMessage);
        protected abstract void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage);
    }
}