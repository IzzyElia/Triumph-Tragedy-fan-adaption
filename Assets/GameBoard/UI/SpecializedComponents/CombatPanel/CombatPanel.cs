using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializeComponents.CombatPanel.Effects;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanel : UIWindow, ICombatPanelAnimationParticipant
    {
        enum RefreshQueueState
        {
            None,
            CombatStateUpdated,
            FullRefresh,
        }

        private RefreshQueueState _refreshQueueState = RefreshQueueState.None;
        private List<CombatPanelUnitGroup> _attackerPanels;
        private List<CombatPanelUnitGroup> _defenderPanels;
        [SerializeField] private CombatPanelFactionGroup attackerPanel;
        [SerializeField] private CombatPanelFactionGroup defenderPanel;
        [SerializeField] private CombatPanelDecisionManager decisionManager;
        [SerializeField] private GameObject PanelRenderer;
        [FormerlySerializedAs("backgroundDarkenerImage")] [SerializeField] private Image background;
        private Color _backgroundBaseColor;
        public IGameCombat ActiveCombat = null;
        public EffectsStageManager CombatScene;

        protected override void Awake()
        {
            base.Awake();
            _backgroundBaseColor = background.color;
        }

        public override void Start()
        {
            base.Start();
            UseDefaultWindowAppearanceAnimations = false;
            RegisterAnimationParticipant(attackerPanel);
            RegisterAnimationParticipant(defenderPanel);
            RegisterAnimationParticipant(decisionManager);
        }

        public void FullRefresh()
        {
            if (ActiveCombat == null) return;
            
            IGameCadre[] cadres = ActiveCombat.CalculateInvolvedCadreInterfaces();
            HashsetDictionary<UnitType, IGameCadre> attackers = new HashsetDictionary<UnitType, IGameCadre>();
            HashsetDictionary<UnitType, IGameCadre> defenders = new HashsetDictionary<UnitType, IGameCadre>();
            HashSet<UnitType> unitTypesInCombat = new HashSet<UnitType>();
            MapFaction attackerFaction = MapRenderer.MapFactionsByID[ActiveCombat.iAttackerFaction];
            MapFaction defenderFaction = MapRenderer.MapFactionsByID[ActiveCombat.iDefenderFaction];

            foreach (var unitType in GameState.Ruleset.unitTypes)
            {
                attackers.EnsureKey(unitType);
                defenders.EnsureKey(unitType);
            }

            foreach (var cadre in cadres)
            {
                UnitType unitType = cadre.UnitType;
                if (cadre.UnitType == null)
                {
                    Debug.LogError("Unrevealed units in combat. This shouldn't happen.");
                    unitType = UnitType.Unknown;
                }
                if (cadre.IFaction.ID == ActiveCombat.iAttackerFaction)
                {
                    attackers.Add_CertainOfKey(cadre.UnitType, cadre);
                }
                else if (cadre.IFaction.ID == ActiveCombat.iDefenderFaction)
                {
                    defenders.Add_CertainOfKey(cadre.UnitType, cadre);
                }
                else
                {
                    throw new InvalidOperationException("Cadre does not belong to either attackers or defenders. Something probably went wrong with CalculateInvolvedCadreInterfaces()");
                }

                unitTypesInCombat.Add(cadre.UnitType);
            }
            
            attackerPanel.FullRefresh(this, UIController.GameState, unitTypesInCombat, attackers, attackerFaction, CombatSide.Attacker);
            defenderPanel.FullRefresh(this, UIController.GameState, unitTypesInCombat, defenders, defenderFaction, CombatSide.Defender);
            decisionManager.FullRefresh(this);
        }

        public void OnCombatStateUpdated()
        {
            attackerPanel.OnCombatStateUpdated();
            defenderPanel.OnCombatStateUpdated();
            decisionManager.OnCombatStateUpdated();
        }

        public override void UIUpdate()
        {
            base.UIUpdate();
            if (AnimationOngoing)
            {
                AdvanceAnimation(Time.deltaTime);
            } 
            else
            {
                switch (_refreshQueueState)
                {
                    case RefreshQueueState.None:
                        break;
                    case RefreshQueueState.CombatStateUpdated:
                        OnCombatStateUpdated();
                        _refreshQueueState = RefreshQueueState.None;
                        break;
                    case RefreshQueueState.FullRefresh:
                        FullRefresh();
                        _refreshQueueState = RefreshQueueState.None;
                        break;
                    default: throw new NotImplementedException();
                }
            }
        }

        public override void OnGamestateChanged()
        {
            ActiveCombat = GameState.GetActiveCombat();
            if (ActiveCombat?.CombatUID != ActiveCombat?.CombatUID) _refreshQueueState = RefreshQueueState.FullRefresh;
            else if (ActiveCombat != null && _refreshQueueState != RefreshQueueState.FullRefresh) _refreshQueueState = RefreshQueueState.CombatStateUpdated;
            
            
        }

        public override void OnResyncEnded()
        {
            ActiveCombat = GameState.GetActiveCombat();
            _refreshQueueState = RefreshQueueState.FullRefresh;
        }

        public override bool WantsToBeActive => GameState.IsCombatHappening;
        protected override void OnActive()
        {
            PanelRenderer.SetActive(true);
            background.enabled = true;
            FullRefresh();
        }

        protected override void OnHidden()
        {
            PanelRenderer.SetActive(false);
            background.enabled = false;
        }
        
        
        // Combat animation system
        public float _darkenTime = 0.5f;
        public float TotalAnimationTime = 2.5f;
        [NonSerialized] public float AnimationTime = 0;
        public float AnimationProgress => TotalAnimationTime > 0 ? AnimationTime / TotalAnimationTime : 1;
        public bool AnimationOngoing => _animationData.Count > 0;
        private Queue<CombatAnimationData> _animationData = new ();
        private List<ICombatPanelAnimationParticipant> _animationParticipants = new ();
        private List<CombatPanelEffect> _activeEffects = new ();

        void AdvanceAnimation(float deltaTime)
        {
            CombatAnimationData currentAnimationData = _animationData.Peek();
            float halfAnimationTime = TotalAnimationTime / 2f;
            AnimationTimeData timeData = new AnimationTimeData()
            {
                Time = AnimationTime,
                TotalAnimationTime = TotalAnimationTime,
                DarkenProgress = Mathf.Clamp(
                    TotalAnimationTime - Mathf.Abs((AnimationTime  / _darkenTime) - TotalAnimationTime), 
                    0, 
                    1)
            };
            this.CombatAnimation(currentAnimationData, timeData);
            foreach (var animationParticipant in _animationParticipants)
            {
                animationParticipant.CombatAnimation(currentAnimationData, timeData);
            }

            if (AnimationTime >= TotalAnimationTime)
            {
                _animationData.Dequeue();
                AnimationTime = 0;
            }
            else
            {
                AnimationTime += deltaTime;
            }
        }

        public void QueueAnimation(CombatAnimationData animationData)
        {
            _animationData.Enqueue(animationData);
        }

        public void RegisterAnimationParticipant(ICombatPanelAnimationParticipant animationParticipant)
        {
            _animationParticipants.Add(animationParticipant);
        }
        
        public void DeregisterAnimationParticipant(ICombatPanelAnimationParticipant animationParticipant)
        {
            _animationParticipants.Remove(animationParticipant);
        }

        
        // Combat panel animation
        private MuzzleFlashData[] muzzleFlashes =
            new MuzzleFlashData[0];

        private HashSet<int> instantiatedMuzzleFlashes = new HashSet<int>();
        private struct MuzzleFlashData
        {
            public Vector3 position;
            public float size;
            public float startTime;
        }
        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            switch (timeData.AnimationState)
            {
                case AnimationState.FirstFrame:
                    int numFlashes = Random.Range(3, 15) * animationData.animatingRolls.Length;
                    muzzleFlashes = new MuzzleFlashData[numFlashes];
                    for (int i = 0; i < numFlashes; i++)
                    {
                        muzzleFlashes[i] = new MuzzleFlashData()
                        {
                            position = new Vector3(Random.value, Random.value, Random.value),
                            size = Random.value * 0.5f,
                            startTime = Random.value * (timeData.TotalAnimationTime - timeData.TotalAnimationTime * 0.2f)
                        };
                    }
                    instantiatedMuzzleFlashes.Clear();
                    break;
            }
            float projectileStartPoint = timeData.TotalAnimationTime / 2f;
            background.color = new Color(_backgroundBaseColor.r * timeData.DarkenProgress, _backgroundBaseColor.g * timeData.DarkenProgress, _backgroundBaseColor.b * timeData.DarkenProgress, _backgroundBaseColor.a);
            
            
            
            for (int i = 0; i < muzzleFlashes.Length; i++)
            {
                if (!instantiatedMuzzleFlashes.Contains(i) && muzzleFlashes[i].startTime <= timeData.Time)
                {
                    //MuzzleFlash muzzleFlash = MuzzleFlash.Create(this, animationData, timeData);
                } 
            }
        }

        public void RegisterCombatEffect(CombatPanelEffect effect)
        {
            _activeEffects.Add(effect);
        }
        
        public void DeregisterCombatEffect(CombatPanelEffect effect)
        {
            _activeEffects.Remove(effect);
        }
    }
}