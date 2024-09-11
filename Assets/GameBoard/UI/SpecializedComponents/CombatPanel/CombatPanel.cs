using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializedComponents.CombatPanel.Effects;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
        public static int ResolvedCombatRollsClearedForCombat = -1;
        public static HashSet<uint> resolvedCombatRolls = new HashSet<uint>(); 
        // ^ NOTE THAT THIS IS STATIC ^ - This is so that combat animations only play once per machine, rather than per player,
        private Dictionary<int, ICombatPanelUnit> _combatPanelUnits = new Dictionary<int, ICombatPanelUnit>();

        public void RegisterCombatPanelUnit(int iCadre, ICombatPanelUnit combatPanelUnit)
        {
            if (!_combatPanelUnits.TryAdd(iCadre, combatPanelUnit)) _combatPanelUnits[iCadre] = combatPanelUnit;
        }
        public void DeregisterCombatPanelUnit(int iCadre)
        {
            _combatPanelUnits.Remove(iCadre);
        }

        void RefreshUnitPips()
        {
            foreach ((int iCadre, ICombatPanelUnit combatPanelUnit) in _combatPanelUnits)
            {
                IGameCadre gameCadre = GameState.GetCadre(iCadre);
                combatPanelUnit.SetPips(gameCadre.Pips);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            UseDefaultWindowAppearanceAnimations = false;
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

        public static bool ShowingFinalResult = false;
        public void FullRefresh()
        {
            if (ActiveCombat == null) return;
            
            IGameCadre[] cadres = ActiveCombat.CalculateInvolvedCadreInterfaces();
            Dictionary<UnitType, List<IGameCadre>> attackers = new Dictionary<UnitType, List<IGameCadre>>();
            Dictionary<UnitType, List<IGameCadre>> defenders = new Dictionary<UnitType, List<IGameCadre>>();
            HashSet<UnitType> unitTypesInCombat = new HashSet<UnitType>();
            MapFaction attackerFaction = MapRenderer.MapFactionsByID[ActiveCombat.iAttackerFaction];
            MapFaction defenderFaction = ActiveCombat.iDefenderFaction == -1 ? null : MapRenderer.MapFactionsByID[ActiveCombat.iDefenderFaction];

            foreach (var unitType in GameState.Ruleset.unitTypes)
            {
                attackers.Add(unitType, new List<IGameCadre>());
                defenders.Add(unitType, new List<IGameCadre>());
            }

            foreach (var cadre in cadres)
            {
                UnitType unitType = cadre.UnitType;
                if (cadre.UnitType == null)
                {
                    Debug.LogError("Unrevealed units in combat. This shouldn't happen.");
                    unitType = UnitType.Unknown;
                    continue;
                }
                if ((cadre.IFaction == null && ActiveCombat.iAttackerFaction == -1) || (cadre.IFaction != null && cadre.IFaction.ID == ActiveCombat.iAttackerFaction))
                {
                    attackers[cadre.UnitType].Add(cadre);
                }
                else if ((cadre.IFaction == null && ActiveCombat.iDefenderFaction == -1) || (cadre.IFaction != null && cadre.IFaction.ID == ActiveCombat.iDefenderFaction))
                {
                    defenders[cadre.UnitType].Add(cadre);
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
            
            attackerPanel.OnCombatStateUpdated();
            defenderPanel.OnCombatStateUpdated();
            decisionManager.OnCombatStateUpdated();
        }

        public void OnCombatStateUpdated()
        {
            if (ActiveCombat is null) return;
            
            attackerPanel.OnCombatStateUpdated();
            defenderPanel.OnCombatStateUpdated();
            decisionManager.OnCombatStateUpdated();

            foreach ((int iCadre, ICombatPanelUnit combatPanelUnit) in _combatPanelUnits)
            {
                IGameCadre gameCadre = GameState.GetCadre(iCadre);
                combatPanelUnit.SetPips(gameCadre.Pips);
            }
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

        private static int _lastResolvedCombatRollCount = 0;

        public void OnCloseButtonClicked()
        {
            ShowingFinalResult = false;
            ActiveCombat = GameState.GetActiveCombat();
            _refreshQueueState = RefreshQueueState.FullRefresh;
            UIController.UnresolvedStateChange = true;
        }
        public override void OnGamestateChanged()
        {
            IGameCombat gamestateActiveCombat = GameState.GetActiveCombat();
            if (gamestateActiveCombat?.CombatUID != ActiveCombat?.CombatUID)
            {
                if (!ShowingFinalResult)
                {
                    ActiveCombat = gamestateActiveCombat;
                    if (ActiveCombat != null && ResolvedCombatRollsClearedForCombat != ActiveCombat?.CombatUID)
                    {
                        ResolvedCombatRollsClearedForCombat = ActiveCombat.CombatUID;
                        resolvedCombatRolls.Clear();
                        _lastResolvedCombatRollCount = 0;
                    }
                    _refreshQueueState = RefreshQueueState.FullRefresh;
                }
            }
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
                    if (target is null) continue;
                    int iShooterFaction = shooter.MapCountry.Faction is null ? -1 : shooter.MapCountry.Faction.ID;
                    CombatAnimationResolveInfo shooterResolveGroup = new CombatAnimationResolveInfo()
                    {
                        Side = iShooterFaction == ActiveCombat.iAttackerFaction
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
        
        public override bool WantsToBeActive => ShowingFinalResult || ActiveCombat != null;
        protected override void OnActive()
        {
            PanelRenderer.SetActive(true);
            background.enabled = true;
            _refreshQueueState = RefreshQueueState.FullRefresh;
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
        public static float _darkenTime = 0.5f;
        public static float TotalAnimationTime => _activeEffect.TotalAnimationTime;
        [NonSerialized] public static float AnimationTime = 0;
        public static bool AnimationOngoing => _animationData.Count > 0;
        private static Queue<CombatAnimationData> _animationData = new ();
        private static List<ICombatPanelAnimationParticipant> _animationParticipants = new ();
        private static CombatPanelEffect _activeEffect;
        private static bool _firstFrame = true;
        void AdvanceAnimation(float deltaTime)
        {
            CombatAnimationData currentAnimationData = _animationData.Peek();
            
            AnimationState animationState;
            if (_firstFrame)
            {
                animationState = AnimationState.FirstFrame;
            }
            else if (_activeEffect is not null && AnimationTime >= TotalAnimationTime)
            {
                animationState = AnimationState.LastFrame;
            }
            else
            {
                animationState = AnimationState.Ongoing;
            }
            
            if (_firstFrame)
            {
                InitializeAnimation(currentAnimationData);
                _firstFrame = false;
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
        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            switch (timeData.AnimationState)
            {
                case AnimationState.FirstFrame:

                    break;
                    
            }
            background.color = new Color(_backgroundBaseColor.r * timeData.DarkenProgress, _backgroundBaseColor.g * timeData.DarkenProgress, _backgroundBaseColor.b * timeData.DarkenProgress, _backgroundBaseColor.a);
        }

        void InitializeAnimation(CombatAnimationData animationData)
        {
            if (_activeEffect is not null)
            {
                _activeEffect.Kill();
                _activeEffect = null;
            }
            
            MapFaction faction = animationData.firingSide == CombatSide.Attacker
                ? ActiveCombat.iAttackerFaction == -1 ? null : MapRenderer.MapFactionsByID[ActiveCombat.iAttackerFaction]
                : ActiveCombat.iDefenderFaction == -1 ? null : MapRenderer.MapFactionsByID[ActiveCombat.iDefenderFaction];
            
            EffectDefinition effectDefinition =
                animationData.firingUnitType.GetCombatEffectDefinition(faction is null ? null : faction.leader.name);
            
            if (effectDefinition is null) Debug.LogWarning($"No effect definition found for {animationData.firingUnitType.Name} ({faction.leader.name})");
            
            _activeEffect = CombatPanelEffect.Generate(CombatEffectStage, animationData, effectDefinition, _combatPanelUnits);
        }
    }
}