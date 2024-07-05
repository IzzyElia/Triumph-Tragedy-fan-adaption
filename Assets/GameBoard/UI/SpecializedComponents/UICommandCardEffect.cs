using System;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;
using HighlightState = GameSharedInterfaces.HighlightState;

namespace GameBoard.UI.SpecializeComponents
{
    public class UICommandCardEffect : UICardEffect
    {
        [SerializeField] private TextMeshProUGUI seasonText;
        [SerializeField] private TextMeshProUGUI mainText;
        [NonSerialized] public int NumActions;
        [NonSerialized] public int Initiative;
        [NonSerialized] public Season Season;
        public void SetCardTarget(int iCard)
        {
            IActionCard actionCard = GameState.GetCard(iCard, CardType.Action) as IActionCard;
            NumActions = actionCard.NumActions;
            Initiative = actionCard.Initiative;
            Season = actionCard.Season;
            
            switch (Season)
            {
                case Season.Spring:
                    seasonText.text = "<color=#5dba71>Spring</color>";
                    break;
                case Season.Summer:
                    seasonText.text = "<color=#ffe261>Summer</color>";
                    break;
                case Season.Fall:
                    seasonText.text = "<color=#f27052>Fall</color>";
                    break;
                case Season.Winter:
                    seasonText.text = "<color=#a7d0f2>Winter</color>";
                    break;
                default: throw new NotImplementedException();
            }
            
            mainText.text = $"{NumberToLetter(Initiative)}\n" +
                            $"{NumActions}";
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
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Global, CardPlayType.Command, Card.cardID);
            }
        }

        protected override HighlightState ShouldHighlight(CardplayInfo cardplayInfo)
        {
            if (Card.CardHand.CardsInPlayArea.Contains(Card))
            {
                if (GameState.GamePhase != GamePhase.SelectCommandCards) return HighlightState.Darken;
                if (cardplayInfo.TargetType == CardEffectTargetSelectionType.Global &&
                    cardplayInfo.iTarget == Card.cardID)
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
        
        
        static string NumberToLetter(int number)
        {
            switch (number)
            {
                case 0: return "A";
                case 1: return "B";
                case 2: return "C";
                case 3: return "D";
                case 4: return "E";
                case 5: return "F";
                case 6: return "G";
                case 7: return "H";
                case 8: return "I";
                case 9: return "J";
                case 10: return "K";
                case 11: return "L";
                case 12: return "M";
                case 13: return "N";
                case 14: return "O";
                case 15: return "P";
                case 16: return "Q";
                case 17: return "R";
                case 18: return "S";
                case 19: return "T";
                case 20: return "U";
                case 21: return "V";
                case 22: return "W";
                case 23: return "X";
                case 24: return "Y";
                case 25: return "Z";
                default: throw new ArgumentOutOfRangeException(nameof(number), "Value must be between 0 and 25.");
            }
        }
    }
}