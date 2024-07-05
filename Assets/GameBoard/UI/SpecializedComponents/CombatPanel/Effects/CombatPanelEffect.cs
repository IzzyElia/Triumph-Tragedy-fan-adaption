using System;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents.CombatPanel.Effects
{
    public abstract class CombatPanelEffect : MonoBehaviour
    {
        protected CombatPanel CombatPanel;
        protected CombatAnimationData AnimationData;
        
        protected static T Generate<T>(GameObject prefab, CombatPanel combatPanel, CombatAnimationData animationData)
            where T : CombatPanelEffect
        {
            T effect = Instantiate(prefab).GetComponent<T>();
            if (effect is null) throw new InvalidOperationException($"Prefab {prefab.name} does not contain a {typeof(T).Name} component");
            
            effect.CombatPanel = combatPanel;
            effect.AnimationData = animationData;
            effect.transform.SetParent(combatPanel.CombatScene.transform);
            combatPanel.RegisterCombatEffect(effect);

            return effect;
        }

        protected Vector3 Translate01ToStageBounds(Vector3 position)
        {
            return new Vector3(
                x: (position.x - 0.5f) * CombatPanel.CombatScene.effectAreaCamera.orthographicSize,
                (position.y - 0.5f) * CombatPanel.CombatScene.effectAreaCamera.orthographicSize,
                position.z * CombatPanel.CombatScene.effectAreaCamera.transform.localPosition.z);
        }
        protected abstract void UpdateAnimationState(AnimationTimeData timeData);

        public void Kill()
        {
            CombatPanel.DeregisterCombatEffect(this);
            Destroy(this.gameObject);
        }
    }
}