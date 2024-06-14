using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI
{
    public class UIInvestmentCard : UICard
    {
        public static UIInvestmentCard Create(int iCard, UICardHand hand, GameObject cardPrefab)
        {
            UIInvestmentCard card = Instantiate(hand.CardPreviewPrefab).GetComponent<UIInvestmentCard>();
            if (card is null) Debug.LogError("UIInvestmentCard prefab does not have a UIInvestmentCard component");
            card.Init(iCard:iCard, hand:hand, CardType.Investment);
            return card;
        }
    }

    public class UIActionCard : UICard
    {
        public static UIActionCard Create(int iCard, UICardHand hand, GameObject cardPrefab)
        {
            UIActionCard card = Instantiate(hand.CardPreviewPrefab).GetComponent<UIActionCard>();
            if (card is null) Debug.LogError("UIActionCard prefab does not have a UIActionCard component");
            card.Init(iCard:iCard, hand:hand, CardType.Action);
            
            return card;
        }
        
        // Utility
        static string NumberToLetter(byte number)
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

    public class UICardPreview : UICard
    {
        public static UICardPreview Create(UICardHand hand, CardType cardType)
        {
            UICardPreview card = Instantiate(hand.CardPreviewPrefab).GetComponent<UICardPreview>();
            if (card is null) Debug.LogError("UICardPreview prefab does not have a UICardPreview component");
            card.Init(iCard:-1, hand:hand, cardType);
            
            return card;
        }

        protected override void Refresh()
        {
            base.Refresh();
            switch (CardType)
            {
                case CardType.Action: BackdropImage.sprite = CardHand.ActionCardBack; return;
                case CardType.Investment: BackdropImage.sprite = CardHand.InvestmentCardBack; return;
                default: BackdropImage.sprite = CardHand.UniversalCardBack; Debug.LogError($"No card back for card type {CardType}"); return;
            }
        }
    }
    
    [RequireComponent(typeof(Image))]
    public class UICard : UIComponent
    {
        public void Init(int iCard, UICardHand hand, CardType cardType)
        {
            this._cardID = iCard;
            this.CardHand = hand;
            this.BackdropImage = GetComponent<Image>();
            Refresh();
        }
        
        [SerializeField] public RectTransform rectTransform;
        protected Image BackdropImage;
        [SerializeField] private List<TextMeshProUGUI> mainTexts;
        [SerializeField] private GameObject cardEffectWrapper;
        private List<GameObject> cardEffects;
        private int _cardID;
        public CardType CardType;
        public ICard Card;

        public int cardID
        {
            get => _cardID;
            set
            {
                _cardID = value;
                Refresh();
            }
        }
        private bool held;
        [NonSerialized] public UICardHand CardHand;
        
        
        protected virtual void Refresh()
        {
            if (_cardID != -1)
            {
                Card = MapRenderer.GameState.GetCard(_cardID, CardType);
            }
        }
        
        
        public override void OnGamestateChanged()
        {
            Refresh();
        }

        public override void OnResyncEnded()
        {
            Refresh();
        }

        public override void UIUpdate()
        {
            
        }
    }
}
