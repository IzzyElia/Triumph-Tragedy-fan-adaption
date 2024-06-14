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
        private void Start()
        {
            _button.onClick.AddListener(OnClick);
            _button.interactable = true;
        }

        void OnClick()
        {
            if (GameState.GamePhase == GamePhase.InitialPlacement)
            {
                CommitInitialPlacement();
            }
        }

        void CommitInitialPlacement()
        {
            // Parameters format is an array of (int iTile, byte unitType, int iCountry, byte pipsToAdd)[]
            UIController.InitialPlacementAction.Reset();
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
                Debug.Log("WOOHOO!!!");
                UIController.DestroyInitialPlacementGhosts();
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