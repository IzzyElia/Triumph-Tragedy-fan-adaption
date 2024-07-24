using System;
using System.Collections.Generic;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace GameSharedInterfaces
{
    public enum UnitCategory
    {
        Unspecified,
        Ground,
        Sea,
        Air,
        Sub
    }

    public enum FireAndRetreatRule
    {
        Cannot,
        Can
    }

    public enum RebaseRule
    {
        Unspecified,
        DoesntRebase,
        CanRebase,
        MustRebase
    }

    public class UnitTypeGraphicsSet
    {
        public Texture2D Texture;
        public Sprite Sprite;
        public Texture2D CombatPanelSceneTexture;
        public Sprite CombatPanelSceneSprite;
        public EffectDefinition CombatEffect;
    }
    [Serializable] public class UnitType
    {
        public static UnitType Unknown = new UnitType("Unknown").LoadUnitGraphics();
        public string Name;
        public int GroundAttack;
        public int SeaAttack;
        public int AirAttack;
        public int SubAttack;
        [NonSerialized] public int IdAndInitiative = -1;
        public int Movement;
        public int SupportRange;
        public int RedeploymentMovement;
        public UnitCategory Category;
        public RebaseRule RebaseRule;
        public FireAndRetreatRule FireAndRetreatRule;
        public bool HasFirstFire;
        public bool TakesDoubleHits;
        public bool IsFortress;
        public bool IsBuildableThroughNormalPlacementRules;
        public bool IsTransportType;
        public Dictionary<string, UnitTypeGraphicsSet> GraphicsSets = new Dictionary<string, UnitTypeGraphicsSet>();
        public UnitModifier[] TechModifiers;

        public UnitType(string name,
            int groundAttack = 0,
            int seaAttack = 0,
            int airAttack = 0,
            int subAttack = 0,
            int movement = 0,
            int supportRange = 0,
            UnitCategory category = default,
            RebaseRule rebaseRule = default,
            FireAndRetreatRule fireAndRetreatRule = FireAndRetreatRule.Cannot,
            bool hasFirstFire = false,
            bool takesDoubleHits = false,
            bool isFortress = false,
            bool isBuildableThroughNormalPlacementRules = true,
            bool isTransportType = false)
        {
            Name = name;
            GroundAttack = groundAttack;
            SeaAttack = seaAttack;
            AirAttack = airAttack;
            SubAttack = subAttack;
            Movement = movement;
            SupportRange = supportRange;
            RedeploymentMovement = movement * 3;
            Category = category;
            RebaseRule = rebaseRule;
            FireAndRetreatRule = fireAndRetreatRule;
            HasFirstFire = hasFirstFire;
            TakesDoubleHits = takesDoubleHits;
            IsFortress = isFortress;
            IsBuildableThroughNormalPlacementRules = isBuildableThroughNormalPlacementRules;
            IsTransportType = isTransportType;
        }

        [NonSerialized] private Dictionary<string, UnitTypeGraphicsSet> unitTypeTexturesByCountry;
        public UnitType LoadUnitGraphics()
        {
            unitTypeTexturesByCountry = new Dictionary<string, UnitTypeGraphicsSet>();
            unitTypeTexturesByCountry.Add(fallbackKey, new UnitTypeGraphicsSet());
            Texture2D[] combatPanelTextures = Resources.LoadAll<Texture2D>("Graphics/UnitBackdrops/");
            Sprite[] combatPanelSprites = Resources.LoadAll<Sprite>("Graphics/UnitBackdrops/");
            Texture2D[] unitIcons = Resources.LoadAll<Texture2D>("Icons/Units/");
            Sprite[] unitIconSprites = Resources.LoadAll<Sprite>("Icons/Units/");
            EffectDefinition[] unitCombatEffects = Resources.LoadAll<EffectDefinition>("Graphics/CombatEffects/");

            ApplyUnitGraphic(combatPanelTextures, (x, graphicsSet) => graphicsSet.CombatPanelSceneTexture = x);
            ApplyUnitGraphic(combatPanelSprites, (x, graphicsSet) => graphicsSet.CombatPanelSceneSprite = x);
            ApplyUnitGraphic(unitIcons, (x, graphicsSet) => graphicsSet.Texture = x);
            ApplyUnitGraphic(unitIconSprites, (x, graphicsSet) => graphicsSet.Sprite = x);
            ApplyUnitGraphic(unitCombatEffects, (x, graphicsSet) => graphicsSet.CombatEffect = x);

            return this;
        }

        private const string fallbackKey = "!fallback!";
        void ApplyUnitGraphic<T>(T[] resources, Action<T, UnitTypeGraphicsSet> apply) where T : UnityEngine.Object
        {
            for (int i = 0; i < resources.Length; i++)
            {
                string[] split = resources[i].name.Split('_');
                string graphicUnitTypeName = split[0];
                if (graphicUnitTypeName != this.Name) continue;
                string graphicCountry;
                if (split.Length > 1)
                {
                    graphicCountry = split[1];
                }
                else graphicCountry = fallbackKey;

                UnitTypeGraphicsSet graphicsSet;
                if (!unitTypeTexturesByCountry.TryGetValue(graphicCountry, out graphicsSet))
                {
                    graphicsSet = new UnitTypeGraphicsSet();
                    unitTypeTexturesByCountry.Add(graphicCountry, graphicsSet);
                }
                Debug.Log($"loaded {resources[i].name} as a {typeof(T).Name} for {graphicCountry}");
                apply.Invoke(resources[i], graphicsSet);
            }
        }

        public Texture2D GetIcon(string country)
        {
            if (unitTypeTexturesByCountry.TryGetValue(country, out UnitTypeGraphicsSet graphicsSet) &&
                graphicsSet.Texture is not null)
                return graphicsSet.Texture;
            else return unitTypeTexturesByCountry[fallbackKey].Texture;
        }
        public Sprite GetSprite(string country)
        {
            if (unitTypeTexturesByCountry.TryGetValue(country, out UnitTypeGraphicsSet graphicsSet) &&
                graphicsSet.Sprite is not null)
                return graphicsSet.Sprite;
            else return unitTypeTexturesByCountry[fallbackKey].Sprite;
        }
        public Texture2D GetCombatPanelTexture(string country)
        {
            if (unitTypeTexturesByCountry.TryGetValue(country, out UnitTypeGraphicsSet graphicsSet) &&
                graphicsSet.CombatPanelSceneTexture is not null)
                return graphicsSet.CombatPanelSceneTexture;
            else return unitTypeTexturesByCountry[fallbackKey].CombatPanelSceneTexture;
        }
        public Sprite GetCombatPanelSprite(string firstChoiceCountry, string secondChoiceCountry = null)
        {
            UnitTypeGraphicsSet graphicsSet;
            if (unitTypeTexturesByCountry.TryGetValue(firstChoiceCountry, out graphicsSet) &&
                    graphicsSet.CombatPanelSceneSprite is not null)
                return graphicsSet.CombatPanelSceneSprite;
            else if (unitTypeTexturesByCountry.TryGetValue(secondChoiceCountry, out graphicsSet) &&
                     graphicsSet.CombatPanelSceneSprite is not null)
                return graphicsSet.CombatPanelSceneSprite;
            else
                return unitTypeTexturesByCountry[fallbackKey].CombatPanelSceneSprite;
        }

        public EffectDefinition GetCombatEffectDefinition(string firstChoiceCountry, string secondChoiceCountry = null)
        {
            UnitTypeGraphicsSet graphicsSet;
            if (unitTypeTexturesByCountry.TryGetValue(firstChoiceCountry, out graphicsSet) &&
                graphicsSet.CombatEffect is not null)
                return graphicsSet.CombatEffect;
            else if (secondChoiceCountry != null && unitTypeTexturesByCountry.TryGetValue(secondChoiceCountry, out graphicsSet) &&
                     graphicsSet.CombatEffect is not null)
                return graphicsSet.CombatEffect;
            else
                return unitTypeTexturesByCountry[fallbackKey].CombatEffect;
        }
        

        public static UnitType GetModifiedUnitType(UnitType unitType, ICollection<int> iTechs)
        {
            foreach (var modifier in unitType.TechModifiers)
            {
                if (iTechs.Contains(modifier.iRequiredTech))
                {
                    unitType = modifier.Apply(unitType);
                }
            }

            return unitType;
        }
    }

    [Serializable]
    public struct UnitModifier
    {
        enum Effect
        {
            FirstFire,
            GroundAttack,
            NavelAttack,
            AirAttack,
            SubAttack,
            Movement
        }
        public int iRequiredTech;
        [SerializeField] Effect[] effects;

        public UnitType Apply(UnitType unitType)
        {
            foreach (var effect in effects)
            {
                switch (effect)
                {
                    case Effect.FirstFire:
                        unitType.HasFirstFire = true;
                        break;
                    case Effect.GroundAttack:
                        unitType.GroundAttack++;
                        break;
                    case Effect.NavelAttack:
                        unitType.SeaAttack++;
                        break;
                    case Effect.AirAttack:
                        unitType.AirAttack++;
                        break;
                    case Effect.SubAttack:
                        unitType.SubAttack++;
                        break;
                    case Effect.Movement:
                        unitType.Movement++;
                        break;
                    default: throw new NotImplementedException();
                }
            }

            return unitType;
        }
    }
}