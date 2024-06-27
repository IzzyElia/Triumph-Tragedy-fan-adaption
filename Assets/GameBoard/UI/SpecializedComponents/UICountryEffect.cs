using System;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UICountryEffect : UICardEffect
    {
        [NonSerialized] public int iCountry;
        public void SetCountry(int country)
        {
            MapCountry mapCountry = UIController.MapRenderer.MapCountriesByID[country];
            this.iCountry = country;
            backgroundImage.sprite = mapCountry.FlagSprite;
            textMesh[0].text = mapCountry.name;
        }


        public override void OnActivated()
        {
            if (HighlightState == CardHighlightState.Highlight)
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.None, CardPlayType.None, -1);
            }
            else
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Country, CardPlayType.Diplomacy, iCountry);
            }
        }

        protected override CardHighlightState ShouldHighlight(CardplayInfo cardplayInfo)
        {
            if (Card.CardHand.CardsInPlayArea.Contains(Card))
            {
                if (GameState.GamePhase != GamePhase.Diplomacy) return CardHighlightState.Darken;
                if (cardplayInfo.TargetType == CardEffectTargetSelectionType.Country &&
                    cardplayInfo.iTarget == iCountry)
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