using System;
using System.Collections.Generic;
using GameBoard;
using GameLogic;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    
    public class GameTile : GameEntity
    {
        public int MapTileID => ID;
        public MapTile MapTile
        {
            get
            {
                if (GameState.IsServer) throw new InvalidOperationException("Map rendering objects can only be accessed on the client side");
                return GameState.MapRenderer.MapTilesByID[MapTileID];
            }
        }
        public MapTile.TerrainType TerrainType;
        public string Name => MapTile.name;
        public int iCountry;
        public GameCountry Country
        {
            get
            {
                if (iCountry >= 0)
                    return ((TTGameState)GameState).GetOrCreateEntity<GameCountry>(iCountry);
                else
                    return null;
            }
            set
            {
                if (value != null)
                    iCountry = value.ID;
                else
                    iCountry = -1;
            }
        }

        public GameFaction Faction
        {
            get
            {
                if (Country == null)
                {
                    return null;
                }
                else return Country.Faction;
            }
        }

        public int iOccupier;
        public GameFaction Occupier
        {
            get
            {
                if (iOccupier >= 0) return ((TTGameState)GameState).GetEntity<GameFaction>(iOccupier);
                return null;
            }
            set
            {
                if (value != null)
                    iOccupier = value.ID;
                else
                    iOccupier = -1;
            }

        }

        public bool IsOccupied => iOccupier != -1;

        public int[] ConnectedTileBorderIDs;

        public int[] ConnectedTileIDs;

        public IReadOnlyCollection<GameCadre> GetCadresOnTile()
        {
            return ((TTGameState)GameState).CadresByTileID.Get(this.ID);
        }

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            iCountry = incomingMessage.ReadInt();
            TerrainType = (MapTile.TerrainType)incomingMessage.ReadByte();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(iCountry);
            outgoingMessage.WriteByte((byte)TerrainType);
        }

        public override int HashFullState(int asPlayer)
        {
            return HashCode.Combine(iCountry, TerrainType);
        }
    }
}
