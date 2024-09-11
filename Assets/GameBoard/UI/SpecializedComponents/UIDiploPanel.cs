using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIDiploPanel : UIWindow
    {
        [SerializeField] public Color undoColor;
        [SerializeField] private Image[] flags = Array.Empty<Image>();
        [SerializeField] private TextMeshProUGUI countryNameText;
        [SerializeField] private GameObject buttonsWrapper;
        [NonSerialized] public List<int> consideringNeutralWars = new List<int>();
        [NonSerialized] public List<int> consideringFactionWars = new List<int>();
        private UIDiploPanelButton[] _buttons = Array.Empty<UIDiploPanelButton>();

        protected override void Awake()
        {
            base.Awake();
            _buttons = buttonsWrapper.GetComponentsInChildren<UIDiploPanelButton>();
            foreach (var button in _buttons)
            {
                button.DiploPanel = this;
            }

            UseDefaultWindowAppearanceAnimations = false;
        }

        private void Refresh()
        {
            MapCountry selectedCountry = SelectedCountry;
            if (selectedCountry is null) return;
            foreach (var flag in flags)
            {
                flag.sprite = selectedCountry.FlagSprite;
            }

            countryNameText.text = selectedCountry.name;
            
            foreach (var diploButton in _buttons)
            {
                diploButton.RefreshImageAndUndoState();
            }
        }

        public MapCountry SelectedCountry
        {
            get
            {
                if (UIController.SelectedMapObject is MapCadre cadre) return cadre.MapCountry;
                if (UIController.SelectedMapObject is MapTile tile && tile.mapCountry is not null) return tile.mapCountry;
                return null;
            }
        }

        private int prevPhaseUID = -1;
        public override void OnGamestateChanged()
        {
            if (GameState.PhaseUID != prevPhaseUID)
            {
                consideringFactionWars.Clear();
                consideringNeutralWars.Clear();
            }
            Refresh();
        }

        public override void OnResyncEnded()
        {
            
        }

        public override bool WantsToBeActive =>
            SelectedCountry is not null && SelectedCountry.Faction != UIController.PlayerMapFaction &&
            (GameState.GamePhase == GamePhase.GiveCommands);
        protected override void OnActive()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        protected override void OnHidden()
        {
            gameObject.SetActive(false);
        }

        private MapCountry prevSelectedCountry;
        public override void UIUpdate()
        {
            base.UIUpdate();
            if (SelectedCountry != prevSelectedCountry)
            {
                Refresh();
            }
        }

        public bool ConsideringWarWith(int iCountry)
        {
            if (consideringNeutralWars.Contains(iCountry)) return true;
            MapCountry country = MapRenderer.MapCountriesByID[iCountry];
            if (country.Faction is not null && consideringFactionWars.Contains(country.Faction.ID)) return true;
            return false;
        }
    }
}