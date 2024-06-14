using System;
using System.Collections.Generic;
using System.IO;
using GameBoard;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameLogic
{
    [CreateAssetMenu(fileName = "Ruleset", menuName = "Game/Ruleset", order = 1)]
    public class Ruleset : ScriptableObject, IRuleset
    {
        public int SeaTransportUnitType => GetIDOfNamedUnitType("Convoy");

        
        // Unit Types
        public UnitType[] unitTypes = new UnitType[0];
        public UnitType[] UnitTypes => unitTypes;
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
        

        public static Ruleset LoadRuleset(string path)
        {
            Ruleset ruleset = Resources.Load<Ruleset>(Path.Combine("Rulesets", path));
            for (int i = 0; i < ruleset.UnitTypes.Length; i++)
            {
                ruleset.UnitTypes[i].IdAndInitiative = i;
                ruleset.UnitTypes[i].SetGraphics();
            }
            if (ruleset is null) Debug.LogError($"Could not find ruleset {path}");
            return ruleset;
        }

        private void OnValidate()
        {
            for (int i = 0; i < unitTypes.Length; i++)
            {
                unitTypes[i].IdAndInitiative = i;
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