using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializeComponents;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameBoard.UI
{
    public enum CardFocusMode
    {
        Neutral,
        Effects,
        MainArea,
    }
    
    public enum CardHighlightState
    {
        Neutral,
        Highlight,
        Darken
    }
    //[ExecuteAlways]
    public class UICardHand : UIWindow
    {
        private void Start()
        {
            CardPreviewPrefab = Resources.Load<GameObject>("Prefabs/CardPreview");
            ActionCardPrefab = Resources.Load<GameObject>("Prefabs/ActionCard");
            ActionCardCountryEffectPrefab = Resources.Load<GameObject>("Prefabs/CardEffects/CountryCardEffect");
            ActionCardInsurgentEffectPrefab = Resources.Load<GameObject>("Prefabs/CardEffects/InsurgentCardEffect");
            ActionCardSpecialEffectPrefab = Resources.Load<GameObject>("Prefabs/CardEffects/SpecialActionCardEffect");
            ActionCardCommandEffectPrefab = Resources.Load<GameObject>("Prefabs/CardEffects/CommandCardEffect");
            InvestmentCardPrefab = Resources.Load<GameObject>("Prefabs/InvestmentCard");
            InvestmentCardTechEffectPrefab = Resources.Load<GameObject>("Prefabs/CardEffects/TechCardEffect");
            InvestmentCardSpecialEffectPrefab = Resources.Load<GameObject>("Prefabs/CardEffects/SpecialInvestmentCardEffect");
            InvestmentCardFactoryEffectPrefab = Resources.Load<GameObject>("Prefabs/CardEffects/FactoryCardEffect");
            ActionCardBack = Resources.Load<Sprite>("Graphics/ActionCardBack");
            InvestmentCardBack = Resources.Load<Sprite>("Graphics/InvestmentCardBack");
            ActionCardBack = Resources.Load<Sprite>("Graphics/ActionCardBack");
            UniversalCardBack = Resources.Load<Sprite>("Graphics/ActionCardBack");
        }

        public bool SelectingCardEffect;

        public GameObject CardPreviewPrefab;
        public GameObject ActionCardPrefab;
        public GameObject ActionCardCountryEffectPrefab;
        public GameObject ActionCardInsurgentEffectPrefab;
        public GameObject ActionCardSpecialEffectPrefab;
        public GameObject ActionCardCommandEffectPrefab;
        
        public GameObject InvestmentCardPrefab;
        public GameObject InvestmentCardTechEffectPrefab;
        public GameObject InvestmentCardSpecialEffectPrefab;
        public GameObject InvestmentCardFactoryEffectPrefab;
        public Sprite ActionCardBack;
        public Sprite InvestmentCardBack;
        public Sprite UniversalCardBack;

        private List<UICard> _cards = new ();
        public UICard HeldCard { get; private set; } = null;
        public UICard HoveredCard { get; private set; } = null;
        private List<UICard> CardsHeldInHand = new List<UICard>();
        [NonSerialized] public List<UICard> CardsInPlayArea = new List<UICard>();
        private CardPlayOption PonderingCardPlayOption => TargetedCardPlayPanel.CardPlayOption;
        [NonSerialized] public UICardPlayPanel TargetedCardPlayPanel = null; 
        public RectTransform relativeStartPoint;
        public RectTransform relativeEndPoint;
        public RectTransform activeCardPosition; // The position to place the selected card while targeting with it
        private bool _heldCardIsOutsideHandRegion = false; // if true then the held card is in targeting mode
        public float curveIntensity = 2f;
        public float rotationIntensity = 10f;
        public float hoveredSpread = 0.2f;
        public float playAreaCardScale = 1.7f;

        [FormerlySerializedAs("returnSpeed")] public float CardMovementSpeed = 1;


        private List<CardType> _queuedCards = new List<CardType>();
        public void RefreshCards()
        {
            List<ICard> cardData = GameState.GetCardsInHand(iPlayer);
            int queuedActionCards = 0;
            int queuedInvestmentCards = 0;
            
            for (int i = 0; i < cardData.Count; i++)
            {
                UICard uiCardAtIndex = _cards.Count > i ? _cards[i] : null;
                ICard gameCard = cardData[i];
                if (gameCard is IActionCard actionCard)
                {
                    UIActionCard uiCard;
                    if (uiCardAtIndex is UIActionCard uiActionCard)
                    {
                        uiCard = uiActionCard;
                    }
                    else
                    {
                        uiCard = UICard.Create<UIActionCard>(ActionCardPrefab, this);
                        _cards.Insert(i, uiCard);
                    }

                    uiCard.Refresh(gameCard);
                }
                else if (gameCard is IInvestmentCard investmentCard)
                {
                    UIInvestmentCard uiCard;
                    if (uiCardAtIndex is UIInvestmentCard uiInvestmentCard)
                    {
                        uiCard = uiInvestmentCard;
                    }
                    else
                    {
                        uiCard = UICard.Create<UIInvestmentCard>(InvestmentCardPrefab, this);
                        _cards.Insert(i, uiCard);
                    }

                    uiCard.Refresh(gameCard);
                }
            }
            
            if (GameState.GamePhase == GamePhase.Production)
            {

                ProductionActionData[] queuedProduction = Array.ConvertAll(UIController.ProductionAction.GetParameters(),
                    x => (ProductionActionData)x);
                for (int i = 0; i < queuedProduction.Length; i++)
                {
                    ProductionActionData actionData = queuedProduction[i];
                    if (actionData.ActionType == ProductionActionType.DrawCard)
                    {
                        switch (actionData.CardType)
                        {
                            case CardType.Action:
                                queuedActionCards++; 
                                break;
                            case CardType.Investment:
                                queuedInvestmentCards++;
                                break;
                        }
                    }
                }

                for (int i = 0; i < queuedActionCards; i++)
                {
                    int index = i + cardData.Count;
                    UICard uiCardAtIndex = _cards.Count > index ? _cards[index] : null;
                    UICardPreview uiCard;
                    if (uiCardAtIndex is UICardPreview)
                    {
                        uiCard = (UICardPreview)uiCardAtIndex;
                    }
                    else
                    {

                        uiCard = UICard.Create<UICardPreview>(CardPreviewPrefab, this);
                        _cards.Insert(index, uiCard);
                    }

                    uiCard.CardType = CardType.Action;
                    uiCard.Refresh(null);
                }
                
                for (int i = 0; i < queuedInvestmentCards; i++)
                {
                    int index = i + queuedActionCards + cardData.Count;
                    UICard uiCardAtIndex = _cards.Count > index ? _cards[index] : null;
                    UICardPreview uiCard;
                    if (uiCardAtIndex is UICardPreview)
                    {
                        uiCard = (UICardPreview)uiCardAtIndex;
                    }
                    else
                    {
                        uiCard = UICard.Create<UICardPreview>(CardPreviewPrefab, this);
                        _cards.Insert(index, uiCard);
                    }

                    uiCard.CardType = CardType.Investment;
                    uiCard.Refresh(null);
                }
            }

            int numCardsInHand = cardData.Count + queuedInvestmentCards + queuedActionCards;
            if (numCardsInHand > 0)
            {
                for (int i = numCardsInHand; i < _cards.Count; i++)
                {
                    CardsInPlayArea.Remove(_cards[i]);
                    _cards[i].DestroyUIComponent();
                }
            }
            int indexRange = _cards.Count - numCardsInHand;
            if (indexRange > 0) {
                _cards.RemoveRange(numCardsInHand, indexRange);
            }
            RecalculateCardsInHand();
        }
        public void SetCardEffectSelection(CardEffectTargetSelectionType selectionType, CardPlayType cardPlayType, int iTarget)
        {
            int[] iCards = new int[CardsInPlayArea.Count];
            for (int i = 0; i < iCards.Length; i++)
            {
                iCards[i] = CardsInPlayArea[i].cardID;
            }
            CardplayInfo cardplayInfo = new CardplayInfo(selectionType, cardPlayType, iTarget, iCards);
            UIController.CardplayAction.SetAllParameters(cardplayInfo);
            foreach (UICard card in _cards)
            {
                card.CalculateHighlightStatus(cardplayInfo);
                foreach (UICardEffect cardEffect in card.CardEffects)
                {
                    cardEffect.OnCardPlayOrSelectionStatusChanged(cardplayInfo);
                }
            }
        }
        public int GetNumberOfTechInPlayArea(int iTech)
        {
            int numMatches = 0;
            foreach (var card in CardsInPlayArea)
            {
                if (card.Card is IInvestmentCard investmentCard)
                {
                    foreach (var cardTech in investmentCard.Techs)
                    {
                        if (cardTech == iTech) numMatches++;
                        //break; //This break would cause only the first match to be counted (ie no duplicates)
                    }
                }
            }

            return numMatches;
        }
        public override void OnGamestateChanged()
        {
            RefreshCards();
        }

        public override void OnResyncEnded()
        {
            RefreshCards();
        }
        
        private bool _moreCardsAllowedInPlayArea = true;
        private UICard _clickedCard = null;
        private Vector2 _clickedCardPositionWhenClicked;

        public void DropCards()
        {
            CardsInPlayArea.Clear();
            if (HeldCard is not null)
            {
                HeldCard.OnMovedToHand();
                HeldCard = null;
            }
            TargetedCardPlayPanel = null;
            HeldCard = null;
            _clickedCard = null;
            _heldCardIsOutsideHandRegion = false;
            SelectingCardEffect = false;
            _moreCardsAllowedInPlayArea = true;
            UIController.CardPlayArea.UpdateForCard(null);
            RecalculateCardsInHand();
            CardplayInfo passCardplayInfo = new CardplayInfo(CardEffectTargetSelectionType.None, CardPlayType.None, -1, Array.Empty<int>());
            UIController.CardplayAction.SetAllParameters(passCardplayInfo);
            
            foreach (UICard card in _cards)
            {
                card.CalculateHighlightStatus(passCardplayInfo);
                foreach (UICardEffect cardEffect in card.CardEffects)
                {
                    cardEffect.OnCardPlayOrSelectionStatusChanged(passCardplayInfo);
                }
            }
        }
        public override void UIUpdate()
        { 
            base.UIUpdate();
            if (UIController.PointerInputStatus == InputStatus.Pressed && UIController.UICardUnderPointer is not null)
            {
                UICard cardUnderPointer = UIController.UICardUnderPointer.GetComponent<UICard>();
                
                if (!CardsInPlayArea.Contains(cardUnderPointer))
                {
                    HeldCard = cardUnderPointer;
                    if (TargetedCardPlayPanel is null) UIController.CardPlayArea.UpdateForCard(HeldCard.Card);
                }
                else
                {
                    _clickedCard = cardUnderPointer;
                    _clickedCardPositionWhenClicked = _clickedCard.transform.position;
                }
            }
            else if (UIController.PointerInputStatus == InputStatus.Releasing)
            {
                if (HeldCard is not null)
                {
                    if (UIController.UICardPlayPanelUnderPointer is not null && !CardsInPlayArea.Contains(HeldCard) && _moreCardsAllowedInPlayArea)
                    {
                        UICardPlayPanel PanelUnderPointer = UIController.UICardPlayPanelUnderPointer.GetComponent<UICardPlayPanel>();
                        bool cardsMatch = true;
                        if (TargetedCardPlayPanel is not null)
                        {
                            if (PonderingCardPlayOption == CardPlayOption.Tech && CardsInPlayArea.Count > 0)
                            {
                                IInvestmentCard heldInvestmentCard = (IInvestmentCard)HeldCard.Card;
                                cardsMatch = false;
                                foreach (var card in CardsInPlayArea)
                                {
                                    IInvestmentCard investmentCard = (IInvestmentCard)card.Card;
                                    foreach (var iTech in investmentCard.Techs)
                                    {
                                        if (heldInvestmentCard.Techs.Contains(iTech))
                                        {
                                            cardsMatch = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (cardsMatch)
                        {
                            UICardPlayPanel cardPlayPanel = UIController.UICardPlayPanelUnderPointer
                                .GetComponent<UICardPlayPanel>();
                            TargetedCardPlayPanel = cardPlayPanel;
                            CardsInPlayArea.Add(HeldCard);
                            RecalculateCardsInHand();
                            OnCardsInPlayAreaChanged(HeldCard, null);
                            HeldCard.OnMovedToPlayArea();
                        }
                    }

                    HeldCard = null;
                    if (CardsInPlayArea.Count == 0)
                    {
                        UIController.CardPlayArea.UpdateForCard(null);
                        TargetedCardPlayPanel = null;
                        _moreCardsAllowedInPlayArea = true;
                        SelectingCardEffect = false;
                        UIController.CardplayAction.SetAllParameters(
                            new CardplayInfo(CardEffectTargetSelectionType.None,
                            CardPlayType.None, -1, Array.Empty<int>()));
                    }
                }
                
                else if (UIController.UICardEffectUnderPointer is not null && UIController.DeltaDragSincePointerPressed < UIController.DragVsClickThreshold)
                {
                    UICardEffect cardEffectUnderPointer = UIController.UICardEffectUnderPointer.GetComponent<UICardEffect>();
                    cardEffectUnderPointer.OnActivated();
                }
                _heldCardIsOutsideHandRegion = false;
                _clickedCard = null;
            }
            else if (UIController.PointerInputStatus == InputStatus.Held)
            {
                if (_clickedCard is not null && UIController.DeltaDragSincePointerPressed >= UIController.DragVsClickThreshold)
                {
                    HeldCard = _clickedCard;
                    _clickedCard = null;
                }
                
                if (CardsInPlayArea.Contains(HeldCard) && UIController.UICardPlayPanelUnderPointer is null)
                {
                    CardsInPlayArea.Remove(HeldCard);
                    RecalculateCardsInHand();
                    OnCardsInPlayAreaChanged(null, HeldCard);
                    if (CardsInPlayArea.Count == 0)
                    {
                        UIController.CardPlayArea.UpdateForCard(null);
                        TargetedCardPlayPanel = null;
                        _moreCardsAllowedInPlayArea = true;
                        SelectingCardEffect = false;
                        UIController.CardplayAction.SetAllParameters(
                            new CardplayInfo(CardEffectTargetSelectionType.None,
                                CardPlayType.None, -1, Array.Empty<int>()));
                    }
                    HeldCard.OnMovedToHand();
                }
            }
            
            if (HeldCard is null && UIController.UICardUnderPointer is not null)
            {
                if (HoveredCard is null || HoveredCard.gameObject != UIController.UICardUnderPointer)
                {
                    HoveredCard = UIController.UICardUnderPointer.GetComponent<UICard>();
                }
            }
            else
            {
                HoveredCard = null;
            }

            if (HeldCard is not null && UIController.UICardRegionUnderPointer is null && !CardsInPlayArea.Contains(HeldCard))
            {
                _heldCardIsOutsideHandRegion = true;
            }
            else
            {
                _heldCardIsOutsideHandRegion = false;
            }
            
            HandleCardPositioning();
        }

        void OnCardsInPlayAreaChanged(UICard addedCard, UICard removedCard)
        {
            
            switch (TargetedCardPlayPanel.CardPlayOption)
            {
                case CardPlayOption.Diplomacy:
                    if (addedCard is not null)
                        SelectingCardEffect = true;
                    _moreCardsAllowedInPlayArea = false;
                    break;
                case CardPlayOption.Insurgents:
                    if (addedCard is not null)
                        SelectingCardEffect = true;
                    _moreCardsAllowedInPlayArea = false;
                    break;
                case CardPlayOption.Industry:
                    int industrySum = 0;
                    for (int q = 0; q < CardsInPlayArea.Count; q++)
                    {
                        industrySum += ((IInvestmentCard)CardsInPlayArea[q].Card).FactoryValue;
                    }

                    if (industrySum >= GameState.GetFaction(iPlayer).FactoriesNeededForIndustryUpgrade)
                        _moreCardsAllowedInPlayArea = false;
                    else
                        _moreCardsAllowedInPlayArea = true;
                    SelectingCardEffect = false;
                    break;
                case CardPlayOption.Tech:
                    if (addedCard is not null)
                        SelectingCardEffect = true;
                    bool hasMatchingSet = false;
                    foreach (var card in CardsInPlayArea)
                    {
                        IInvestmentCard investmentCard = card.Card as IInvestmentCard;
                        foreach (var tech in investmentCard.Techs)
                        {
                            if (GetNumberOfTechInPlayArea(tech) >= GameState.Ruleset.TechMatchesRequiredForTechUpgrade)
                            {
                                hasMatchingSet = true;
                            }
                        }
                    }
                    _moreCardsAllowedInPlayArea = !hasMatchingSet;
                    break;
                case CardPlayOption.Commands:
                    SelectingCardEffect = false;
                    _moreCardsAllowedInPlayArea = false;
                    break;
                default:
                    throw new NotImplementedException();
            }

            CardplayInfo cardplayInfo = (CardplayInfo)UIController.CardplayAction.GetParameters()[0];
            foreach (UICard card in _cards)
            {
                card.CalculateHighlightStatus(cardplayInfo);
                foreach (UICardEffect cardEffect in card.CardEffects)
                {
                    cardEffect.OnCardPlayOrSelectionStatusChanged(cardplayInfo);
                }
            }
        }
        void RecalculateCardsInHand()
        {
            CardsHeldInHand.Clear();
            for (int i = 0; i < _cards.Count; i++)
            {
                if (CardsInPlayArea.Contains(_cards[i])) continue;
                CardsHeldInHand.Add(_cards[i]);
            }
        }
        void HandleCardPositioning()
        {
            // Card positioning and logic
            Vector3 controlPoint = Vector3.Lerp(relativeStartPoint.position, relativeEndPoint.position, 0.5f) + new Vector3(0, curveIntensity, 0);
            int numCards = CardsHeldInHand.Count;
            int indexOfHeldCard = CardsHeldInHand.IndexOf(HeldCard);
            int indexOfHoveredCard = CardsHeldInHand.IndexOf(HoveredCard);

            for (int i = 0; i < numCards; i++)
            {
                if (indexOfHeldCard == i) continue;
                (Vector2 preferredPosition, float preferredRotationZ, float preferredScale) = PickCardPosition(i,
                    numCards: numCards,
                    indexOfHeldCard: indexOfHeldCard,
                    indexOfHoveredCard: indexOfHoveredCard,
                    controlPoint: controlPoint);
            
                LerpTowardsPosition(CardsHeldInHand[i], preferredPosition, preferredRotationZ, preferredScale);
            }

            int numCardsInPlayArea = CardsInPlayArea.Count;
            if (numCardsInPlayArea > 0)
            {
                float widthPerCard = CardsInPlayArea[0].rectTransform.rect.width;
                float totalWidth = widthPerCard * numCardsInPlayArea;
                for (int i = 0; i < numCardsInPlayArea; i++)
                {
                    UICard card = CardsInPlayArea[i];
                    if (HeldCard == card) continue;
                    if (numCardsInPlayArea == 1)
                    {
                        LerpTowardsPosition(card, TargetedCardPlayPanel.transform.position, 0, playAreaCardScale);
                    }
                    else
                    {
                        float offset = (-totalWidth / 2) + (i * widthPerCard);
                        Vector2 preferredPosition = new Vector2(
                            x: TargetedCardPlayPanel.RectTransform.position.x + offset,
                            y: TargetedCardPlayPanel.RectTransform.position.y);
                        LerpTowardsPosition(card, preferredPosition, 0, playAreaCardScale);
                    }
                }
            }

            if (HeldCard is not null)
            {
                Vector2 preferredPosition;
                if (_heldCardIsOutsideHandRegion) preferredPosition = activeCardPosition.position;
                else preferredPosition = UIController.PointerPositionOnScreen;
                LerpTowardsPosition(HeldCard, preferredPosition, 0, 1.5f, 3);
            }

            if (_clickedCard is not null)
            {
                Vector2 pointerDelta =
                    UIController.PointerPressedAtScreenPosition - UIController.PointerPositionOnScreen;
                Vector2 preferredPosition = Vector2.Lerp(_clickedCardPositionWhenClicked, _clickedCardPositionWhenClicked + pointerDelta,
                    0.2f);
                _clickedCard.transform.position = preferredPosition;
            }
        }

        void LerpTowardsPosition(UICard card, Vector2 preferredPosition, float preferredRotation, float preferredScale, float transformAndRotationSpeedFactor = 1)
        {
            Quaternion quaternion = Quaternion.Euler(0, 0, preferredRotation);
            card.rectTransform.localRotation = Quaternion.Lerp(
                card.rectTransform.localRotation,
                quaternion,
                Mathf.Min(CardMovementSpeed * transformAndRotationSpeedFactor * Time.deltaTime, 1)
            );
                
            card.rectTransform.position = Vector2.Lerp(card.rectTransform.position, 
                preferredPosition,
                Mathf.Min(CardMovementSpeed * transformAndRotationSpeedFactor * Time.deltaTime, 1));

            card.rectTransform.localScale = Vector3.Lerp(card.rectTransform.localScale,
                Vector3.one * preferredScale,
                Mathf.Min(CardMovementSpeed * Time.deltaTime * 1.5f, 1));
        }
        (Vector2 preferredPosition, float preferredRotation, float preferredScale) PickCardPosition(int i, int numCards, int indexOfHeldCard,
            int indexOfHoveredCard, Vector2 controlPoint)
        {
            if (indexOfHeldCard == i)
            {
                throw new InvalidOperationException();
            }
            else
            {
                if (indexOfHoveredCard == -1)
                {
                    float t = numCards <= 1 ? 0.5f : i / (float)(numCards - 1); // Normalize the step along the curve
                    // If the card is NOT being held (not being dragged)
                    return (CalculateBezierPoint(t, relativeStartPoint.position, controlPoint, relativeEndPoint.position),
                        Mathf.Lerp(rotationIntensity, -rotationIntensity, t),
                        1f);
                }
                else if (indexOfHoveredCard == i)
                {
                    float t = numCards <= 1 ? 0.5f : i / (float)(numCards - 1); // Normalize the step along the curve
                    // If the card is NOT being held (not being dragged)
                    Vector3 positionOffset = new Vector3(0, 60f);
                    return (positionOffset + CalculateBezierPoint(t, relativeStartPoint.position, controlPoint, relativeEndPoint.position),
                    Mathf.Lerp(rotationIntensity, -rotationIntensity, t),
                    1.5f);
                }
                else
                {
                    float t = numCards <= 1 ? 0.5f : i / (float)(numCards - 1);
                    float distance = Mathf.Abs(i - indexOfHoveredCard);
                    if (i > indexOfHoveredCard)
                    {
                        t += hoveredSpread * Mathf.Exp(-distance);
                    }
                    else
                    {
                        t -= hoveredSpread * Mathf.Exp(-distance);
                    }
                    t = Mathf.Clamp(t, 0, 1); 

                    return (CalculateBezierPoint(t, relativeStartPoint.position, controlPoint, relativeEndPoint.position),
                        Mathf.Lerp(rotationIntensity, -rotationIntensity, t),
                        1);
                }
            }
        }

        public override bool WantsToBeActive => true;
        protected override void OnActive()
        {
            Debug.LogWarning("OnActive needs implementation");
        }

        protected override void OnHidden()
        {
            throw new System.NotImplementedException();
        }
        
        // Utility Functions
        // One control point
        Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            Vector3 p = uu * p0; //first term
            p += 2 * u * t * p1; //second term
            p += tt * p2; //third term
            return p;
        }
        // Two control points
        Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            Vector3 p = uuu * p0; //first term
            p += 3 * uu * t * p1; //second term
            p += 3 * u * tt * p2; //third term
            p += ttt * p3; //fourth term
            return p;
        }
    }
}