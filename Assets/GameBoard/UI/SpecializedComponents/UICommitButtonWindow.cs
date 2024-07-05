using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents
{
    public class UICommitButtonWindow : UIWindow
    {
        [SerializeField] private Button _button;
        public override void Start()
        {
            base.Start();
            _button.onClick.AddListener(OnClick);
            _button.interactable = true;
        }

        void OnClick()
        {
            if (GameState.GamePhase == GamePhase.InitialPlacement)
            {
                CommitInitialPlacement();
            }
            else if (GameState.GamePhase == GamePhase.Production)
            {
                CommitProduction();
            }
            else if (GameState.GamePhase == GamePhase.Diplomacy)
            {
                CommitCardPlay();
            }
            else if (GameState.GamePhase == GamePhase.SelectCommandCards)
            {
                CommitCardPlay();
            }
            else if (GameState.GamePhase == GamePhase.GiveCommands)
            {
                CommitCommands();
            }
            else if (GameState.GamePhase == GamePhase.CommitCombats)
            {
                CommitCombatCommittal();
            }
            else if (GameState.GamePhase == GamePhase.SelectSupport)
            {
                CommitCombatSupportSelection();
            }
            else if (GameState.GamePhase == GamePhase.SelectNextCombat)
            {
                throw new NotSupportedException("Next combat selection handled by the combat selection window");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void CommitCombatSupportSelection()
        {
            UIController.CombatSupportSelectionAction.Reset();
            foreach ((int iCadre, CombatOption combat) in UIController.CombatSelectionWindow.SupportUnitSelections)
            {
                UIController.CombatSupportSelectionAction.AddParameter(iCadre, combat.iTile);
            }
            UIController.CombatSupportSelectionAction.Send(OnCombatSupportSelectionReply);
            
            _button.interactable = false;
        }
        
        void OnCombatSupportSelectionReply(bool success)
        {
            if (success)
            {
                Debug.Log("WOOHOO!!!");
            }
            else
            {
                Debug.Log(":(");
            }

            _button.interactable = true;
        }
        
        void CommitCombatCommittal()
        {
            UIController.CombatCommittalAction.Reset();
            foreach (var combat in UIController.CombatSelectionWindow.SelectedCombats)
            {
                UIController.CombatCommittalAction.AddParameter(combat);
            }
            UIController.CombatCommittalAction.Send(OnCombatCommittalReply);
            
            _button.interactable = false;
        }

        void OnCombatCommittalReply(bool success)
        {
            if (success)
            {
                Debug.Log("WOOHOO!!!");
            }
            else
            {
                Debug.Log(":(");
            }

            _button.interactable = true;
        }
        
        void CommitCommands()
        {
            UIController.MovementAction.Send(OnCommandsReply);
            
            _button.interactable = false;
        }

        void OnCommandsReply(bool success)
        {
            if (success)
            {
                UIController.CardHand.DropCards(); // Reset the UI Status of the players hand
                UIController.CleanupAfterMovement();
                Debug.Log("WOOHOO!!!");
            }

            _button.interactable = true;
        }

        void CommitCardPlay()
        {
            UICardHand cardHand = UIController.CardHand;

            CardplayInfo cardplayInfo = (CardplayInfo)UIController.CardplayAction.GetParameters()[0];
            if (cardplayInfo.CardPlayType == CardPlayType.None) 
                UIController.CardplayAction.SetAllParameters(
                    new CardplayInfo(CardEffectTargetSelectionType.Global, CardPlayType.Pass, -1, Array.Empty<int>()));
            UIController.CardplayAction.Send(OnCardplayReply);
            
            _button.interactable = false;
        }

        void OnCardplayReply(bool success)
        {
            if (success)
            {
                UIController.CardHand.DropCards(); // Reset the UI Status of the players hand
                UIController.CleanupAfterMovement();
                Debug.Log("WOOHOO!!!");
            }

            _button.interactable = true;
        }

        void CommitProduction()
        {
            UIController.ProductionAction.Send(OnProductionReply);
            _button.interactable = false;
        }

        void OnProductionReply(bool success)
        {
            if (success)
            {
                foreach (MapCadre cadre in MapRenderer.MapCadresByID)
                {
                    if (cadre is not null) cadre.ProjectedPips = 0;
                }
                UIController.ProductionAction.Reset();
                Debug.Log("WOOHOO!!");
            }
            _button.interactable = true;
        }

        void CommitInitialPlacement()
        {
            // Parameters format is an array of (int iTile, byte unitType, int iCountry, byte pipsToAdd)[]
            List<(int iTile, byte unitType, int iCountry, byte pipsToAdd)> placements = new List<(int iTile, byte unitType, int iCountry, byte pipsToAdd)>();
            foreach (var placementGhost in UIController.InitialPlacementGhosts)
            {
                placements.Add((placementGhost.Tile.ID, (byte)placementGhost.UnitType.IdAndInitiative, placementGhost.MapCountry.ID, 1));
            }
            UIController.InitialPlacementAction.SetAllParameters(placements.ToArray());
            UIController.InitialPlacementAction.Send(OnInitialPlacementReply);
            _button.interactable = false;
        }

        void OnInitialPlacementReply(bool success)
        {
            if (success)
            {
                UIController.InitialPlacementAction.Reset();
                Debug.Log("WOOHOO!!!");
                UIController.DestroyInitialPlacementGhosts();
            }
            _button.interactable = true;
        }

        public override void OnGamestateChanged()
        {
            
        }

        public override void OnResyncEnded()
        {
            
        }

        public override void UIUpdate()
        {
            if (Active)
            {
                _button.interactable = !UIController.GameState.IsWaitingOnNetworkReply;
            }
        }

        public override bool WantsToBeActive => UIController.GameState.IsWaitingOnPlayer(UIController.iPlayer);
        protected override void OnActive()
        {
            _button.interactable = true;
        }

        protected override void OnHidden()
        {
            _button.interactable = false;
        }
    }
}