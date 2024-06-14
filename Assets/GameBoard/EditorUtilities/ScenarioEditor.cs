using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    [CustomEditor(typeof(Scenario))]
    public class ScenarioEditor : Editor
    {
        private Scenario _scenario;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Copy Values"))
            {
                Map map = _scenario.copyValuesFrom;
                _scenario.factions.Clear();
                for (int i = 0; i < map.MapFactionsByID.Length; i++)
                {
                    MapFaction mapFaction = map.MapFactionsByID[i];
                    Scenario.Faction faction = new Scenario.Faction();
                    faction.name = mapFaction.name;
                    foreach (var mapCountry in map.MapCountriesByID)
                    {
                        if (mapCountry.faction == mapFaction) faction.countries.Add(mapCountry.name);
                    }

                    faction.startingUnits = mapFaction.startingUnits;
                    faction.startingSpecialUnits = mapFaction.startingSpecialUnits;
                    _scenario.factions.Add(faction);
                }
            }
        }

        private void OnEnable()
        {
            _scenario = (Scenario)target;
        }
    }
}