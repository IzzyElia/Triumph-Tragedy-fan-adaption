using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    [Serializable] public class SpecialDiplomacyActionFactionDefinition
    {
        public bool IsInsurgentAction;
        public string faction;
        public string[] countries;
        public string[] tiles;
        [NonSerialized] public int iFaction;
        [NonSerialized] public int[] iCountries;
        [NonSerialized] public int[] iTiles;

        public int GetStateHashCode()
        {
            int hash = 17;
            unchecked
            {
                hash *= faction.GetHashCode();
                foreach (var country in countries)
                {
                    hash *= country.GetHashCode();
                }
                foreach (var tile in tiles)
                {
                    hash *= tile.GetHashCode();
                }
            }
            return hash;
        }
    }
    
    [Serializable] public class SpecialDiplomacyAction
    {
        private const string _notSetupErrorMessage =
            "SpecialDiplomacyAction needs to have its values cached from the outside. In the gamestate, once factions and countries are setup, iterate through the faction definitions and set the iFaction and iCountries fields. Then, set Setup to true.";
        [NonSerialized] public bool Setup = false;
        public int ID;
        public string name;
        [SerializeField] private SpecialDiplomacyActionFactionDefinition[] factionDefinitions;
        public IEnumerable<SpecialDiplomacyActionFactionDefinition> FactionDefinitions => factionDefinitions;

        public SpecialDiplomacyActionFactionDefinition GetFactionDefinition(int iFaction)
        {
            if (!Setup) throw new InvalidOperationException(_notSetupErrorMessage);
            if (iFaction >= factionDefinitions.Length) return null;
            for (int i = 0; i < factionDefinitions.Length; i++)
            {
                if (factionDefinitions[i].iFaction == iFaction) 
                    return factionDefinitions[iFaction];
            }

            return null;
        }

        public int GetStateHashCode()
        {
            int hash = 17;
            unchecked
            {
                foreach (var factionDefinition in factionDefinitions)
                {
                    hash *= factionDefinition.GetStateHashCode();
                }
            }

            return hash;
        }
    }
}