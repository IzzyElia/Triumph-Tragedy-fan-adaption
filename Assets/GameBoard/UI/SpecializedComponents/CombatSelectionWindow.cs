using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard.MapMarkers;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class CombatSelectionWindow : UIWindow
    {
        private static GameObject _combatMarkerPrefab;
        private Dictionary<CombatOption, CombatMarker> _combatSelectionMarkers = new Dictionary<CombatOption, CombatMarker>();
        private HashsetDictionary<int, CombatOption> combatOptionsByTile = new HashsetDictionary<int, CombatOption>();
        public HashSet<CombatOption> SelectedCombats = new HashSet<CombatOption>();
        public CombatMarker CombatMarkerSelectedForSupport
        {
            get => UIController.CombatMarkerSelectedForSupport;
            set => UIController.CombatMarkerSelectedForSupport = value;
        }
        public Dictionary<int, CombatOption> SupportUnitSelections => UIController.SupportUnitSelections;
        private bool _waitingOnSelectionCallback = false;
        
        void Refresh()
        {
            if (_combatMarkerPrefab is null)
            {
                _combatMarkerPrefab = Resources.Load<GameObject>("Prefabs/MapMarkers/CombatMarker");
            }
            CombatOption[] combatOptions = GameState.GetCombatOptions();
            List<CombatOption> obsoletedCombatOptions = new List<CombatOption>();
            foreach ((CombatOption markerCombatOption, MapMarker mapMarker) in _combatSelectionMarkers)
            {
                if (!combatOptions.Contains(markerCombatOption))
                {
                    mapMarker.DestroyMapObject();
                    obsoletedCombatOptions.Add(markerCombatOption);
                }
            }

            foreach (var obsoletedCombatOption in obsoletedCombatOptions)
            {
                MapTile mapTile = MapRenderer.MapTilesByID[obsoletedCombatOption.iTile];
                combatOptionsByTile.Remove(obsoletedCombatOption.iTile, obsoletedCombatOption);
                _combatSelectionMarkers.Remove(obsoletedCombatOption);
                mapTile.CombatMarkerDarkenState = DarkenState.None;
            }

            foreach (var combatOption in combatOptions)
            {
                MapTile combatTile = MapRenderer.MapTilesByID[combatOption.iTile];
                CombatMarker combatMarker;
                if (_combatSelectionMarkers.TryGetValue(combatOption, out combatMarker))
                {
                    RefreshCombatMarkerStatus(combatMarker);
                    combatMarker.RecalculateSupportOptions();
                }
                else
                {
                    combatMarker = MapMarker.Create<CombatMarker>(MapRenderer, _combatMarkerPrefab);
                    combatMarker.CombatOption = combatOption;
                    _combatSelectionMarkers.Add(combatOption, combatMarker);
                    combatOptionsByTile.Add(combatOption.iTile, combatOption);
                    RefreshCombatMarkerStatus(combatMarker);
                    combatMarker.RecalculateSupportOptions();
                }
                combatMarker.transform.SetParent(combatTile.transform);
                combatMarker.transform.localPosition = new Vector3(0, 0, -0.55f);

                foreach (var mapCadre in MapRenderer.MapCadresByID)
                {
                    if (mapCadre is null) continue;
                    mapCadre.RecalculateArrowTarget();
                    mapCadre.RecalculateAppearance();
                }
                
                // TODO Handle multiple combat options in a single tile
            }
        }
        

        void SelectCombatOption(CombatOption combatOption)
        {
            SelectedCombats.Add(combatOption);
            MapTile mapTile = MapRenderer.MapTilesByID[combatOption.iTile];
            mapTile.RecalculateHighlighting();
            CombatMarker combatMarker = _combatSelectionMarkers[combatOption];
            RefreshCombatMarkerStatus(combatMarker);
        }

        void DeselectCombatOptionIfPossible(CombatOption combatOption)
        {
            if (SelectedCombats.Remove(combatOption))
            {
                bool allCombatOptionsDeselected = true;
                foreach (var combatOptionOnTile in combatOptionsByTile.Get(combatOption.iTile))
                {
                    if (SelectedCombats.Contains(combatOptionOnTile)) allCombatOptionsDeselected = false;
                }
                if (allCombatOptionsDeselected)
                {
                    MapTile mapTile = MapRenderer.MapTilesByID[combatOption.iTile];
                    mapTile.RecalculateHighlighting();
                    CombatMarker combatMarker = _combatSelectionMarkers[combatOption];
                    RefreshCombatMarkerStatus(combatMarker);
                }
            }
        }

        void RefreshCombatMarkerStatus(CombatMarker combatMarker)
        {
            MapTile mapTile = MapRenderer.MapTilesByID[combatMarker.CombatOption.iTile];
            if (GameState.GamePhase == GamePhase.SelectSupport)
            {
                combatMarker.SetStatus(CombatMarkerSelectedForSupport == combatMarker ? 
                    CombatMarkerStatus.SelectedForSupport : 
                    CombatMarkerStatus.Active);
                mapTile.CombatMarkerDarkenState =
                    CombatMarkerSelectedForSupport ? DarkenState.SupportHighlight : DarkenState.Combat;
            }
            else if (GameState.GamePhase == GamePhase.CommitCombats || GameState.GamePhase == GamePhase.SelectNextCombat)
            {
                combatMarker.SetStatus(SelectedCombats.Contains(combatMarker.CombatOption) ? 
                    CombatMarkerStatus.Active : 
                    CombatMarkerStatus.Inactive);
                mapTile.CombatMarkerDarkenState =
                    SelectedCombats.Contains(combatMarker.CombatOption) ? DarkenState.Combat : DarkenState.None;
            }
            mapTile.RecalculateHighlighting();
            foreach (var iCadre in combatMarker.SupportOptions)
            {
                MapCadre mapCadre = MapRenderer.MapCadresByID[iCadre];
                if (mapCadre is null) continue;
                mapCadre.RecalculateArrowTarget();
                mapCadre.RecalculateAppearance();
            }
        }

        public override void UIUpdate()
        {
            base.UIUpdate();
            if (GameState.GamePhase == GamePhase.CommitCombats)
            {
                if (UIController.PointerInputStatus == InputStatus.Pressed)
                {
                    CombatMarker combatMarkerAtPointer = UIController.HoveredMapObject as CombatMarker;
                    if (combatMarkerAtPointer is not null)
                    {
                        if(combatMarkerAtPointer.Status == CombatMarkerStatus.Active) DeselectCombatOptionIfPossible(combatMarkerAtPointer.CombatOption);
                        else SelectCombatOption(combatMarkerAtPointer.CombatOption);
                    }
                }
            }
            else if (GameState.GamePhase == GamePhase.SelectSupport)
            {
                MapCadre cadreAtPointer = UIController.HoveredMapObject as MapCadre;
                if (UIController.PointerInputStatus == InputStatus.Pressed)
                {
                    CombatMarker prev = CombatMarkerSelectedForSupport;
                    CombatMarker combatMarkerAtPointer = UIController.HoveredMapObject as CombatMarker;
                    if (combatMarkerAtPointer is not null)
                    {
                        if (combatMarkerAtPointer == CombatMarkerSelectedForSupport)
                        {
                            CombatMarkerSelectedForSupport = null;
                            RefreshCombatMarkerStatus(prev);
                        }
                        else
                        {
                            CombatMarkerSelectedForSupport = combatMarkerAtPointer;
                            if (prev is not null)
                            {
                                RefreshCombatMarkerStatus(prev);
                            }
                            if (CombatMarkerSelectedForSupport is not null)
                            {
                                RefreshCombatMarkerStatus(CombatMarkerSelectedForSupport);
                            }
                        }
                    }
                    else if (cadreAtPointer is not null)
                    {
                        if (GameState.CalculateAccessibleTiles(cadreAtPointer.ID, MoveType.Support).Contains(CombatMarkerSelectedForSupport.CombatOption.iTile))
                        {
                            if (SupportUnitSelections.TryGetValue(cadreAtPointer.ID,
                                    out CombatOption cadreCurrentlySupporting))
                            {
                                if (CombatMarkerSelectedForSupport.CombatOption.Equals(cadreCurrentlySupporting))
                                    SupportUnitSelections.Remove(cadreAtPointer.ID);
                                else SupportUnitSelections[cadreAtPointer.ID] = CombatMarkerSelectedForSupport.CombatOption;
                            }
                            else SupportUnitSelections.Add(cadreAtPointer.ID, CombatMarkerSelectedForSupport.CombatOption);
                        }
                        else SupportUnitSelections.Remove(cadreAtPointer.ID);
                    }
                }
            }
            else if (GameState.GamePhase == GamePhase.SelectNextCombat)
            {
                if (UIController.PointerInputStatus == InputStatus.Pressed && !_waitingOnSelectionCallback)
                {
                    CombatMarker combatMarkerAtPointer = UIController.HoveredMapObject as CombatMarker;
                    if (combatMarkerAtPointer is not null)
                    {
                        UIController.CombatSelectionAction.SetAllParameters(combatMarkerAtPointer.CombatOption);
                        _waitingOnSelectionCallback = true;
                        UIController.CombatSelectionAction.Send(CombatSelectionCallback);
                    }
                }
            }
        }

        void CombatSelectionCallback(bool success)
        {
            if (success)
            {
                Debug.Log("Sucessfully selected a combat");
            }

            _waitingOnSelectionCallback = false;
        }
        
        public override void OnGamestateChanged()
        {
            Refresh();
        }

        public override void OnResyncEnded()
        {
            Refresh();
        }

        public override bool WantsToBeActive => GameState.GamePhase == GamePhase.CommitCombats;
        protected override void OnActive()
        {
            Refresh();
        }

        protected override void OnHidden()
        {
            Refresh();
        }
    }
}