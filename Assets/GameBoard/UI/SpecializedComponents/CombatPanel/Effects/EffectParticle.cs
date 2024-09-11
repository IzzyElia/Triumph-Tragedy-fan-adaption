using System;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using UnityEngine;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    public class EffectParticle : EffectObject
    {
        [SerializeField] private float lifetime;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [NonSerialized] public bool PlaySound = true;

        public override void OnCreate(CombatPanelEffect combatPanelEffect, CombatAnimationData animationData, AnimationTimeData timeData)
        {
            base.OnCreate(combatPanelEffect, animationData, timeData);
        }

        public override void UpdateAnimationState(AnimationTimeData timeData)
        {
            float timeAlive = timeData.Time - this.StartTime;
            float t = timeAlive / lifetime;
            spriteRenderer.material.SetFloat("_T", t);
            
            if (timeAlive >= lifetime) Kill();
        }
    }
}