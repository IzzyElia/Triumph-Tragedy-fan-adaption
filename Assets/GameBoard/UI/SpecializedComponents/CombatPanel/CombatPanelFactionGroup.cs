using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelFactionGroup : UIComponent, ICombatPanelAnimationParticipant
    {
        private List<CombatPanelUnitGroup> _unitGroups = new List<CombatPanelUnitGroup>();
        
        public void FullRefresh(CombatPanel combatPanel, ITTGameState gameState, HashSet<UnitType> unitTypes, HashsetDictionary<UnitType, IGameCadre> cadres, MapFaction faction, CombatSide side)
        {
            int iUnitGroup = 0;
            for (int i = 0; i < gameState.Ruleset.unitTypes.Length; i++)
            {
                UnitType unitType = gameState.Ruleset.unitTypes[i];
                if (unitTypes.Contains(unitType))
                {
                    CombatPanelUnitGroup unitGroup;
                    if (iUnitGroup < _unitGroups.Count)
                    {
                        unitGroup = _unitGroups[iUnitGroup];
                        unitGroup.Refresh(gameState, unitType, cadres.Get_CertainOfKey(unitType), faction);
                    }
                    else
                    {
                        unitGroup = CombatPanelUnitGroup.Create(UIController, combatPanel, gameState, this, unitType, cadres.Get_CertainOfKey(unitType), faction, side);
                        _unitGroups.Add(unitGroup);
                    }
                    
                    iUnitGroup++;
                }
            }
            
            if (_unitGroups.Count > iUnitGroup)
            {
                for (int j = iUnitGroup; j < _unitGroups.Count; j++)
                {
                    _unitGroups[j].DestroyUIComponent();
                }
                _unitGroups.RemoveRange(iUnitGroup, _unitGroups.Count - iUnitGroup);
            }
            else if (iUnitGroup < _unitGroups.Count)
            {
                // TODO Is anything even needed here??
            }
        }
        
        public void OnCombatStateUpdated()
        {
            foreach (var unitGroup in _unitGroups)
            {
                unitGroup.OnCombatStateUpdated();
            }
        }

        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
        }

        public override void UIUpdate()
        {
        }

        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            
        }
    }
}