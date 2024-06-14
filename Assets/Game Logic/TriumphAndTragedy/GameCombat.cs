using System;
using GameLogic;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    public class GameCombat : GameEntity
    {
        public int iTile;

        protected override void OnDeactivated()
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new System.NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            throw new System.NotImplementedException();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            throw new System.NotImplementedException();
        }

        public override int HashFullState(int asPlayer)
        {
            throw new System.NotImplementedException();
        }
    }
}