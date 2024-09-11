using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializeComponents;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.UI;
using HighlightState = GameSharedInterfaces.HighlightState;

namespace GameBoard.UI
{
    [RequireComponent(typeof(Image))]
    public class UICard : UIComponent
    {
        public static T Create<T>(GameObject cardPrefab, UICardHand cardHand) where T : UICard
        {
            T uiCard = Instantiate(cardPrefab).GetComponent<T>();
            cardHand.UIController.RegisterUIComponent(uiCard);
            uiCard.transform.SetParent(cardHand.transform);
            uiCard.CardHand = cardHand;
            return uiCard;
        }
        
        void Awake()
        {
            if (BackdropImage is null) Debug.LogError($"Backdrop image unset in {name}");
            if (overlayImage is null) Debug.LogError($"Overlay image unset in {name}");
            if (outlineImage is null) Debug.LogError($"Outline image unset in {name}");
            overlayBaseColor = overlayImage.color;
            outlineBaseColor = outlineImage.color;
            if (!(this is UICardPreview))
            {
                if (cardEffectWrapper is not null) _cardEffectWrapperRectTransformProperties = new RectTransformProperties(cardEffectWrapper);
                if (cardMainEffectWrapper is not null) _cardMainEffectWrapperRectTransformProperties = new RectTransformProperties(cardMainEffectWrapper);
            }
        }
        
        [SerializeField] public RectTransform rectTransform;
        [SerializeField] protected Image BackdropImage;
        [SerializeField] private Image overlayImage;
        private Color overlayBaseColor;
        [SerializeField] private Image outlineImage;
        [SerializeField] private Color outlineImageHighlightColor;
        private Color outlineBaseColor;
        public RectTransform cardEffectWrapper;
        private RectTransformProperties _cardEffectWrapperRectTransformProperties;
        public RectTransform cardMainEffectWrapper;
        private RectTransformProperties _cardMainEffectWrapperRectTransformProperties;
        public List<UICardEffect> CardEffects = new List<UICardEffect>();
        private int _cardID;
        public CardType CardType;
        public ICard Card;
        public HighlightState HighlightState;
        public bool InPlayArea => CardHand.CardsInPlayArea.Contains(this);

        public int cardID
        {
            get => _cardID;
        }
        private bool held;
        [NonSerialized] public UICardHand CardHand;
        
        public void CalculateHighlightStatus(CardplayInfo cardplayInfo)
        {
            if (cardplayInfo.TargetType == CardEffectTargetSelectionType.None)
            {
                if (CardHand.HeldCard == this || CardHand.HoveredCard == this) HighlightState = HighlightState.Highlight;
                else HighlightState = HighlightState.Neutral;
            }
            else if (!CardHand.CardsInPlayArea.Contains(this))
            {
                HighlightState = HighlightState.Darken;
            }
            
            switch (HighlightState)
            {
                case HighlightState.Neutral:
                    outlineImage.color = outlineBaseColor;
                    overlayImage.color = overlayBaseColor;
                    break;
                case HighlightState.Darken:
                    outlineImage.color = outlineBaseColor;
                    overlayImage.color = new Color(overlayBaseColor.r, overlayBaseColor.g, overlayBaseColor.b, 0.5f);
                    break;
                case HighlightState.Highlight:
                    outlineImage.color = outlineImageHighlightColor;
                    overlayImage.color = overlayBaseColor;
                    break;
            }
        }

        protected void ClearCardEffects()
        {
            for (int i = 0; i < CardEffects.Count; i++)
            {
                CardEffects[i].DestroyUIComponent();
            }
            CardEffects.Clear();
        }
        protected UICardEffect InstantiateCardEffect(GameObject prefab, bool isMainEffect = false)
        {
            UICardEffect cardEffect = Instantiate(prefab).GetComponent<UICardEffect>();
            cardEffect.Card = this;
            UIController.RegisterUIComponent(cardEffect);
            if (isMainEffect)
            {
                cardEffect.transform.SetParent(cardMainEffectWrapper.transform);
            }
            else
            {
                cardEffect.transform.SetParent(cardEffectWrapper.transform);
            }
            CardEffects.Add(cardEffect);
            return cardEffect;
        }
        
        public virtual void Refresh(ICard gameCard)
        {
            if (gameCard != null)
            {
                _cardID = gameCard.ID;
                CardType = gameCard.CardType;
                Card = gameCard;
            }
        }

        public override void OnGamestateChanged()
        {
            // Handled by UICardHand
        }

        public override void OnResyncEnded()
        {
            //Handled by UICardHand
        }

        public void OnDroppedOnPanel()
        {
            foreach (var cardEffect in CardEffects)
            {
                cardEffect.OnDroppedOnPanel();
            }
        }


        public override void UIUpdate()
        {
            // If restoring animation, make sure to set SetAnchors() default of preserveSize to true
            //HandleAnimation()
        }
        private bool _animatingAreaSizes = false;
        private int _animationTimer = 0;
        private const int AnimationTime = 30;
        public void SetAnchors(RectTransform rectTransform, Vector2 newAnchorMin, Vector2 newAnchorMax, bool preserveSize = false)
        {
            var OriginalPosition = rectTransform.localPosition;
            var OriginalSize = rectTransform.sizeDelta;

            rectTransform.anchorMin = newAnchorMin;
            rectTransform.anchorMax = newAnchorMax;

            if (preserveSize)
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, OriginalSize.x);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, OriginalSize.y);
                rectTransform.localPosition = OriginalPosition;
            }
            


        }
        void LerpRect(RectTransform rectTransform, Vector2 preferredOffsetMin, Vector2 preferredOffsetMax, float t)
        {
            rectTransform.offsetMin = Vector2.Lerp(rectTransform.offsetMin, preferredOffsetMin, t);
            rectTransform.offsetMax = Vector2.Lerp(rectTransform.offsetMax, preferredOffsetMax, t);
        }
    }
}
