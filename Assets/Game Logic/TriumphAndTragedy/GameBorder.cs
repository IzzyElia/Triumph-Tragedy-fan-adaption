using System;
using GameBoard;
using GameLogic;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    
    public class GameBorder : GameEntity
    {
        public MapBorder MapBorder
        {
            get
            {
                if (GameState.IsServer) throw new InvalidOperationException("Map rendering objects can only be accessed on the client side");
                return GameState.MapRenderer.MapBordersByID[ID];
            }
        }
        public BorderType BorderType;

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            BorderType = (BorderType)incomingMessage.ReadByte();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteByte((byte)BorderType);
        }

        public override int HashFullState(int asPlayer)
        {
            return HashCode.Combine(BorderType);
        }
    }
}