using System;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI
{
    public class UICardDrawButton : UIWindow
    {
        [SerializeField] private Button _button;
        [SerializeField] private CardType _cardType;

        void OnClick()
        {
            ProductionActionData actionData = ProductionActionData.DrawCardAction(_cardType);
            (bool valid, string failureReason) =
                UIController.ProductionAction.TestParameter(actionData);
            if (valid)
            {
                UIController.ProductionAction.AddParameter(actionData);
                UIController.UnresolvedStateChange = true;
            }
            else
            {
                Debug.Log(failureReason);
            }
        }
        public override void OnRegistered()
        {
            base.OnRegistered();
            _button.onClick.AddListener(OnClick);
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

        public override bool WantsToBeActive => GameState.GamePhase == GamePhase.Production;
        protected override void OnActive()
        {
            
        }

        protected override void OnHidden()
        {
            
        }
    }
}