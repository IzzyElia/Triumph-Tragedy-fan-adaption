using System;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using UnityEngine;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    public abstract class EffectObject : MonoBehaviour
    {
        [NonSerialized] protected float StartTime;
        [NonSerialized] protected CombatAnimationData AnimationData;
        [NonSerialized] protected CombatPanelEffect CombatPanelEffect;

        public virtual void OnCreate(CombatPanelEffect combatPanelEffect, CombatAnimationData animationData, AnimationTimeData timeData)
        {
            AnimationData = animationData;
            StartTime = timeData.Time;
            CombatPanelEffect = combatPanelEffect;
            transform.SetParent(combatPanelEffect.Stage.transform);
            CombatPanelEffect.RegisterEffectObject(this);
        }
        
        public abstract void UpdateAnimationState(AnimationTimeData timeData);

        public virtual void Kill()
        {
            if (Application.isPlaying)
            {
                CombatPanelEffect.DeregisterEffectObject(this);
                Destroy(this.gameObject);
            }
            else
            {
                Kill_Editor();
            }
        }

        protected virtual void Kill_Editor()
        {
            CombatPanelEffect.DeregisterEffectObject(this);
            DestroyImmediate(this.gameObject);
        }
    }
}