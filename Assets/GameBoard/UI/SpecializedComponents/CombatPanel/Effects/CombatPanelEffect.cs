using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    public class CombatPanelEffect : MonoBehaviour
    {
        public EffectsStageManager Stage;
        public CombatAnimationData AnimationData;
        public EffectDefinition EffectDefinition;
        public IReadOnlyDictionary<int, ICombatPanelUnit> CombatPanelUnits;
        [NonSerialized] public float TotalAnimationTime = 3f;
        private List<EffectObject> _effectObjects = new List<EffectObject>();
        public float burstEndTime = -1;

        public static CombatPanelEffect Generate(EffectsStageManager stage, CombatAnimationData animationData, EffectDefinition effectDefinition, IReadOnlyDictionary<int, ICombatPanelUnit> combatPanelUnits)
        {
            CombatPanelEffect effect = new GameObject("Combat Effect", typeof(CombatPanelEffect))
                .GetComponent<CombatPanelEffect>();

            if (effectDefinition is not null)
            {
                effect.Stage = stage;
                effect.AnimationData = animationData;
                effect.EffectDefinition = effectDefinition;
                effect.TotalAnimationTime = effectDefinition.baseAnimationTime +
                                            effectDefinition.animationTimePerUnit * animationData.animatingRolls.Length;
                if (combatPanelUnits is not null) effect.CombatPanelUnits = combatPanelUnits;
                else effect.CombatPanelUnits = new Dictionary<int, ICombatPanelUnit>();
                effect.transform.localPosition = Vector3.zero;
                stage.effectNearCamera.transform.localPosition = effectDefinition.NearCameraPosition;
            }

            return effect;
        }

        public Vector3 Translate01ToStageBounds(Vector3 position)
        {
            return new Vector3(
                x: (position.x - 0.5f) * Stage.effectAreaCamera.orthographicSize,
                (position.y - 0.5f) * Stage.effectAreaCamera.orthographicSize,
                position.z * Stage.effectAreaCamera.transform.position.z);
        }

        public float NormalizedDistance =>
            1 - (transform.localPosition.z / Stage.effectAreaCamera.transform.localPosition.z);

        public void UpdateAnimationState(AnimationTimeData timeData)
        {
            if (timeData.AnimationState == SpecializeComponents.CombatPanel.AnimationState.FirstFrame)
            {
                foreach (var effect in EffectDefinition.BaseEffects)
                {
                    InstantiateEffect(effect, timeData, isHit:false, null);
                }

                for (int i = 0; i < AnimationData.animatingRolls.Length; i++)
                {
                    CombatRoll roll = AnimationData.animatingRolls[i];
                    foreach (var effect in EffectDefinition.PerUnitEffects)
                    {
                        InstantiateEffect(effect, timeData, roll.IsHit, roll);
                    }
                }
            }

            foreach (var effectObject in new List<EffectObject>(_effectObjects))
            {
                effectObject.UpdateAnimationState(timeData);
            }
        }

        public void InstantiateEffect(EffectDefinition.Effect effect, AnimationTimeData timeData, bool isHit, CombatRoll? combatRoll)
        {
            int num = Random.Range(effect.MinNumber, effect.MaxNumber + 1);
            int hitNum = -1;
            if (isHit) hitNum = Random.Range(0, num);
            for (int i = 0; i < num; i++)
            {
                EffectUnit effectUnit = Instantiate(effect.unitPrefabs[Random.Range(0, effect.unitPrefabs.Length)]).GetComponent<EffectUnit>();
                effectUnit.StartPosition = Stage.transform.position + new Vector3(
                    EffectDefinition.SpawnBoxPosition.x + (Random.value - 0.5f) * EffectDefinition.SpawnBoxSize.x * 2f,
                    EffectDefinition.SpawnBoxPosition.y + (Random.value - 0.5f) * EffectDefinition.SpawnBoxSize.y * 2f,
                    EffectDefinition.SpawnBoxPosition.z + (Random.value - 0.5f) * EffectDefinition.SpawnBoxSize.z * 2f
                );
                effectUnit.EndPosition = Stage.transform.position + new Vector3(
                    EffectDefinition.DestinationBoxPosition.x + (Random.value - 0.5f) * EffectDefinition.SpawnBoxSize.x * 2f,
                    EffectDefinition.SpawnBoxPosition.y + (Random.value - 0.5f) * EffectDefinition.SpawnBoxSize.y * 2f,
                    EffectDefinition.SpawnBoxPosition.z + (Random.value - 0.5f) * EffectDefinition.SpawnBoxSize.z * 2f
                );
                effectUnit.HasHit = hitNum == i;
                effectUnit.CombatRoll = combatRoll;
                effectUnit.OnCreate(this, AnimationData, timeData);
            }
        }

        public void RegisterEffectObject(EffectObject effectObject)
        {
            _effectObjects.Add(effectObject);
        }

        public void DeregisterEffectObject(EffectObject effectObject)
        {
            _effectObjects.Remove(effectObject);
        }

        public void Kill()
        {
            foreach (var effectObject in new List<EffectObject>(_effectObjects))
            {
                effectObject.Kill();
            }
            Destroy(this.gameObject);
            Destroy(this);
        }

        public void Kill_Editor()
        {
            foreach (var effectObject in new List<EffectObject>(_effectObjects))
            {
                effectObject.Kill();
            }
            DestroyImmediate(this.gameObject);
        }
    }
}