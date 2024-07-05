using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelDecisionManager : UIComponent, ICombatPanelAnimationParticipant
    {
        private static GameObject _dicePrefab;

        private CombatPanel _combatPanel;
        private CombatPanelDiceOption[] _diceOptions;
        private CombatPanelDiceOption _defaultDiceOption;
        private CombatPanelDie[] _dice = Array.Empty<CombatPanelDie>();
        private CombatPanelDie grabbedDie;
        private CombatPanelDie highlightedDie;

        public void Awake()
        {
            _diceOptions = GetComponentsInChildren<CombatPanelDiceOption>();
            for (int i = 0; i < _diceOptions.Length; i++)
            {
                if (_diceOptions[i].UnitCategory == UnitCategory.Ground)
                    _defaultDiceOption = _diceOptions[i];
            }

            if (_dicePrefab is null)
            {
                _dicePrefab = Resources.Load<GameObject>("Prefabs/CombatPanel/Dice");
            }
        }

        private int _lastUpdatedForStage = -1;
        private int _lastUpdatedDiceAvailable = -1;
        public void FullRefresh(CombatPanel combatPanel)
        {
            _combatPanel = combatPanel;
            RefreshDice();
            
            _lastUpdatedForStage = _combatPanel.ActiveCombat.StageCounter;
            _lastUpdatedDiceAvailable = _combatPanel.ActiveCombat.numDiceAvailable;
        }
        
        public void OnCombatStateUpdated()
        {
            if (_combatPanel.ActiveCombat.StageCounter != _lastUpdatedForStage ||
                _combatPanel.ActiveCombat.numDiceAvailable != _lastUpdatedDiceAvailable)
                RefreshDice();
            
            _lastUpdatedForStage = _combatPanel.ActiveCombat.StageCounter;
            _lastUpdatedDiceAvailable = _combatPanel.ActiveCombat.numDiceAvailable;
        }

        void RefreshDice()
        {
            Debug.Log("Refreshing dice");
            foreach (var die in _dice)
            {
                die.DestroyUIComponent();
            }

            if (_combatPanel.ActiveCombat.iPhasingPlayer == UIController.iPlayer)
            {
                _dice = new CombatPanelDie[_combatPanel.ActiveCombat.numDiceAvailable];
                for (int i = 0; i < _combatPanel.ActiveCombat.numDiceAvailable; i++)
                {
                    _dice[i] = CombatPanelDie.Create(prefab:_dicePrefab, decisionWrapper:this, startingPanel:_defaultDiceOption, startingPanelPositionIndex:i, uiController:UIController);
                }
                Debug.Log($"Created {_combatPanel.ActiveCombat.numDiceAvailable} dice");
            }
        }


        public CombatDiceDistribution GetDiceDistribution()
        {
            int airDice = 0;
            int groundDice = 0;
            int seaDice = 0;
            int subDice = 0;
            for (int i = 0; i < _dice.Length; i++)
            { ;
                switch (_dice[i].DroppedOnPanel.UnitCategory)
                {
                    case UnitCategory.Air:
                        airDice++;
                        break;
                    case UnitCategory.Ground:
                        groundDice++;
                        break;
                    case UnitCategory.Sea:
                        seaDice++;
                        break;
                    case UnitCategory.Sub:
                        subDice++;
                        break;
                    default: throw new NotImplementedException();
                }
            }
            return new CombatDiceDistribution
            {
                GroundDice = (ushort)groundDice, 
                AirDice = (ushort)airDice, 
                SeaDice = (ushort)seaDice, 
                SubDice = (ushort)subDice
            };
        }

        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
        }

        private Vector3 _grabDelta;
        private CombatPanelDiceOption _hoveredDiceOption;
        public override void UIUpdate()
        {
            CombatPanelDiceOption hoveredDiceOption = null;
            if (UIController.PointerInputStatus == InputStatus.Held)
            {
                if (grabbedDie is not null)
                {
                    grabbedDie.transform.position = UIController.PointerPositionOnScreen + _grabDelta;
                }

                if ((_hoveredDiceOption is null && UIController.UIDiceOptionUnderPointer is not null) || 
                    (_hoveredDiceOption is not null && UIController.UIDiceOptionUnderPointer != _hoveredDiceOption.gameObject))
                {
                    if (UIController.UIDiceOptionUnderPointer is null) hoveredDiceOption = null;
                    else hoveredDiceOption = UIController.UIDiceOptionUnderPointer.GetComponent<CombatPanelDiceOption>();
                    if (hoveredDiceOption is not null) hoveredDiceOption.OnHoveredStatusChanged(true);
                    if (_hoveredDiceOption is not null) _hoveredDiceOption.OnHoveredStatusChanged(false);
                    _hoveredDiceOption = hoveredDiceOption;
                }
            }
            else if (UIController.PointerInputStatus == InputStatus.Pressed)
            {
                if (UIController.UIObjectAtPointer.CompareTag("UICombatPanelDice"))
                {
                    grabbedDie = UIController.UIObjectAtPointer.GetComponent<CombatPanelDie>();
                    grabbedDie.transform.SetParent(_combatPanel.transform);
                    _grabDelta = grabbedDie.transform.position - UIController.PointerPositionOnScreen;
                }
            }
            else if (UIController.PointerInputStatus == InputStatus.Releasing)
            {
                if (grabbedDie is not null)
                {
                    if (UIController.UIDiceOptionUnderPointer is not null)
                    {
                        grabbedDie.transform.SetParent(UIController.UIDiceOptionUnderPointer.transform);
                        grabbedDie.DroppedOnPanel = UIController.UIDiceOptionUnderPointer.GetComponent<CombatPanelDiceOption>();
                        grabbedDie.transform.position = UIController.PointerPositionOnScreen + _grabDelta;
                        grabbedDie = null;
                    }
                    else
                    {
                        grabbedDie.transform.position = UIController.PointerPositionOnScreen + _grabDelta;
                        grabbedDie = null;
                    }

                    if (_hoveredDiceOption is not null)
                    {
                        _hoveredDiceOption.OnHoveredStatusChanged(false);
                        _hoveredDiceOption = null;

                    }
                }

                if (_hoveredDiceOption is not null)
                {
                    _hoveredDiceOption.OnHoveredStatusChanged(false);
                    _hoveredDiceOption = null;
                }
            }
        }

        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            
        }
    }
}