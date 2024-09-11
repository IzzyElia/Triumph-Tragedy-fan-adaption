using System;
using System.Collections.Generic;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using Izzy;
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

        public GameFaction AssociatedFaction
        {
            get
            {
                if (Country == null)
                {
                    return null;
                }
                else return Country.AssociatedFaction;
            }
        }

        private int iOccupier = -1; // If made public then be sure to update the value as country control changes
        public GameCountry Occupier
        {
            get
            {
                if (iOccupier >= 0) return ((TTGameState)GameState).GetEntity<GameCountry>(iOccupier);
                else if (Country != null) return Country;
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

        public int[] ConnectedTileIDs = Array.Empty<int>();
        public GameTile[] ConnectedTiles; // Derived Value
        public GameBorder[] ConnectedBorders; // Derived Value
        public int Resources;
        public int ColonialResources;

        public int Population
        {
            get
            {
                switch (CitySize)
                {
                    case 0: // No settlement
                        return 0;
                    case 1: // Town
                        return 0;
                    case 2: // City
                        return 1;
                    case 3: // Minor Capital
                        return 1;
                    case 4: // Sub-capital
                        return 2;
                    case 5: // Main-capital
                        return 3;
                    default: throw new NotImplementedException();
                }
            }
        }

        public int Muster
        {
            get
            {
                switch (CitySize)
                {
                    case 0: // No settlement
                        return 0; 
                    case 1: // Town
                        return 1;
                    case 2: // City
                        return 2;
                    case 3: // Minor Capital
                        return 3;
                    case 4: // Sub-capital
                        return 3;
                    case 5: // Main-capital
                        return 4;
                    default: throw new NotImplementedException();
                }
            }
        }
        public bool IsCoastal { get; private set; } // Derived Value
        public int CitySize;
        public int iStartingFaction;

        
        public IReadOnlyCollection<GameCadre> GetCadresOnTile()
        {
            return ((TTGameState)GameState).CadresByTileID.Get(this.ID);
        }
        
        public override void RecalculateDerivedValues()
        {
            // Connected tiles
            ConnectedTiles = new GameTile[ConnectedTileIDs.Length];
            ConnectedBorders = new GameBorder[ConnectedTileBorderIDs.Length];
            for (int i = 0; i < ConnectedTileIDs.Length; i++)
            {
                ConnectedTiles[i] = GameState.GetEntity<GameTile>(ConnectedTileIDs[i]);
                ConnectedBorders[i] = GameState.GetEntity<GameBorder>(ConnectedTileBorderIDs[i]);
            }
            
            // Coastal
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
        }

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            iCountry = incomingMessage.ReadInt();
            int prevOccupier = iOccupier;
            iOccupier = incomingMessage.ReadInt();
            iStartingFaction = incomingMessage.ReadInt();
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

            if (prevOccupier != iOccupier)
            {
                GameState.FlagForRecalculation(this);
                foreach (var gameFaction in GameState.GetEntitiesOfType<GameFaction>())
                {
                    GameState.FlagForRecalculation(gameFaction);
                }
            }

            if (GameState.NetworkMember.GameStarted) RefreshMapState();

        }

        public override void RefreshMapState()
        {
            MapTile.mapCountry = iCountry == -1 ? null : MapRenderer.MapCountriesByID[iCountry];
            MapTile.Occupier = Occupier == null || Occupier == Country ? null : MapRenderer.MapCountriesByID[Occupier.ID];
            MapTile.terrainType = TerrainType;
            MapTile.citySize = CitySize;
            MapTile.resources = Resources;
            MapTile.colonialResources = ColonialResources;
            MapTile.RecalculateMaterialDuringRuntime();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(iCountry);
            outgoingMessage.WriteInt(iOccupier);
            outgoingMessage.WriteInt(iStartingFaction);
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
            int hash = Hashing.MurmurHash3_Combine(iCountry, iOccupier, (int)TerrainType, CitySize, Resources);
            unchecked
            {
                for (int i = 0; i < ConnectedTileIDs.Length; i++)
                {
                    hash = Hashing.CombineHashes(hash, Hashing.MurmurHash3(ConnectedTileIDs[i]));
                }
                for (int i = 0; i < ConnectedTileBorderIDs.Length; i++)
                {
                    hash = Hashing.CombineHashes(hash, Hashing.MurmurHash3(ConnectedTileBorderIDs[i]));

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

        public void EvaluateControl()
        {
            GameCountry prevOccupier = Occupier;
            bool contested = false;
            bool firstFactionNeutral = false;
            GameFaction firstFactionPresent = null; // The first faction found. If a second faction is found after this value is set, then the tile is contested and does not change hands
            foreach (var gameCadre in GetCadresOnTile())
            {
                if (firstFactionPresent == null && firstFactionNeutral == false)
                {
                    firstFactionPresent = gameCadre.Faction;
                    if (gameCadre.Faction == null) firstFactionNeutral = true;
                }
                else
                {
                    if (gameCadre.Faction != firstFactionPresent)
                    {
                        contested = true;
                        break;
                    }
                }
            }

            if (!contested)
            {
                if (firstFactionPresent != null && Country?.Faction != firstFactionPresent)
                {
                    Occupier = firstFactionPresent.LeaderCountry;
                }
                else
                {
                    Occupier = null;
                }
            }

            if (prevOccupier != Occupier)
            {
                foreach (var gameFaction in GameState.GetEntitiesOfType<GameFaction>())
                {
                    GameState.FlagForRecalculation(gameFaction);
                }
                PushFullState();
            }
        }
    }
}
