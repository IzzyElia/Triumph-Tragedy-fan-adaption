using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard
{

    [Serializable] 
    public struct StartingUnitInfo
    {
        public MapTile MapTile;
        public MapCountry Country;
        public int startingCadres;
    
        public StartingUnitInfo(MapTile mapTile, MapCountry country, int startingCadres)
        {
            this.MapTile = mapTile;
            this.Country = country;
            this.startingCadres = startingCadres;
        }

        public override int GetHashCode() => HashCode.Combine(MapTile.ID, Country.ID, startingCadres);
    }
    [Serializable] public struct SpecialStartingUnitInfo
    {
        
        public MapTile MapTile;
        public MapCountry Country;
        public string UnitType;
        public int pips;
        
        public SpecialStartingUnitInfo(MapTile mapTile, MapCountry country, string unitType, int pips)
        {
            MapTile = mapTile;
            Country = country;
            UnitType = unitType;
            this.pips = pips;
        }
    }
    public class MapFaction : MapObject
    {
        public static MapFaction Create(string name, Map map, int id)
        {
            MapFaction mapFaction = new GameObject(name, typeof(MapFaction)).GetComponent<MapFaction>();
            mapFaction.RegisterTo(map);
            mapFaction.ID = id;
            mapFaction.transform.SetParent(map.countriesWrapper.transform);
            return mapFaction;
        }
        public List<StartingUnitInfo> startingUnits = new List<StartingUnitInfo>();
        [NonSerialized] public List<SpecialStartingUnitInfo> startingSpecialUnits = new List<SpecialStartingUnitInfo>();
        public MapCountry leader;

        public IReadOnlyCollection<MapCountry> GetControlledCountries()
        {
            List<MapCountry> members = new List<MapCountry>();
            foreach (var mapCountry in Map.MapCountriesByID)
            {
                if (mapCountry.faction == this) members.Add(mapCountry);
            }

            return members;
        }
    }
}