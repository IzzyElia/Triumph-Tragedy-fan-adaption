using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public enum EffectStyle
    {
        AirStrike,
        Bombardment,
        Gunfire,
    }
    [CreateAssetMenu(fileName = "New Combat Effect", menuName = "Game/Combat Effect", order = 1)]
    public class EffectDefinition : ScriptableObject
    {
        [SerializeField] public EffectStyle style;
        
        public List<Effect> BaseEffects;
        public List<Effect> PerUnitEffects;
        public Vector3 SpawnBoxPosition;
        public Vector3 SpawnBoxSize;
        public Vector3 DestinationBoxPosition;
        public Vector3 DestinationBoxSize;
        public Vector3 NearCameraPosition;
        
        [Serializable] public struct Effect
        {
            public int MinNumber;
            public int MaxNumber;
            public GameObject[] unitPrefabs;
        }
    }
}