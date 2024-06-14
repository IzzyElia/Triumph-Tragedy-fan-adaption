using System;
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
    [Serializable] public class UnitType
    {
        public static UnitType Unknown = new UnitType("Unknown").SetGraphics();
        public string Name;
        public int GroundAttack;
        public int SeaAttack;
        public int AirAttack;
        public int SubAttack;
        [NonSerialized] public int IdAndInitiative = -1;
        public int Movement;
        public UnitCategory Category;
        public RebaseRule RebaseRule;
        public FireAndRetreatRule FireAndRetreatRule;
        public bool TakesDoubleHits;
        public bool IsFortress;
        public bool IsBuildableThroughNormalPlacementRules;
        public bool IsTransportType;
        public Texture2D Icon { get; private set; }
        public Sprite Sprite { get; private set; }

        public UnitType(string name,
            int groundAttack = 0,
            int seaAttack = 0,
            int airAttack = 0,
            int subAttack = 0,
            int movement = 0,
            UnitCategory category = default,
            RebaseRule rebaseRule = default,
            FireAndRetreatRule fireAndRetreatRule = FireAndRetreatRule.Cannot,
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
            Category = category;
            RebaseRule = rebaseRule;
            TakesDoubleHits = takesDoubleHits;
            IsFortress = isFortress;
            IsBuildableThroughNormalPlacementRules = isBuildableThroughNormalPlacementRules;
            IsTransportType = isTransportType;
        }

        public UnitType SetGraphics()
        {
            Icon = Resources.Load<Texture2D>($"Icons/Units/{Name}");
            if (Icon is null)
            {
                Debug.LogWarning($"No icon found for unit type {Name}");
                Icon = Resources.Load<Texture2D>("Icons/Units/Fallback");
            }
            Sprite = Resources.Load<Sprite>($"Icons/Units/{Name}");
            if (Sprite is null)
            {
                Debug.LogWarning($"No icon found for unit type {Name}");
                Sprite = Resources.Load<Sprite>("Icons/Units/Fallback");
            }

            return this;
        }
    }
}