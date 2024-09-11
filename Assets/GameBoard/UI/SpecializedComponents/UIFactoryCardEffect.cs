using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HighlightState = GameSharedInterfaces.HighlightState;

namespace GameBoard.UI.SpecializeComponents
{
 public class UIFactoryCardEffect : UICardEffect
    {
        [SerializeField] private TextMeshProUGUI factoryText;
        [SerializeField] private Image background;
        public void SetCardTarget(int iCard)
        {
            IInvestmentCard investmentCard = GameState.GetCard(iCard, CardType.Investment) as IInvestmentCard;
            
            factoryText.text = $"Factory\nx{investmentCard.FactoryValue}";
        }
        
        
        public override void OnActivated()
        {
            return;
            /*
            if (HighlightState == HighlightState.Darken) return;
            if (HighlightState == HighlightState.Highlight)
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.None, CardPlayType.None, -1);
            }
            else
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Global, CardPlayType.Industry, -1);
            }
            */
        }

        public override void OnDroppedOnPanel()
        {
            if (Card.CardHand.TargetedCardPlayPanel.CardPlayOption == CardPlayOption.Industry) 
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Global, CardPlayType.Industry, -1);
        }

        protected override HighlightState ShouldHighlight(CardplayInfo cardplayInfo)
        {
            if (Card.CardHand.CardsInPlayArea.Contains(Card))
            {
                if (GameState.GamePhase != GamePhase.Diplomacy) return HighlightState.Darken;
                if (Card.CardHand.TargetedCardPlayPanel.CardPlayOption == CardPlayOption.Industry)
                {
                    return HighlightState.Highlight;
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