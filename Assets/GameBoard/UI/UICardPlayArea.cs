using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace GameBoard.UI
{
    public class UICardPlayArea : UIWindow
    {
        public override void Start()
        {
            base.Start();
            _cardPlayPanelPrefab = Resources.Load<GameObject>("Prefabs/CardPlayPanel");
            _diplomacySprite = Resources.Load<Sprite>("Graphics/CardPlayPanelBackgrounds/Diplomacy");
            _insurgentsSprite = Resources.Load<Sprite>("Graphics/CardPlayPanelBackgrounds/Insurgents");
            _commandsSprite = Resources.Load<Sprite>("Graphics/CardPlayPanelBackgrounds/Commands");
            _industrySprite = Resources.Load<Sprite>("Graphics/CardPlayPanelBackgrounds/Industry");
            _techSprite = Resources.Load<Sprite>("Graphics/CardPlayPanelBackgrounds/Tech");
        }

        private GameObject _cardPlayPanelPrefab;
        private Sprite _diplomacySprite;
        private Sprite _insurgentsSprite;
        private Sprite _industrySprite;
        private Sprite _techSprite;
        private Sprite _commandsSprite;
        private List<UICardPlayPanel> _cardPlayPanels = new List<UICardPlayPanel>();

        void SetCardPlayPanelsNumber(int number)
        {
            if (number < _cardPlayPanels.Count)
            {
                for (int i = number; i < _cardPlayPanels.Count; i++)
                {
                    _cardPlayPanels[i].DestroyUIComponent();
                }
                _cardPlayPanels.RemoveRange(number, _cardPlayPanels.Count - number);
            }
            else if (number > _cardPlayPanels.Count)
            {
                int initialCount = _cardPlayPanels.Count;
                for (int i = initialCount; i < number; i++)
                {
                    UICardPlayPanel panel = Instantiate(_cardPlayPanelPrefab).GetComponent<UICardPlayPanel>();
                    UIController.RegisterUIComponent(panel);
                    panel.transform.SetParent(this.transform);
                    _cardPlayPanels.Add(panel);
                }
            }
        }
        public void UpdateForCard(ICard card)
        {
            if (card is IActionCard actionCard)
            {
                if (GameState.GamePhase == GamePhase.Diplomacy)
                {
                    bool hasCountries = false;
                    bool hasInsurgents = false;
                    SetCardPlayPanelsNumber(1);
                    _cardPlayPanels[0].Image.sprite = _diplomacySprite;
                    _cardPlayPanels[0].CardPlayOption = CardPlayOption.Diplomacy;
                    //_cardPlayPanels[1].Image.sprite = _insurgentsSprite;
                    //_cardPlayPanels[1].CardPlayOption = CardPlayOption.Insurgents;
                }
                else if (GameState.GamePhase == GamePhase.SelectCommandCards)
                {
                    SetCardPlayPanelsNumber(1);
                    _cardPlayPanels[0].Image.sprite = _commandsSprite;
                    _cardPlayPanels[0].CardPlayOption = CardPlayOption.Commands;
                }
                else
                {
                    SetCardPlayPanelsNumber(0);
                }
            }
            else if (card is IInvestmentCard investmentCard)
            {
                if (GameState.GamePhase == GamePhase.Diplomacy)
                {
                    SetCardPlayPanelsNumber(2);
                    _cardPlayPanels[0].Image.sprite = _industrySprite;
                    _cardPlayPanels[0].CardPlayOption = CardPlayOption.Industry;
                    _cardPlayPanels[1].Image.sprite = _techSprite;
                    _cardPlayPanels[1].CardPlayOption = CardPlayOption.Tech;
                }
                else
                {
                    SetCardPlayPanelsNumber(0);
                }
            }
            else if (card == null)
            {
                SetCardPlayPanelsNumber(0);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public override void OnGamestateChanged()
        {
            
        }

        public override void OnResyncEnded()
        {
            
        }

        public override bool WantsToBeActive => true;
        protected override void OnActive()
        {
            
        }

        protected override void OnHidden()
        {
            
        }
    }
}