using System;
using System.Collections.Generic;
using System.IO;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public class RuleAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        public RuleAttribute(string name = null, string description = null)
        {
            this.Name = name;
            this.Description = description;
        }
    }
    [Rule(name:"Unit Placement Rules", description:"Sets the rules for where on the map new units can be built")]
    public enum UnitPlacementRule
    {
        [Rule(name:" Initial Faction Member Only", description:"Units can only be built on the home territory on their factions starting countries")]
        HomeTerritoryOnly, // Original rules from triumph and tragedy
        [Rule(name:"Allied Builds Allowed", description:"Units can be built on allied home territory (home territory of any country brought into the faction diplomatically)")]
        DiploAnnexedCountriesAllowed, // Any country brought to your faction *diplomatically* can build their national units
        [Rule(name:"All Controlled Territory", description:"Units can be built anywhere your faction occupies")]
        AllControlledTerritory, // Units can be built anywhere your faction occupies
    }

    public enum CombatDamageRule
    {
        [Rule(name:"Prefer higher initiative (even spread)", description:"Higher initiative units are damaged before lower initiative units, but no cadre will be damaged more than the weakest cadre of its category (land/air/sea/sub)")]
        PreferHigherInitiativeEvenSpread, // Units can be built anywhere your faction occupies
        [Rule(name:"Random (even spread)", description:"Units are assigned damage at random, but no cadre will be damaged more than the weakest cadre of its category (land/air/sea/sub)")]
        RandomEvenSpread, // Units can be built anywhere your faction occupies
        [Rule(name:"Random", description:"Units are damaged completely at random")]
        FullRandom, // Units can be built anywhere your faction occupies
    }
    [CreateAssetMenu(fileName = "Ruleset", menuName = "Game/Ruleset", order = 1)]
    public class Ruleset : ScriptableObject
    {
        // Global rules
        public UnitPlacementRule unitPlacementRule = UnitPlacementRule.DiploAnnexedCountriesAllowed;
        public CombatDamageRule CombatDamageRule = CombatDamageRule.RandomEvenSpread;
        
        public int maxCadrePips = 4;
        public int TechMatchesRequiredForTechUpgrade = 2;
        
        // Techs
        public Tech[] techs = new Tech[0];
        public int GetIDOfNamedTech(string tech)
        {
            for (int i = 0; i < techs.Length; i++)
            {
                if (techs[i].Name == name)
                    return i;
            }
            return -1;
        }
        public Tech GetNamedTech(string name)
        {
            for (int i = 0; i < techs.Length; i++)
            {
                if (techs[i].Name == name)
                    return techs[i];
            }

            throw new InvalidOperationException($"No unit type in ruleset with the name {name}");
        }
        public Tech GetTech(int iTech)
        {
            if (iTech >= 0 && iTech < techs.Length) return techs[iTech];
            return null;
        }
        
        // Unit Types
        public int iSeaTransportUnitType => GetIDOfNamedUnitType("Convoy");
        public UnitType[] unitTypes = new UnitType[0];
        public int GetIDOfNamedUnitType(string name)
        {
            for (int i = 0; i < unitTypes.Length; i++)
            {
                if (unitTypes[i].Name == name)
                    return i;
            }
            return -1;
        }
        public UnitType GetNamedUnitType(string name)
        {
            for (int i = 0; i < unitTypes.Length; i++)
            {
                if (unitTypes[i].Name == name)
                    return unitTypes[i];
            }

            throw new InvalidOperationException($"No unit type in ruleset with the name {name}");
        }
        
        // Special Diplomacy Actions
        public SpecialDiplomacyAction[] specialDiplomacyActionDefinitions = new SpecialDiplomacyAction[0];

        public SpecialDiplomacyAction GetSpecialDiplomacyActionByName(string name)
        {
            for (int i = 0; i < specialDiplomacyActionDefinitions.Length; i++)
            {
                if (specialDiplomacyActionDefinitions[i].name == name)
                    return specialDiplomacyActionDefinitions[i];
            }
            
            Debug.LogError($"No special diplomacy action by the name of {name}");
            return null;
        }
        
        
        
        // General functions
        public static Ruleset LoadRuleset(string path)
        {
            Ruleset ruleset = Resources.Load<Ruleset>(Path.Combine("Rulesets", path));
            for (int i = 0; i < ruleset.techs.Length; i++)
            {
                ruleset.techs[i].ID = i;
                ruleset.techs[i].SetGraphics();
            }
            for (int i = 0; i < ruleset.unitTypes.Length; i++)
            {
                ruleset.unitTypes[i].IdAndInitiative = i;
                ruleset.unitTypes[i].LoadUnitGraphics();
            }
            if (ruleset is null) Debug.LogError($"Could not find ruleset {path}");
            return ruleset;
        }

        private void OnValidate()
        {
            for (int i = 0; i < techs.Length; i++)
            {
                techs[i].ID = i;
            }
            
            for (int i = 0; i < unitTypes.Length; i++)
            {
                unitTypes[i].IdAndInitiative = i;
            }

            for (int i = 0; i < specialDiplomacyActionDefinitions.Length; i++)
            {
                specialDiplomacyActionDefinitions[i].ID = i;
            }
        }

        public void Awake()
        {
            if (unitTypes.Length == 0 || unitTypes == null) unitTypes = new []
            {
                new UnitType("Fortress", category: UnitCategory.Ground, rebaseRule: RebaseRule.DoesntRebase,
                    groundAttack: 4, seaAttack: 3, airAttack: 2, subAttack: 2, movement: 0,
                    isFortress: true),
                new UnitType("AirForce", category: UnitCategory.Air, rebaseRule: RebaseRule.MustRebase,
                    groundAttack: 1, seaAttack: 1, airAttack: 3, subAttack: 1, movement: 2),
                new UnitType("Carrier", category: UnitCategory.Sea, rebaseRule: RebaseRule.CanRebase,
                    groundAttack: 1, seaAttack: 2, airAttack: 2, subAttack: 2, movement: 3,
                    takesDoubleHits: true, fireAndRetreatRule: FireAndRetreatRule.Can),
                new UnitType("Submarine", category: UnitCategory.Sub, rebaseRule: RebaseRule.MustRebase,
                    groundAttack: 0, seaAttack: 1, airAttack: 0, subAttack: 1, movement: 2),
                new UnitType("Fleet", category: UnitCategory.Sea, rebaseRule: RebaseRule.MustRebase,
                    groundAttack: 1, seaAttack: 3, airAttack: 1, subAttack: 2, movement: 3),
                new UnitType("Tank", category: UnitCategory.Ground, rebaseRule: RebaseRule.MustRebase,
                    groundAttack: 2, seaAttack: 0, airAttack: 0, subAttack: 0, movement: 2),
                new UnitType("Infantry", category: UnitCategory.Ground, rebaseRule: RebaseRule.MustRebase,
                    groundAttack: 3, seaAttack: 1, airAttack: 1, subAttack: 0, movement: 2),
                new UnitType("Marine", category: UnitCategory.Ground, rebaseRule: RebaseRule.MustRebase,
                    groundAttack: 2, seaAttack: 0, airAttack: 0, subAttack: 0, movement: 2),
                new UnitType("Militia", category: UnitCategory.Ground, rebaseRule: RebaseRule.DoesntRebase,
                    groundAttack: 2, seaAttack: 0, airAttack: 0, subAttack: 0, movement: 2,
                    isBuildableThroughNormalPlacementRules: false),
                new UnitType("Convoy", category: UnitCategory.Sea,
                    rebaseRule: RebaseRule.DoesntRebase,
                    groundAttack: 0, seaAttack: 0, airAttack: 0, subAttack: 0, movement: 2,
                    isBuildableThroughNormalPlacementRules: false, isTransportType:true)
            };
        }
    }
}