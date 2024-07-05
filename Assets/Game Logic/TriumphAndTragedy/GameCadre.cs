using System;
using System.Collections.Generic;
using GameBoard;
using GameBoard.UI.AnimatedEvents;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{

    
    // Reminder that GameEntity's should only ever have parameterless constructors.
    // They are created using GameEntity.Create() or with their own custom static creation methods
    /// <summary>
    /// Create with CreateCadre()
    /// </summary>
    public class GameCadre : GameEntity, IGameCadre
    {
        // Update headers
        private const byte MoveHeader = 0;
        
        public const int UnitHidden = -2;
        public static GameCadre CreateCadre(GameState gameState, int iUnitType, int iCountry, int iTile, int pips = 1)
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
            cadre.Pips = pips;
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

        protected override void OnDeactivatedClientside()
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
        
        public int iCountry { get; private set; }
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
                _iTile = value.ID;
            }
        }
        public int Pips { get; set; }
        public int MaxPips { get; set; }
        public GameFaction Faction => Country.Faction;
        public IGameFaction IFaction => (IGameFaction)Faction;

        protected override void Init()
        {
            iTile = -1;
        }

        public bool IsRevealedTo(int iPlayer)
        {
            TTGameState ttGameState = (TTGameState)GameState;
            if (ttGameState.GamePhase == GamePhase.SelectNextCombat || ttGameState.GamePhase == GamePhase.Combat)
            {
                foreach (var combat in ttGameState.CommittedCombats)
                {
                    if (combat.iTile == Tile.ID) return true;
                }
            }
            return Faction == null ? true : iPlayer == Faction.ID;
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

                    iTile = path[^1];
                    // ReSharper disable once ObjectCreationAsStatement
                    new UnitMoveAnimation(GameState.UIController, path[^1], this.ID);
                    break;
            }
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            iUnitType = incomingMessage.ReadInt();
            Pips = incomingMessage.ReadShort();
            MaxPips = incomingMessage.ReadShort();
            iCountry = incomingMessage.ReadInt();
            iTile = incomingMessage.ReadInt();
            
            MapTile tile = MapRenderer.MapTilesByID[iTile];
            MapCountry country = MapRenderer.MapCountriesByID[iCountry];
            UnitType mapCadreUnitType = this.UnitType == null ? UnitType.Unknown : this.UnitType;
            if (MapCadre is null)
            {
                try
                {
                    MapCadre.Create(name:$"Cadre {ID}", 
                        map:MapRenderer, 
                        tile:tile, 
                        country:country,
                        unitType:mapCadreUnitType,
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
                MapCadre.UnitType = mapCadreUnitType;
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
                outgoingMessage.WriteShort((short)Pips);
                outgoingMessage.WriteShort((short)MaxPips);
                outgoingMessage.WriteInt(iCountry);
                outgoingMessage.WriteInt(iTile);
            }
            else
            {
                outgoingMessage.WriteInt(UnitHidden); // Unknown unit type
                outgoingMessage.WriteShort(0);
                outgoingMessage.WriteShort(0);
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

        public void TakeHit(byte hits = 1)
        {
            if (hits >= Pips)
            {
                //Kill(); // Units are killed at end of combat
            }
            else
            {
                Pips -= hits;
            }
        }
    }
}