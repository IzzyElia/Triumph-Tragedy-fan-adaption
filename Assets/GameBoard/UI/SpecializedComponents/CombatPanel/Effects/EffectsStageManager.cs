using System;
using UnityEngine;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    public class EffectsStageManager : MonoBehaviour
    {
        public static EffectsStageManager Instance;
        public Camera effectAreaCamera;
        public Camera effectNearCamera;

        private void Awake()
        {
            if (Instance is not null) throw new InvalidOperationException("May only have on effects stage manager in the scene");
            Instance = this;
        }
    }
}