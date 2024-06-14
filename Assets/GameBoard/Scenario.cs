using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameBoard
{
    [CreateAssetMenu(fileName = "Scenario", menuName = "Game/Scenario", order = 1)]
    public class Scenario : ScriptableObject
    {
        // Basic scenario properties
        public int startYear;
        public List<Faction> factions;
        public Map copyValuesFrom;

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
            public string leader => countries.Count > 0 ? countries[0] : null;
        }
    }
}