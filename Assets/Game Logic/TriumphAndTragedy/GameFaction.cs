using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
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
    
    public class GameFaction : GameEntity
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
        public byte CommandsAvailable;
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

        public Color Color => LeaderCountry.Color;
        
        public bool HasTech(Tech tech) => techs.Contains(tech);

        public HashSet<Tech> techs = new ();
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
            if (MapFaction is null) return;
            foreach (GameCountry country in GameState.GetEntitiesOfType<GameCountry>())
            {
                if (country.iFaction == ID)
                {
                    GameState.MapRenderer.MapCountriesByID[country.ID].SetFaction(MapFaction.ID);
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
                MapTile mapTile = GameState.MapRenderer.MapTilesByID[iTile];
                MapCountry mapCountry = GameState.MapRenderer.MapCountriesByID[iCountry];
                mapFaction.startingUnits.Add(new StartingUnitInfo(mapTile, mapCountry, startingCadres));
            }
        }

        protected override void OnDeactivated()
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            throw new NotImplementedException();
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            Name = incomingMessage.ReadFixedString64().ToString();
            CommandsAvailable = incomingMessage.ReadByte();
            iLeaderCountry = incomingMessage.ReadShort();
            
            ushort techsLength = incomingMessage.ReadUShort();
            techs.Clear();
            for (int i = 0; i < techsLength; i++)
            {
                techs.Add((Tech)incomingMessage.ReadUShort());
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
            outgoingMessage.WriteByte(CommandsAvailable);
            outgoingMessage.WriteShort((short)iLeaderCountry);
            
            outgoingMessage.WriteUShort((ushort)techs.Count);
            foreach (var tech in techs)
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
                hash *= CommandsAvailable;
                hash *= iLeaderCountry;
                foreach (var tech in techs)
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