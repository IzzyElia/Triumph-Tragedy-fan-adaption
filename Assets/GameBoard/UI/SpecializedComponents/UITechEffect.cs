using GameSharedInterfaces.Triumph_and_Tragedy;

namespace GameBoard.UI.SpecializeComponents
{
using System;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UITechEffect : UICardEffect
    {
        [NonSerialized] public int iTech;
        public void SetTech(Tech tech)
        {
            if (tech == null) { Debug.LogError(tech is null); return; }
            backgroundImage.sprite = null;
            textMesh[0].text = tech.Name;
            iTech = tech.ID;
        }


        public override void OnActivated()
        {
            if (HighlightState == CardHighlightState.Darken) return;
            if (HighlightState == CardHighlightState.Highlight)
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.None, CardPlayType.None, -1);
            }
            else if (HighlightState == CardHighlightState.Darken)
            {
                return;
            }
            else
            {
                Card.CardHand.SetCardEffectSelection(CardEffectTargetSelectionType.Tech, CardPlayType.Tech, iTech);
            }
        }

        protected override CardHighlightState ShouldHighlight(CardplayInfo cardplayInfo)
        {
            if (GameState.GamePhase != GamePhase.SelectCommandCards) return CardHighlightState.Darken;
            if (Card.CardHand.CardsInPlayArea.Contains(Card))
            {
                if (cardplayInfo.TargetType == CardEffectTargetSelectionType.Tech &&
                    cardplayInfo.iTarget == iTech)
                {
                    return CardHighlightState.Highlight;
                }
                else if (cardplayInfo.TargetType == CardEffectTargetSelectionType.None && Card.CardHand.GetNumberOfTechInPlayArea(iTech) >= GameState.Ruleset.TechMatchesRequiredForTechUpgrade)
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
}