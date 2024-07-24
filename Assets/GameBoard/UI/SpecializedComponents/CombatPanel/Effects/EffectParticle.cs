using System;
using FMODUnity;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using UnityEditor;
using UnityEngine;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    public class EffectParticle : EffectObject
    {
        [SerializeField] private float lifetime;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private EventReference sound;

        [NonSerialized] public bool PlaySound = true;

        public override void OnCreate(CombatPanelEffect combatPanelEffect, CombatAnimationData animationData, AnimationTimeData timeData)
        {
            base.OnCreate(combatPanelEffect, animationData, timeData);
            if (PlaySound && Application.isPlaying) FMODUnity.RuntimeManager.PlayOneShot(sound, transform.position);
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