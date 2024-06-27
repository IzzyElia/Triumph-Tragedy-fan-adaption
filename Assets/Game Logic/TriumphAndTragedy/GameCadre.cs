using System;
using System.Collections.Generic;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{

    
    // Reminder that GameEntity's should only ever have parameterless constructors.
    // They are created using GameEntity.Create() or with their own custom static creation methods
    /// <summary>
    /// Create with CreateCadre()
    /// </summary>
    public class GameCadre : GameEntity
    {
        // Update headers
        private const byte MoveHeader = 0;
        
        public const int UnitHidden = -2;
        public static GameCadre CreateCadre(GameState gameState, int iUnitType, int iCountry, int iTile)
        {
            if (!gameState.IsServer) throw new ServerOnlyException();
            
            int newId;
            if (!((TTGameState)gameState).FreedCadreIDs.TryDequeue(out newId))
            {
                newId = ((TTGameState)gameState).NextCadreID;
                ((TTGameState)gameState).NextCadreID++;
            }
            GameCadre cadre = gameState.GetOrCreateEntity<GameCadre>(newId);
            cadre.iUnitType = iUnitType;
            cadre.iCountry = iCountry;
            cadre.Tile = gameState.GetOrCreateEntity<GameTile>(iTile);
            ((TTGameState)gameState).CadresByTileID.Add(iTile, cadre);
            cadre.Active = true;
            cadre.RecalculateDerivedValues();
            return cadre;
        }
        public void Kill ()
        {
            if (!GameState.IsServer) throw new InvalidOperationException();
            ((TTGameState)GameState).CadresByTileID.Remove_CertainOfKey(_iTile, this);
            ((TTGameState)GameState).FreedCadreIDs.Enqueue(this.ID);
            Active = false;
            RecalculateDerivedValuesAndPushFullState();
        }

        protected override void OnDeactivated()
        {
            MapCadre.DestroyMapObject();
        }
        

        public MapCadre MapCadre
        {
            get
            {
                if (GameState.IsServer) throw new InvalidOperationException("Map rendering objects can only be accessed on the client side");
                return GameState.MapRenderer.MapCadresByID[ID];
            }
        }

        public int iUnitType;
        public UnitType UnitType => iUnitType == UnitHidden ? null : GameState.Ruleset.unitTypes[iUnitType];
        public bool UnitTypeIsHidden => iUnitType == UnitHidden;
        
        public int iCountry;
        public GameCountry Country
        {
            get => ((TTGameState)GameState).GetOrCreateEntity<GameCountry>(iCountry);
            set => iCountry = value.ID;
        }

        private int _iTile;

        public int iTile
        {
            get => _iTile;
            set
            {
                if (iTile != -1) ((TTGameState)GameState).CadresByTileID.Remove(_iTile, this);
                if (value != -1) ((TTGameState)GameState).CadresByTileID.Add(value, this);
                _iTile = value;
            }
        }
        public GameTile Tile
        {
            get => ((TTGameState)GameState).GetEntity<GameTile>(iTile);
            set
            {
                if (iTile != -1) ((TTGameState)GameState).CadresByTileID.Remove(iTile, this);
                ((TTGameState)GameState).CadresByTileID.Add(value.ID, this);
                iTile = value.ID;
            }
        }
        public byte Pips;
        public byte MaxPips;
        public GameFaction Faction => Country.Faction;

        protected override void Init()
        {
            iTile = -1;
        }

        public bool IsRevealedTo(int iPlayer)
        {
            return Faction == null ? true : iPlayer == Faction.ID; // TODO also reveal if in combat
        }

        
        public void PushMove(int[] path)
        {
            foreach (int iPlayer in GameState.Players)
            {
                DataStreamWriter message = StartCustomUpdate(MoveHeader, iPlayer);
                message.WriteUShort((ushort)path.Length);
                for (int i = 0; i < path.Length; i++)
                {
                    message.WriteUShort((ushort)path[i]);
                }

                Tile = GameState.GetEntity<GameTile>(path[^1]);
                PushCustomUpdate(iPlayer, ref message);
            }
        }
        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            switch (header)
            {
                case MoveHeader:
                    ushort pathLength = incomingMessage.ReadUShort();
                    int[] path = new int[pathLength];
                    for (int i = 0; i < pathLength; i++)
                    {
                        path[i] = (int)incomingMessage.ReadUShort();
                    }

                    Tile = GameState.GetEntity<GameTile>(path[^1]);
                    MapCadre.AnimateMovement(path);
                    break;
            }
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            iUnitType = incomingMessage.ReadInt();
            Pips = incomingMessage.ReadByte();
            MaxPips = incomingMessage.ReadByte();
            iCountry = incomingMessage.ReadInt();
            iTile = incomingMessage.ReadInt();
            
            MapTile tile = MapRenderer.MapTilesByID[iTile];
            MapCountry country = MapRenderer.MapCountriesByID[iCountry];
            UnitType unitType = iUnitType != byte.MaxValue ? GameState.Ruleset.unitTypes[iUnitType] : UnitType.Unknown;
            if (MapCadre is null)
            {
                try
                {
                    MapCadre.Create(name:$"Cadre {ID}", 
                        map:MapRenderer, 
                        tile:tile, 
                        country:country,
                        unitType:unitType,
                        id: ID);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else
            {
                MapCadre.MapCountry = country;
                MapCadre.Tile = tile;
                MapCadre.UnitType = unitType;
                MapCadre.MaxPips = MaxPips;
                MapCadre.Pips = Pips;
            }
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            // Note the differing serialization depending on whether the unit is revealed
            // Any changes need to be made to both sides
            if (IsRevealedTo(targetPlayer))
            {
                outgoingMessage.WriteInt(iUnitType);
                outgoingMessage.WriteByte(Pips);
                outgoingMessage.WriteByte(MaxPips);
                outgoingMessage.WriteInt(iCountry);
                outgoingMessage.WriteInt(iTile);
            }
            else
            {
                outgoingMessage.WriteInt(byte.MaxValue); // Unknown unit type
                outgoingMessage.WriteByte(0);
                outgoingMessage.WriteByte(0);
                outgoingMessage.WriteInt(iCountry);
                outgoingMessage.WriteInt(iTile);
            }
                

        }

        public override int HashFullState(int asPlayer)
        {
            int hash = 17;
            unchecked
            {
                if (IsRevealedTo(asPlayer))
                    hash *= iUnitType.GetHashCode();
                else
                    hash *= -2.GetHashCode();
                hash *= HashCode.Combine(iTile, iCountry, Pips, MaxPips);
            }

            return hash;
        }
    }
}