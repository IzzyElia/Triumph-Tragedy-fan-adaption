using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializeComponents;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        public CardHighlightState HighlightState;
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
                if (CardHand.HeldCard == this || CardHand.HoveredCard == this) HighlightState = CardHighlightState.Highlight;
                else HighlightState = CardHighlightState.Neutral;
            }
            else if (!CardHand.CardsInPlayArea.Contains(this))
            {
                HighlightState = CardHighlightState.Darken;
            }
            
            switch (HighlightState)
            {
                case CardHighlightState.Neutral:
                    outlineImage.color = outlineBaseColor;
                    overlayImage.color = overlayBaseColor;
                    break;
                case CardHighlightState.Darken:
                    outlineImage.color = outlineBaseColor;
                    overlayImage.color = new Color(overlayBaseColor.r, overlayBaseColor.g, overlayBaseColor.b, 0.5f);
                    break;
                case CardHighlightState.Highlight:
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

        public void OnMovedToPlayArea()
        {
            /* Commented because it also needs to work for main effects
            if (CardHand.SelectingCardEffect)
            {
                SetAnchors(cardTextArea, new Vector2(cardTextArea.anchorMin.x, 1), new Vector2(cardTextArea.anchorMax.x, 1));
                SetAnchors(cardEffectWrapper, new Vector2(0, 0), new Vector2(1, 1));
                _animatingAreaSizes = true;
                _animationTimer = AnimationTime;
            }
            */
        }
        public void OnMovedToHand()
        {
            SetAnchors(cardMainEffectWrapper, _cardMainEffectWrapperRectTransformProperties.AnchorMin,
                _cardMainEffectWrapperRectTransformProperties.AnchorMax);
            SetAnchors(cardEffectWrapper, _cardEffectWrapperRectTransformProperties.AnchorMin, _cardEffectWrapperRectTransformProperties.AnchorMax);
            _animatingAreaSizes = true;
            _animationTimer = AnimationTime;
        }


        public override void UIUpdate()
        {
            // If restoring animation, make sure to set SetAnchors() default of preserveSize to true
            //HandleAnimation()
        }
        private bool _animatingAreaSizes = false;
        private int _animationTimer = 0;
        private const int AnimationTime = 30;
        void HandleAnimation()
        {
            if (_animatingAreaSizes)
            {
                float t = Time.deltaTime * CardHand.CardMovementSpeed;
                _animationTimer--;
                if (_animationTimer < 0)
                {
                    _animatingAreaSizes = false;
                    t = 1;
                }
                if (CardHand.CardsInPlayArea.Contains(this))
                {
                    LerpRect(cardMainEffectWrapper, 
                        new Vector2(
                            0,
                            0
                        ),
                        new Vector2(
                            0,
                            0
                        ),
                        t
                    );
                    LerpRect(cardEffectWrapper, 
                        new Vector2(
                            0,
                            0
                        ),
                        new Vector2(
                            0,
                            0
                        ),
                        t
                    );
                    
                }
                else
                {
                    LerpRect(cardMainEffectWrapper, 
                        _cardMainEffectWrapperRectTransformProperties.OffsetMin, 
                        _cardMainEffectWrapperRectTransformProperties.OffsetMax, t);
                    LerpRect(cardEffectWrapper, 
                        _cardEffectWrapperRectTransformProperties.OffsetMin, 
                        _cardEffectWrapperRectTransformProperties.OffsetMax,
                        t);
                }
            }
        }
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
