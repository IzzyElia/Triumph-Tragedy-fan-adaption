using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{

    public enum CountryAttributes
    {
        /// <summary>
        /// No more than 3 pips per cadre
        /// </summary>
        LimitedCadres,
    }
    public enum FactionAttribute
    {
        /// <summary>
        /// May make winter moves in home territory
        /// </summary>
        WinterMovesInHomeTerritory,
    }
    
    public class GameFaction : GameEntity, IGameFaction
    {
        public string Name;
        public MapFaction MapFaction
        {
            get
            {
                if (GameState.IsServer) throw new InvalidOperationException("Map rendering objects can only be accessed on the client side");
                return GameState.MapRenderer.MapFactionsByID[ID];
            }
        } 
        public int iLeaderCountry = -1;
        public GameCountry LeaderCountry
        {
            get
            {
                if (iLeaderCountry >= 0)
                    return ((TTGameState)GameState).GetOrCreateEntity<GameCountry>(iLeaderCountry);
                else
                    throw new InvalidOperationException("Faction leader not set");
            }
            set
            {
                iLeaderCountry = value.ID;
            }
        }

        public DiplomaticStatus[] DiplomaticStatuses;
        public bool HasTech(int iTech) => iTechs.Contains(iTech);

        public HashSet<int> iTechs = new ();
        public int ProductionAvailable { get; set; } = 0;
        public int CommandsAvailable { get; set; } = 0;
        public int CommandInitiative { get; set; } = 0;
        public int Resources { get; private set; } // Derived value
        public int Population { get; private set; } // Derived value
        public int Industry { get; set; } = 0;
        public int FactoriesNeededForIndustryUpgrade { get; set; } = 5;
        public int Production => Mathf.Min(Industry, Population, Resources);

        public override void RecalculateDerivedValues()
        {
            Resources = 0;
            Population = 0;
            foreach (GameTile tile in GameState.GetEntitiesOfType<GameTile>())
            {
                if (tile.Active && tile.Occupier == this)
                {
                    Resources += tile.Resources;
                    Population += tile.Population;
                }
            }
        }
        
        public ICollection<GameCadre> GetCadres ()
        {
            GameCadre[] allCadres = GameState.GetEntitiesOfType<GameCadre>();
            return allCadres.Where(cadre => cadre.Country.iFaction == ID).ToList();
        }
        
        public HashSet<FactionAttribute> attributes = new ();
        public (int iTile, int iCountry, int startingCadres)[] startingUnits;
        
        public void ApplyToMap()
        {
            GameState.NetworkMember.NetworkingLog($"Applying Map State for {Name}");
            if (MapFaction is null) MapFaction.Create(name: Name, map: GameState.MapRenderer, id: ID);
            else
            {
                MapFaction.name = Name;
            }
            foreach (GameCountry country in GameState.GetEntitiesOfType<GameCountry>())
            {
                if (country.iFaction == ID)
                {
                    GameState.MapRenderer.MapCountriesByID[country.ID].SetFaction(MapFaction.ID, country.MembershipStatus);
                }
            }

            MapFaction.gameObject.name = Name;
            MapFaction.leader = GameState.MapRenderer.MapCountriesByID[iLeaderCountry];
            CopyStartingUnitsTo(MapFaction);
        }
        
        public void CopyStartingUnitsTo(MapFaction mapFaction)
        {
            mapFaction.startingUnits.Clear();
            foreach ((int iTile, int iCountry, int startingCadres) in startingUnits) // We pass along starting units onto the map renderer side so the UI can access them
            {
                string mapTile = GameState.MapRenderer.MapTilesByID[iTile].name;
                string mapCountry = GameState.MapRenderer.MapCountriesByID[iCountry].name;
                mapFaction.startingUnits.Add(new StartingUnitInfo(mapTile, mapCountry, startingCadres));
            }
        }

        protected override void OnDeactivatedClientside()
        {
            throw new NotSupportedException("Did you mean to deactivate a game faction?");
        }

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            Name = incomingMessage.ReadFixedString64().ToString();
            CommandsAvailable = incomingMessage.ReadShort();
            CommandInitiative = incomingMessage.ReadShort();
            iLeaderCountry = incomingMessage.ReadShort();
            Industry = incomingMessage.ReadShort();
            ProductionAvailable = incomingMessage.ReadShort();
            FactoriesNeededForIndustryUpgrade = incomingMessage.ReadByte();
            
            ushort techsLength = incomingMessage.ReadUShort();
            iTechs.Clear();
            for (int i = 0; i < techsLength; i++)
            {
                iTechs.Add(incomingMessage.ReadUShort());
            }

            ushort startingUnitsLength = incomingMessage.ReadUShort();
            startingUnits = new (int iTile, int iCountry, int startingCadres)[startingUnitsLength];
            for (int i = 0; i < startingUnitsLength; i++)
            {
                int iTile = (int)incomingMessage.ReadShort();
                int iCountry = (int)incomingMessage.ReadShort();
                int startingCadres = (int)incomingMessage.ReadUShort();
                startingUnits[i] = (iTile, iCountry, startingCadres);
            }

            // Updates to the map
            ApplyToMap();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteFixedString64(Name);
            outgoingMessage.WriteShort((short)CommandsAvailable);
            outgoingMessage.WriteShort((short)CommandInitiative);
            outgoingMessage.WriteShort((short)iLeaderCountry);
            outgoingMessage.WriteShort((short)Industry);
            outgoingMessage.WriteShort((short)ProductionAvailable);
            outgoingMessage.WriteByte((byte)FactoriesNeededForIndustryUpgrade);
            
            outgoingMessage.WriteUShort((ushort)iTechs.Count);
            foreach (var tech in iTechs)
            {
                outgoingMessage.WriteUShort((ushort)tech);
            }

            outgoingMessage.WriteUShort((ushort)startingUnits.Length);
            for (int i = 0; i < startingUnits.Length; i++)
            {
                outgoingMessage.WriteShort((short)startingUnits[i].iTile);
                outgoingMessage.WriteShort((short)startingUnits[i].iCountry);
                outgoingMessage.WriteUShort((ushort)startingUnits[i].startingCadres);
            }
        }

        public override int HashFullState(int asPlayer)
        {
            int hash = 17;
            unchecked
            {
                hash *= CommandsAvailable.GetHashCode();
                hash *= iLeaderCountry.GetHashCode();
                hash *= ProductionAvailable.GetHashCode();
                hash *= Industry.GetHashCode();
                hash *= FactoriesNeededForIndustryUpgrade.GetHashCode();
                foreach (var tech in iTechs)
                {
                    hash += tech.GetHashCode();
                }
                for (int i = 0; i < startingUnits.Length; i++)
                {
                    hash *= startingUnits[i].iTile.GetHashCode();
                    hash *= startingUnits[i].iCountry.GetHashCode();
                    hash *= startingUnits[i].startingCadres.GetHashCode();
                }
            }

            return hash;
        }
    }
}