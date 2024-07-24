using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelDecisionManager : UIComponent, ICombatPanelAnimationParticipant
    {
        private CombatPanel _combatPanel;
        private CombatPanelDiceOption[] _diceOptions;
        public CombatDiceDistribution SelectedDiceDistribution;
        [SerializeField] private Button commitButton;


        public void Awake()
        {
            _diceOptions = GetComponentsInChildren<CombatPanelDiceOption>();
            for (int i = 0; i < _diceOptions.Length; i++)
            {
                _diceOptions[i].DecisionManager = this;
            }

            commitButton.onClick.AddListener(OnCommitButtonClicked);
        }

        private int _lastUpdatedForStage = -1;
        private int _lastUpdatedDiceAvailable = -1;
        public void FullRefresh(CombatPanel combatPanel)
        {
            _combatPanel = combatPanel;
            
            _lastUpdatedForStage = _combatPanel.ActiveCombat.StageCounter;
            _lastUpdatedDiceAvailable = _combatPanel.ActiveCombat.numDiceAvailable;
            
            _removalQueue.Clear();
            SelectedDiceDistribution = default;
        }

        public void RefreshDiceOptions()
        {
            foreach (var diceOption in _diceOptions)
            {
                diceOption.OnDiceDistributionUpdated(SelectedDiceDistribution);
            }
        }
        
        public void OnCombatStateUpdated()
        {
            if (_combatPanel.ActiveCombat.StageCounter != _lastUpdatedForStage ||
                _combatPanel.ActiveCombat.numDiceAvailable != _lastUpdatedDiceAvailable)
            {
                FullRefresh(_combatPanel);
            }
            
            _lastUpdatedForStage = _combatPanel.ActiveCombat.StageCounter;
            _lastUpdatedDiceAvailable = _combatPanel.ActiveCombat.numDiceAvailable;
        }

        private List<UnitCategory> _removalQueue = new List<UnitCategory>();
        public void AdjustDice(UnitCategory unitCategory, short adjustment)
        {
            int diceAvailable = _combatPanel.ActiveCombat.numDiceAvailable;
            if (adjustment > 0)
            {
                if (adjustment > diceAvailable) adjustment = (short)diceAvailable;
                switch (unitCategory)
                {
                    case UnitCategory.Air:
                        SelectedDiceDistribution.AirDice += adjustment;
                        break;
                    case UnitCategory.Ground:
                        SelectedDiceDistribution.GroundDice += adjustment;
                        break;
                    case UnitCategory.Sea:
                        SelectedDiceDistribution.SeaDice += adjustment;
                        break;
                    case UnitCategory.Sub:
                        SelectedDiceDistribution.SubDice += adjustment;
                        break;
                }

                for (int i = 0; i < adjustment; i++)
                {
                    _removalQueue.Insert(0, unitCategory);
                }

                int excessDice = SelectedDiceDistribution.TotalDice - diceAvailable;
                for (int i = 0; i < excessDice; i++)
                {
                    if (_removalQueue.Count > 0)
                    {
                        UnitCategory category = _removalQueue[^1];
                        switch (category)
                        {
                            case UnitCategory.Air:
                                SelectedDiceDistribution.AirDice -= 1;
                                break;
                            case UnitCategory.Ground:
                                SelectedDiceDistribution.GroundDice -= 1;
                                break;
                            case UnitCategory.Sea:
                                SelectedDiceDistribution.SeaDice -= 1;
                                break;
                            case UnitCategory.Sub:
                                SelectedDiceDistribution.SubDice -= 1;
                                break;
                        }

                        _removalQueue.RemoveAt(_removalQueue.Count-1);
                    }
                    else throw new InvalidOperationException("dice removal queue empty");
                }
            }
            else if (adjustment < 0)
            {
                
                switch (unitCategory)
                {
                    case UnitCategory.Air:
                        if (SelectedDiceDistribution.AirDice + adjustment < 0)
                            adjustment = SelectedDiceDistribution.AirDice;
                        SelectedDiceDistribution.AirDice += adjustment;
                        break;
                    case UnitCategory.Ground:
                        if (SelectedDiceDistribution.GroundDice + adjustment < 0)
                            adjustment = SelectedDiceDistribution.GroundDice;
                        SelectedDiceDistribution.GroundDice += adjustment;
                        break;
                    case UnitCategory.Sea:
                        if (SelectedDiceDistribution.SeaDice + adjustment < 0)
                            adjustment = SelectedDiceDistribution.SeaDice;
                        SelectedDiceDistribution.SeaDice += adjustment;
                        break;
                    case UnitCategory.Sub:
                        if (SelectedDiceDistribution.SubDice + adjustment < 0)
                            adjustment = SelectedDiceDistribution.SubDice;
                        SelectedDiceDistribution.SubDice += adjustment;
                        break;
                }

                for (int i = 0; i < -adjustment; i++)
                {
                    _removalQueue.Remove(unitCategory);
                }
            }
            
            RefreshDiceOptions();
        }

        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
        }

        void OnCommitButtonClicked()
        {
            IPlayerAction combatDecisionAction =
                MapRenderer.GameState.GenerateClientsidePlayerActionByName("CombatDecisionAction");
            combatDecisionAction.SetAllParameters(SelectedDiceDistribution);
            combatDecisionAction.Send(CommitButtonCallback);
            commitButton.interactable = false;
        }

        void CommitButtonCallback(bool success)
        {
            commitButton.interactable = true;
        }

        private Vector3 _grabDelta;
        private CombatPanelDiceOption _hoveredDiceOption;
        public override void UIUpdate()
        {
            
        }

        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            foreach (var diceOption in _diceOptions)
            {
                diceOption.CombatAnimation(animationData, timeData);
            }
        }
    }
}