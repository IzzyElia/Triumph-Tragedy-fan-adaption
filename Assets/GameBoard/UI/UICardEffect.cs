using System;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI
{
    public abstract class UICardEffect : UIComponent
    {
        public Image outlineImage;
        public Color outlineImageHighlightColor;
        public Image backgroundImage;
        public TextMeshProUGUI[] textMesh = new TextMeshProUGUI[0];
        [NonSerialized] private Color baseColor;
        [NonSerialized] private Color outlineBaseColor;
        [NonSerialized] public UICard Card;
        protected CardHighlightState HighlightState;

        private void Awake()
        {
            if (backgroundImage is null) Debug.LogError($"Background Image is unset in Card Effect {name}");
            if (outlineImage is null) Debug.LogError($"Outline Image is unset in Card Effect {name}");
            if (textMesh is null) Debug.LogError($"Text mesh is unset in Card Effect {name}");
            baseColor = backgroundImage.color;
            outlineBaseColor = outlineImage.color;
        }

        public abstract void OnActivated();
        
        protected abstract CardHighlightState ShouldHighlight(CardplayInfo cardplayInfo);
        public void OnCardPlayOrSelectionStatusChanged(CardplayInfo cardplayInfo)
        {
            HighlightState = ShouldHighlight(cardplayInfo);
            switch (HighlightState)
            {
                case CardHighlightState.Neutral:
                    outlineImage.color = outlineBaseColor;
                    break;
                case CardHighlightState.Darken:
                    outlineImage.color = outlineBaseColor;
                    break;
                case CardHighlightState.Highlight:
                    outlineImage.color = outlineImageHighlightColor;
                    break;
            }
        }


        public override void OnGamestateChanged()
        {
            //Handled by UICardHand
        }

        public override void OnResyncEnded()
        {
            //Handled by UICardHand
        }

        public override void UIUpdate()
        {
            
            if (Card.CardHand.CardsInPlayArea.Contains(Card) && ((Card.CardHand.SelectingCardEffect && UIController.UICardEffectUnderPointer == this.gameObject) || HighlightState == CardHighlightState.Darken))
            {
                backgroundImage.color = baseColor * 0.6f;

            }
            else
            {
                backgroundImage.color = baseColor;
            }
        }
    }
}