using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UICardPreview : UICard
    {
        public override void Refresh(ICard card)
        {
            base.Refresh(card);
            switch (CardType)
            {
                case CardType.Action: BackdropImage.sprite = CardHand.ActionCardBack; break;
                case CardType.Investment: BackdropImage.sprite = CardHand.InvestmentCardBack; break;
                default: BackdropImage.sprite = CardHand.UniversalCardBack; Debug.LogError($"No card back for card type {CardType}"); return;
            }
        }
    }
}