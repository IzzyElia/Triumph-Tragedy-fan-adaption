using System;
using System.Collections;
using System.Threading;
using GameBoard;
using Lidgren.Network;
using Unity.Collections;

namespace GameLogic
{
    
    public class GameTile : GameEntity
    {
        public string Name { get; private set; }
        public string Country { get; private set; }
        public MapTile MapTile { get; private set; }

        protected override void ReceiveCustomUpdate(DataStreamReader incomingMessage)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(DataStreamReader incomingMessage)
        {
            Name = incomingMessage.ReadFixedString64().ToString();
            MapTile = GameState.MapRenderer.GetTileByName(Name);
            if (MapTile.mapCountry != null)
            {
                Country = MapTile.mapCountry.name;
            }
            else
            {
                Country = null;
            }

        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteFixedString64(Name);
        }
    }

    public class GameUnit : GameEntity
    {
        public byte UnitType { get; set; }
        public byte Owner { get; set; }
        protected override void ReceiveCustomUpdate(DataStreamReader incomingMessage)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(DataStreamReader incomingMessage)
        {
            throw new NotImplementedException();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            throw new NotImplementedException();
        }
    }

    /*
    public class GameFaction : GameEntity
    {
        public GameFaction(GameState gameState) : base(gameState)
        {
            
        }
        public override void RecieveFullStateUpdate(NetIncomingMessage incomingMessage)
        {
            
        }

        public override void RecieveCustomUpdate(NetIncomingMessage incomingMessage)
        {
            
        }

        protected override void WriteFullState(ref NetOutgoingMessage message)
        {
            
        }
    }
    

    public class NeutralFaction : GameFaction
    {
        public NeutralFaction(GameState gameState) : base(gameState)
        {
            
        }
    }

    public class PlayerFaction : GameFaction
    {
        public PlayerFaction(GameState gameState) : base(gameState)
        {
            
        }
    }
    */
}
