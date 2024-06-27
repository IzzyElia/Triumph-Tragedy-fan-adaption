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
        public TerrainType TerrainType;
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

        private int iOccupier = -1; // If made public then be sure to update the value it as country control changes
        public GameFaction Occupier
        {
            get
            {
                if (iOccupier >= 0) return ((TTGameState)GameState).GetEntity<GameFaction>(iOccupier);
                else if (Faction != null) return Faction;
                else return null;
            }
            set
            {
                if (value != null)
                    iOccupier = value.ID;
                else
                    iOccupier = -1;
            }

        }

        public bool IsOccupied => Country != null && iOccupier != Country.iFaction;

        public int[] ConnectedTileBorderIDs;

        public int[] ConnectedTileIDs;
        public GameTile[] ConnectedTiles; // Derived Value
        public GameBorder[] ConnectedBorders; // Derived Value
        public int Resources;
        public int ColonialResources;
        public int Population { get; private set; } // Derived Value
        public int Muster { get; private set; } // Derived Value
        public bool IsCoastal { get; private set; } // Derived Value
        public int CitySize;

        
        public IReadOnlyCollection<GameCadre> GetCadresOnTile()
        {
            return ((TTGameState)GameState).CadresByTileID.Get(this.ID);
        }
        
        public override void RecalculateDerivedValues()
        {
            // Connected tiles?
            ConnectedTiles = new GameTile[ConnectedTileIDs.Length];
            ConnectedBorders = new GameBorder[ConnectedTileBorderIDs.Length];
            for (int i = 0; i < ConnectedTileIDs.Length; i++)
            {
                ConnectedTiles[i] = GameState.GetEntity<GameTile>(ConnectedTileIDs[i]);
                ConnectedBorders[i] = GameState.GetEntity<GameBorder>(ConnectedTileBorderIDs[i]);
            }
            
            // Coastal?
            IsCoastal = false;
            if (TerrainType == TerrainType.Land || TerrainType == TerrainType.NotInPlay)
            {
                foreach (int iConnectedTile in ConnectedTileIDs)
                {
                    GameTile connectedTile = GameState.GetEntity<GameTile>(iConnectedTile);
                    if (connectedTile.TerrainType == TerrainType.Sea || connectedTile.TerrainType == TerrainType.Ocean)
                    {
                        IsCoastal = true;
                    }
                }
            }
            else if (TerrainType == TerrainType.Strait)
            {
                IsCoastal = true;
            }
            
            // Population and muster?
            int populationOfCitySize;
            int musterOfCitySize;
            switch (CitySize)
            {
                case 0: // No settlement
                    populationOfCitySize = 0; 
                    musterOfCitySize = 0; 
                    break; 
                case 1: // Town
                    populationOfCitySize = 0;
                    musterOfCitySize = 1;
                    break; 
                case 2: // City
                    populationOfCitySize = 1;
                    musterOfCitySize = 2;
                    break; 
                case 3: // Minor Capital
                    populationOfCitySize = 1;
                    musterOfCitySize = 3;
                    break; 
                case 4: // Sub-capital
                    populationOfCitySize = 2;
                    musterOfCitySize = 3;
                    break; 
                case 5: // Main-capital
                    populationOfCitySize = 3;
                    musterOfCitySize = 4;
                    break; 
                default: throw new NotImplementedException();
            }

            Muster = musterOfCitySize;

            // If the calculated population changed recalculate the faction as well, since total production may have changed
            if (Population != populationOfCitySize)
            {
                Population = populationOfCitySize;
                Faction?.RecalculateDerivedValues();
            }
            else
            {
                Population = populationOfCitySize;
            }
        }

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            iCountry = incomingMessage.ReadInt();
            iOccupier = incomingMessage.ReadInt();
            TerrainType = (TerrainType)incomingMessage.ReadByte();
            CitySize = incomingMessage.ReadByte();
            Resources = incomingMessage.ReadByte();
            ColonialResources = incomingMessage.ReadByte();

            byte connectedTilesLength = incomingMessage.ReadByte();
            ConnectedTileIDs = new int[connectedTilesLength];
            for (int i = 0; i < connectedTilesLength; i++)
            {
                ConnectedTileIDs[i] = incomingMessage.ReadShort();
            }
            byte connectedBordersLength = incomingMessage.ReadByte();
            ConnectedTileBorderIDs = new int[connectedBordersLength];
            for (int i = 0; i < connectedBordersLength; i++)
            {
                ConnectedTileBorderIDs[i] = incomingMessage.ReadShort();
            }
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(iCountry);
            outgoingMessage.WriteInt(iOccupier);
            outgoingMessage.WriteByte((byte)TerrainType);
            outgoingMessage.WriteByte((byte)CitySize);
            outgoingMessage.WriteByte((byte)Resources);
            outgoingMessage.WriteByte((byte)ColonialResources);

            outgoingMessage.WriteByte((byte)ConnectedTileIDs.Length);
            for (int i = 0; i < ConnectedTileIDs.Length; i++)
            {
                outgoingMessage.WriteShort((short)ConnectedTileIDs[i]);
            }
            outgoingMessage.WriteByte((byte)ConnectedTileBorderIDs.Length);
            for (int i = 0; i < ConnectedTileBorderIDs.Length; i++)
            {
                outgoingMessage.WriteShort((short)ConnectedTileBorderIDs[i]);
            }
        }

        public override int HashFullState(int asPlayer)
        {
            int hash = HashCode.Combine(iCountry, iOccupier, TerrainType, CitySize, Resources);
            unchecked
            {
                for (int i = 0; i < ConnectedTileIDs.Length; i++)
                {
                    hash *= ConnectedTileIDs[i].GetHashCode();
                }
                for (int i = 0; i < ConnectedTileBorderIDs.Length; i++)
                {
                    hash *= ConnectedTileBorderIDs[i].GetHashCode();
                }
            }
            return hash;
        }

        GameBorder GetBorder(GameTile otherTile)
        {
            for (int i = 0; i < ConnectedBorders.Length; i++)
            {
                if (ConnectedTiles[i] == otherTile) return ConnectedBorders[i];
            }

            return null;
        }

        public int GetBorderMovementLimit(GameTile otherTile, GameCadre cadre)
        {
            GameBorder border = GetBorder(otherTile);
            switch (border.BorderType)
            {
                case BorderType.Impassable:
                    return 0;
                case BorderType.Plains:
                    return 3;
                case BorderType.Forest:
                    return 2;
                case BorderType.River:
                    return 2;
                case BorderType.Mountain:
                    return 1;
                case BorderType.Coast:
                    if (cadre.Faction.HasTech(GameState.Ruleset.GetIDOfNamedTech("LSTs")))
                        return 2;
                    else
                        return 1;
                case BorderType.Strait:
                    if (cadre.Faction.HasTech(GameState.Ruleset.GetIDOfNamedTech("LSTs")))
                        return 2;
                    else
                        return 1;
                case BorderType.Unspecified:
                    return 0;
                default: return 0;
            }
        }
    }
}
