using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace GameBoard.UI.SpecializeComponents
{
    public class UnitPlacementWindow : UIWindow
    {
        private GameObject _unitLayout;
        
        public override bool WantsToBeActive => 
            UIController.GameState.GamePhase == GamePhase.InitialPlacement ||
                UIController.GameState.GamePhase == GamePhase.Production;


        private int knownUnitTypesHash = 0;

        private Image[] _images = new Image[0];
        private List<UnitType> _buildableUnitTypes = new List<UnitType>();
        public override void OnResyncEnded()
        {
            int hash = 17;
            unchecked
            {
                for (int i = 0; i < GameState.Ruleset.UnitTypes.Length; i++)
                {
                    UnitType unitType = GameState.Ruleset.UnitTypes[i];
                    hash = hash * 23 + unitType.GetHashCode();
                }
            }
            


            if (hash != knownUnitTypesHash)
            {
                knownUnitTypesHash = hash;
                
                _buildableUnitTypes.Clear();
                for (int i = 0; i < GameState.Ruleset.UnitTypes.Length; i++)
                {
                    UnitType unitType = GameState.Ruleset.UnitTypes[i];
                    if (unitType.IsBuildableThroughNormalPlacementRules) _buildableUnitTypes.Add(unitType);
                }

                foreach (var image in _images)
                {
                    Destroy(image.gameObject);
                }

                for (int i = 0; i < _buildableUnitTypes.Count; i++)
                {
                    UnitType unitType = _buildableUnitTypes[i];
                    Button button = new GameObject($"{unitType.Name}", typeof(Image), typeof(Button), typeof(UnitButton))
                        .GetComponent<Button>();
                    UnitButton unitButton = button.GetComponent<UnitButton>();
                    unitButton.placementWindowObject = this;
                    unitButton.UnitType = unitType;
                    button.onClick.AddListener(unitButton.OnClick);
                    unitButton.Image = button.GetComponent<Image>();
                    unitButton.Image.material = new Material(UIController.uiIconMaterial);
                    unitButton.Image.sprite = unitType.Sprite;
                    unitButton.Image.transform.SetParent(_unitLayout.transform);
                }
            }
        }

        private int knownFactionDataHash = 0;
        private static readonly int BackgroundColor = Shader.PropertyToID("_BackgroundColor");
        private static readonly int IconColor = Shader.PropertyToID("_IconColor");
        private static readonly int Highlight = Shader.PropertyToID("_Highlight");


        public override void OnGamestateChanged()
        {
            if (PlayerCountry is null) return;
            int hash = HashCode.Combine(PlayerCountry.GetHashCode(), PlayerCountry.color.GetHashCode(), PlayerCountry.unitMainColor.GetHashCode(),
                PlayerCountry.unitSecondaryColor.GetHashCode());
            if (hash != knownFactionDataHash)
            {
                knownFactionDataHash = hash;
                
                foreach (var image in _images)
                {
                    image.material.SetColor(BackgroundColor, PlayerCountry.unitMainColor);
                    image.material.SetColor(IconColor, PlayerCountry.unitSecondaryColor);
                }
            }
        }

        private UnitButton _selectedPlacementButton;

        void SetSelectedPlacementButton(UnitButton selectedButton)
        {
            if (_selectedPlacementButton == selectedButton) { Debug.LogError("Button already selected"); return; }
            if (_selectedPlacementButton is not null)
            {
                _selectedPlacementButton.Image.material.SetInt(Highlight, 0);
            }

            if (selectedButton is not null)
            {
                selectedButton.Image.material.SetInt(Highlight, 1);
            }

            _selectedPlacementButton = selectedButton;
        }

        
        private bool _placing = false;

        public override void UIUpdate()
        {
            if (UIController.PointerInputStatus == PointerInputStatus.Pressed)
            {
                if (_selectedPlacementButton is not null && UIController.HoveredMapObject is CadrePlacementGhost ghost)
                {
                    ghost.UnitType = _selectedPlacementButton.UnitType;
                }
            }
        }
        
        
        
        protected override void OnActive()
        {
            
        }

        protected override void OnHidden()
        {
            
        }

        private class UnitButton : MonoBehaviour
        {
            [NonSerialized] public UnitPlacementWindow placementWindowObject;
            [NonSerialized] public UnitType UnitType;
            [NonSerialized] public Image Image;
            private bool _selected = false;

            public void OnClick()
            {
                if (_selected)
                {
                    _selected = false;
                    placementWindowObject.SetSelectedPlacementButton(null);
                }
                else
                {
                    _selected = true;
                    placementWindowObject.SetSelectedPlacementButton(this);
                }
            }
        }
    }
}