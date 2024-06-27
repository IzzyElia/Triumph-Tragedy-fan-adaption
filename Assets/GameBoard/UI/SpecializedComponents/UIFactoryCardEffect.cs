using System;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;

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
            if (HighlightState == CardHighlightState.Darken) return;
            if (HighlightState == CardHighlightState.Highlight)
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.None, CardPlayType.None, -1);
            }
            else
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Global, CardPlayType.Industry, -1);
            }
        }

        protected override CardHighlightState ShouldHighlight(CardplayInfo cardplayInfo)
        {
            if (Card.CardHand.CardsInPlayArea.Contains(Card))
            {
                if (GameState.GamePhase != GamePhase.Diplomacy) return CardHighlightState.Darken;
                if (cardplayInfo.CardPlayType == CardPlayType.Industry)
                {
                    return CardHighlightState.Highlight;
                }
                else if (cardplayInfo.TargetType == CardEffectTargetSelectionType.None)
                {
                    return CardHighlightState.Neutral;
                }
                else
                {
                    return CardHighlightState.Darken;
                }
            }
            else
            {
                return CardHighlightState.Neutral;
            }
        }
    }
}