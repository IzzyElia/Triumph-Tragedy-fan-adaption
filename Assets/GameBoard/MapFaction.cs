using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace GameBoard
{
    public class MapFaction : MapObject
    {
        public static MapFaction Create(string name, Map map, int id)
        {
            MapFaction mapFaction = new GameObject(name, typeof(MapFaction)).GetComponent<MapFaction>();
            mapFaction.ID = id;
            mapFaction.RegisterTo(map);
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