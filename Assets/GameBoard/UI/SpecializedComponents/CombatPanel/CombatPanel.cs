using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializedComponents.CombatPanel.Effects;
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
        public struct CombatAnimationResolveInfo
        {
            public UnitType UnitType;
            public CombatSide Side;
            public UnitCategory TargetCategory;

            public override int GetHashCode()
            {
                return HashCode.Combine(UnitType.IdAndInitiative.GetHashCode(), Side.GetHashCode(), TargetCategory.GetHashCode());
            }
    
            public static bool operator == (CombatAnimationResolveInfo left, CombatAnimationResolveInfo right)
            {
                return left.UnitType == right.UnitType && left.Side == right.Side && left.TargetCategory == right.TargetCategory;
            }
    
            public static bool operator != (CombatAnimationResolveInfo left, CombatAnimationResolveInfo right)
            {
                return !(left == right);
            }
    
            public override bool Equals(object obj)
            {
                if(obj is CombatAnimationResolveInfo)
                {
                    var other = (CombatAnimationResolveInfo)obj;      
                    return this == other;
                }
        
                return false;
            }
        }
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
        [NonSerialized] public EffectsStageManager CombatEffectStage;
        public static HashSet<uint> resolvedCombatRolls = new HashSet<uint>(); 
        // ^ NOTE THAT THIS IS STATIC ^ - Combat animations only play once per machine, rather than per player,
        // this is to avoid repeating animations in hotseat which get annoying

        protected override void Awake()
        {
            base.Awake();
            _backgroundBaseColor = background.color;
        }

        private void OnEnable()
        {
            CombatEffectStage = EffectsStageManager.Instance;
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
            ActiveCombat = GameState.GetActiveCombat();
            if (ActiveCombat == null) return;
            
            resolvedCombatRolls.Clear();
            
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

        private int _lastResolvedCombatRollCount = 0;
        public bool RefreshAllowed = true;
        public override void OnGamestateChanged()
        {
            IGameCombat gamestateActiveCombat = GameState.GetActiveCombat();
            if (gamestateActiveCombat?.CombatUID != ActiveCombat?.CombatUID && ActiveCombat == null) _refreshQueueState = RefreshQueueState.FullRefresh;
            else if (ActiveCombat != null) _refreshQueueState = RefreshQueueState.CombatStateUpdated;
            
            if (ActiveCombat is not null && ActiveCombat.CombatRolls.Count > _lastResolvedCombatRollCount)
            {
                _lastResolvedCombatRollCount = ActiveCombat.CombatRolls.Count;
                CombatAnimationResolveInfo currentResolveGroup = default;
                List<CombatRoll> combatRollGroup = new List<CombatRoll>();
                foreach (var unresolvedRoll in GetUnresolvedCombatRollsAndMarkAsResolved())
                {
                    MapCadre shooter = MapRenderer.GetCadreByID(unresolvedRoll.iShooter);
                    MapCadre target = MapRenderer.GetCadreByID(unresolvedRoll.iTarget);
                    CombatAnimationResolveInfo shooterResolveGroup = new CombatAnimationResolveInfo()
                    {
                        Side = shooter.MapCountry.faction.ID == ActiveCombat.iAttackerFaction
                            ? CombatSide.Attacker
                            : CombatSide.Defender,
                        UnitType = shooter.UnitType,
                        TargetCategory = target.UnitType.Category
                    };
                    if (currentResolveGroup == default) currentResolveGroup = shooterResolveGroup;
                    if (shooterResolveGroup == currentResolveGroup)
                    {
                        combatRollGroup.Add(unresolvedRoll);
                    }
                    else
                    {
                        CombatAnimationData animationData = new CombatAnimationData()
                        {
                            animatingRolls = combatRollGroup.ToArray(),
                            firingSide = currentResolveGroup.Side,
                            firingTargetType = currentResolveGroup.TargetCategory,
                            firingUnitType = currentResolveGroup.UnitType
                        };
                        QueueAnimation(animationData);
                        currentResolveGroup = default;
                        combatRollGroup.Clear();
                    }
                }

                if (currentResolveGroup != default)
                {
                    CombatAnimationData animationData = new CombatAnimationData()
                    {
                        animatingRolls = combatRollGroup.ToArray(),
                        firingSide = currentResolveGroup.Side,
                        firingTargetType = currentResolveGroup.TargetCategory,
                        firingUnitType = currentResolveGroup.UnitType
                    };
                    QueueAnimation(animationData);
                }
            }
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

        List<CombatRoll> GetUnresolvedCombatRollsAndMarkAsResolved()
        {
            List<CombatRoll> rolls = new List<CombatRoll>();
            foreach (var roll in ActiveCombat.CombatRolls)
            {
                if (resolvedCombatRolls.Contains(roll.UID)) continue;
                rolls.Add(roll);
                resolvedCombatRolls.Add(roll.UID);
            }

            return rolls;
        }
        
        
        // Combat animation system
        public float _darkenTime = 0.5f;
        public float TotalAnimationTime = 7f;
        [NonSerialized] public float AnimationTime = 0;
        public float AnimationProgress => TotalAnimationTime > 0 ? AnimationTime / TotalAnimationTime : 1;
        public bool AnimationOngoing => _animationData.Count > 0;
        private Queue<CombatAnimationData> _animationData = new ();
        private List<ICombatPanelAnimationParticipant> _animationParticipants = new ();
        private CombatPanelEffect _activeEffect;
        private bool _firstFrame = true;
        void AdvanceAnimation(float deltaTime)
        {
            CombatAnimationData currentAnimationData = _animationData.Peek();
            float halfAnimationTime = TotalAnimationTime / 2f;
            AnimationState animationState;
            if (_firstFrame)
            {
                animationState = AnimationState.FirstFrame;
            }
            else if (AnimationTime >= TotalAnimationTime)
            {
                animationState = AnimationState.LastFrame;
            }
            else
            {
                animationState = AnimationState.Ongoing;
            }
            
            AnimationTimeData timeData = new AnimationTimeData()
            {
                Time = AnimationTime,
                TotalAnimationTime = TotalAnimationTime,
                DarkenProgress = Mathf.Clamp(
                    TotalAnimationTime - Mathf.Abs((AnimationTime  / _darkenTime) - TotalAnimationTime), 
                    0, 
                    1),
                AnimationState = animationState
            };

            if (_firstFrame)
            {
                InitializeAnimation(currentAnimationData, timeData);
                _firstFrame = false;
            }
            
            this.CombatAnimation(currentAnimationData, timeData);
            foreach (var animationParticipant in _animationParticipants)
            {
                animationParticipant.CombatAnimation(currentAnimationData, timeData);
            }
            if (_activeEffect is not null) 
                _activeEffect.UpdateAnimationState(timeData);

            if (AnimationTime >= TotalAnimationTime)
            {
                _animationData.Dequeue();
                AnimationTime = 0;
                _firstFrame = true;
                if (_activeEffect is not null)
                {
                    _activeEffect.Kill();
                    _activeEffect = null;
                }
            }
            else
            {
                AnimationTime += deltaTime;
            }
            Debug.Log($"Animation time:{AnimationTime}");
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

                    break;
                    
            }
            background.color = new Color(_backgroundBaseColor.r * timeData.DarkenProgress, _backgroundBaseColor.g * timeData.DarkenProgress, _backgroundBaseColor.b * timeData.DarkenProgress, _backgroundBaseColor.a);
        }

        void InitializeAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            if (_activeEffect is not null)
            {
                _activeEffect.Kill();
                _activeEffect = null;
            }
            
            MapFaction faction = animationData.firingSide == CombatSide.Attacker
                ? MapRenderer.MapFactionsByID[ActiveCombat.iAttackerFaction]
                : MapRenderer.MapFactionsByID[ActiveCombat.iDefenderFaction];
            
            EffectDefinition effectDefinition =
                animationData.firingUnitType.GetCombatEffectDefinition(faction.leader.name);
            
            if (effectDefinition is null) Debug.LogWarning($"No effect definition found for {animationData.firingUnitType.Name} ({faction.leader.name})");
            
            _activeEffect = CombatPanelEffect.Generate(CombatEffectStage, animationData, effectDefinition);
        }
    }
}