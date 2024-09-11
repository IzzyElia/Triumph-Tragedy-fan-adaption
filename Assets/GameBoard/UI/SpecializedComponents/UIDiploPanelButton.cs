using System;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIDiploPanelButton : UIComponent
    {
        [NonSerialized] public UIDiploPanel DiploPanel;
        [SerializeField] private DiploPanelAction action;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private string baseText;
        [SerializeField] private Sprite baseSprite;
        private Color _backgroundBaseColor;
        private Color _backgroundActiveColor;

        private void Awake()
        {
            _backgroundBaseColor = background.color;
        }

        public override void OnGamestateChanged()
        {
            
        }

        public override void OnResyncEnded()
        {
            
        }

        public void OnClick()
        {
            MapCountry selectedCountry = DiploPanel.SelectedCountry;
            if (selectedCountry is null) return;
            switch (action)
            {
                case DiploPanelAction.DeclareWar:
                    if (selectedCountry.Faction is not null && selectedCountry.Faction != UIController.PlayerMapFaction)
                    {
                        if (_isActivatedInContext) DiploPanel.consideringFactionWars.Remove(selectedCountry.Faction.ID);
                        else DiploPanel.consideringFactionWars.Add(selectedCountry.Faction.ID);
                    }
                    else if (selectedCountry.Faction is null)
                    {
                        if (_isActivatedInContext) DiploPanel.consideringNeutralWars.Remove(selectedCountry.ID);
                        else DiploPanel.consideringNeutralWars.Add(selectedCountry.ID);
                    }
                    break;
                default: throw new NotImplementedException();
            }
            
            RefreshImageAndUndoState();
        }

        private bool _isActivatedInContext; // true when the diplomatic action is set to happen already (ie when the selected country is already a war target)
        public void RefreshImageAndUndoState()
        {
            MapCountry selectedCountry = DiploPanel.SelectedCountry;
            if (selectedCountry is null) return;
            switch (action)
            {
                case DiploPanelAction.DeclareWar:
                    if (selectedCountry.Faction is not null && selectedCountry.Faction != UIController.PlayerMapFaction)
                    {
                        _isActivatedInContext = DiploPanel.consideringFactionWars.Contains(selectedCountry.Faction.ID);
                    }
                    else if (selectedCountry.Faction is null)
                    {
                        _isActivatedInContext = DiploPanel.consideringNeutralWars.Contains(selectedCountry.ID);
                    }
                    break;
                default: throw new NotImplementedException();
            }

            if (_isActivatedInContext)
            {
                _backgroundActiveColor = DiploPanel.undoColor;
                text.text = "Undo" + baseText;
            }
            else
            {
                _backgroundActiveColor = _backgroundBaseColor;
                text.text = baseText;
            }
        }

        private bool IsUnderPointer => UIController.UIObjectAtPointer == this.gameObject ||
                                       UIController.UIObjectAtPointer == text.gameObject;
        
        private bool _pressing = false;
        public override void UIUpdate()
        {
            if (_pressing)
            {
                background.color = _backgroundActiveColor * 0.4f;
                if (UIController.PointerInputStatus == InputStatus.Releasing &&
                    IsUnderPointer)
                {
                    _pressing = false;
                    OnClick();
                }
            }
            else
            {
                if (IsUnderPointer)
                {
                    background.color = _backgroundActiveColor * 0.7f;
                    if (UIController.PointerInputStatus == InputStatus.Pressed)
                    {
                        _pressing = true;
                    }
                }
                else
                {
                    background.color = _backgroundActiveColor;
                }
            }

            text.color = background.color * 1.15f;
            if (UIController.SelectionChanged) RefreshImageAndUndoState();
        }

        public enum DiploPanelAction
        {
            DeclareWar
        }
    }
}