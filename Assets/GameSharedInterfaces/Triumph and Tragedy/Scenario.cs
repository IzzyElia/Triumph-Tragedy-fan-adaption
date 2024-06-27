using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    [Serializable] 
    public struct StartingUnitInfo
    {
        public string MapTile;
        public string Country;
        public int startingCadres;
    
        public StartingUnitInfo(string mapTile, string country, int startingCadres)
        {
            this.MapTile = mapTile;
            this.Country = country;
            this.startingCadres = startingCadres;
        }
    }
    [Serializable] public struct SpecialStartingUnitInfo
    {
        
        public string Tile;
        public string Country;
        public string UnitType;
        public int pips;
        
        public SpecialStartingUnitInfo(string mapTile, string country, string unitType, int pips)
        {
            Tile = mapTile;
            Country = country;
            UnitType = unitType;
            this.pips = pips;
        }
    }
    [CreateAssetMenu(fileName = "Scenario", menuName = "Game/Scenario", order = 1)]
    public class Scenario : ScriptableObject
    {
        // Basic scenario properties
        public int startYear;
        public List<Faction> factions;

        public static Scenario LoadScenario(string path)
        {
            return Resources.Load<Scenario>($"Scenarios/{path}");
        }
        
        [Serializable] public class Faction
        {
            public string name;
            public List<string> countries = new List<string>();
            public List<StartingUnitInfo> startingUnits = new List<StartingUnitInfo>();
            public List<SpecialStartingUnitInfo> startingSpecialUnits = new List<SpecialStartingUnitInfo>();
            public int startingIndustry;
            public string leader => countries.Count > 0 ? countries[0] : null;
        }
    }
}