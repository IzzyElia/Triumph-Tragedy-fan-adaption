using System;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;
using HighlightState = GameSharedInterfaces.HighlightState;

namespace GameBoard.UI.SpecializeComponents
{
 public class UIFactoryCardEffect : UICardEffect
    {
        [SerializeField] private TextMeshProUGUI factoryText;
        public void SetCardTarget(int iCard)
        {
            IInvestmentCard investmentCard = GameState.GetCard(iCard, CardType.Action) as IInvestmentCard;

            factoryText.text = $"{investmentCard.FactoryValue}";
        }
        
        
        public override void OnActivated()
        {
            if (HighlightState == HighlightState.Darken) return;
            if (HighlightState == HighlightState.Highlight)
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.None, CardPlayType.None, -1);
            }
            else
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Global, CardPlayType.Industry, -1);
            }
        }

        protected override HighlightState ShouldHighlight(CardplayInfo cardplayInfo)
        {
            if (Card.CardHand.CardsInPlayArea.Contains(Card))
            {
                if (GameState.GamePhase != GamePhase.Diplomacy) return HighlightState.Darken;
                if (cardplayInfo.CardPlayType == CardPlayType.Industry)
                {
                    return HighlightState.Highlight;
                }
                else if (cardplayInfo.TargetType == CardEffectTargetSelectionType.None)
                {
                    return HighlightState.Neutral;
                }
                else
                {
                    return HighlightState.Darken;
                }
            }
            else
            {
                return HighlightState.Neutral;
            }
        }
    }
}