using System;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    [ExecuteInEditMode]
    public class EffectSimulator : MonoBehaviour
    {
        [SerializeField] private EffectDefinition effectDefinition;
        [SerializeField] private EffectsStageManager stage;
        [SerializeField] private float totalAnimationTime;
        [SerializeField] private CombatSide simulatedSide;
        [SerializeField] private float darkenTime;

        [SerializeField] private CombatPanelEffect _activeEffect = null;
        
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private double _activeAnimationTime;
        private double _lastTimeSinceStartup;
        private bool _firstFrame;
        private void OnEditorUpdate()
        {
            if (_activeEffect is not null)
            {
                SpecializeComponents.CombatPanel.AnimationState animationState =
                    SpecializeComponents.CombatPanel.AnimationState.Ongoing;
                if (_firstFrame)
                {
                    animationState = SpecializeComponents.CombatPanel.AnimationState.FirstFrame;
                    _firstFrame = false;
                }
                _activeAnimationTime += EditorApplication.timeSinceStartup - _lastTimeSinceStartup;
                _lastTimeSinceStartup = EditorApplication.timeSinceStartup;
                AnimationTimeData timeData = new AnimationTimeData()
                {
                    Time = (float)_activeAnimationTime,
                    TotalAnimationTime = totalAnimationTime,
                    DarkenProgress = Mathf.Clamp(
                        totalAnimationTime - Mathf.Abs(((float)_activeAnimationTime)  / darkenTime) - totalAnimationTime, 
                        0, 
                        1),
                    AnimationState = animationState
                };
                _activeEffect.UpdateAnimationState(timeData);
            }
        }

        public void Simulate()
        {
            CombatAnimationData simulatedAnimationData = new CombatAnimationData()
            {
                animatingRolls = new CombatRoll[0],
                firingSide = simulatedSide,
                firingTargetType = default,
                firingUnitType = default
            };
            _firstFrame = true;
            _activeEffect = CombatPanelEffect.Generate(stage, simulatedAnimationData, effectDefinition);
        }

        public void Stop()
        {
            if (_activeEffect is not null)
            {
                _activeEffect.Kill_Editor();
            }

            _activeEffect = null;
        }
    }

    [CustomEditor(typeof(EffectSimulator))]
    class EffectSimulatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EffectSimulator effectSimulator = (EffectSimulator)target;
            if (GUILayout.Button("Simulate"))
            {
                effectSimulator.Simulate();
            }
            if (GUILayout.Button("Stop"))
            {
                effectSimulator.Stop();
            }
        }
    }
}