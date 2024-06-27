using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace GameBoard.UI.SpecializeComponents
{
    public class UnitPlacementWindow : UIWindow
    {
        struct PlacementGhostReference
        {
            public MapCadrePlacementGhost PlacementGhost;
            public ProductionActionData ProductionAction;

            public PlacementGhostReference(MapCadrePlacementGhost placementGhost, ProductionActionData productionAction)
            {
                this.PlacementGhost = placementGhost;
                this.ProductionAction = productionAction;
            }
        }
        [SerializeField] private GameObject _unitLayout;
        [NonSerialized] private MapCadrePlacementGhost _heldPlacementGhost;
        [NonSerialized] private List<PlacementGhostReference> _placedUnits = new List<PlacementGhostReference>();
        
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
                for (int i = 0; i < GameState.Ruleset.unitTypes.Length; i++)
                {
                    UnitType unitType = GameState.Ruleset.unitTypes[i];
                    hash = hash * 23 + unitType.GetHashCode();
                }
            }
            


            if (hash != knownUnitTypesHash)
            {
                knownUnitTypesHash = hash;
                
                _buildableUnitTypes.Clear();
                for (int i = 0; i < GameState.Ruleset.unitTypes.Length; i++)
                {
                    UnitType unitType = GameState.Ruleset.unitTypes[i];
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

            if ((GameState.GamePhase != GamePhase.Production || GameState.ActivePlayer != iPlayer) && _placedUnits.Count > 0)
            {
                foreach (var placedUnitRef in _placedUnits)
                {
                    placedUnitRef.PlacementGhost.DestroyMapObject();
                }
                
                _placedUnits.Clear();
            }
        }

        private UnitButton _selectedPlacementButton;

        void SetSelectedPlacementButton(UnitButton selectedButton)
        {
            if (_selectedPlacementButton is not null)
            {
                _selectedPlacementButton.Selected = false;
                _selectedPlacementButton.Image.material.SetInt(Highlight, 0);
            }

            if (selectedButton is not null)
            {
                selectedButton.Selected = true;
                selectedButton.Image.material.SetInt(Highlight, 1);
            }

            _selectedPlacementButton = selectedButton;

            if (GameState.GamePhase == GamePhase.Production)
            {
                if (selectedButton is null)
                {
                    if (_heldPlacementGhost is not null)
                    {
                        _heldPlacementGhost.DestroyMapObject();
                        _heldPlacementGhost = null;
                    }
                }
                else
                {
                    if (_heldPlacementGhost is null)
                    {
                        _heldPlacementGhost = MapCadrePlacementGhost.Create("Placement Ghost", UIController.MapRenderer,
                            null, PlayerFaction.leader, selectedButton.UnitType, UnitGhostPurpose.Held);
                    }
                    else
                    {
                        _heldPlacementGhost.UnitType = selectedButton.UnitType;
                    }
                }
            }
        }
        
        private bool _placing = false;
        
        public override void UIUpdate()
        {
            base.UIUpdate();
            if (GameState.GamePhase == GamePhase.InitialPlacement) InitialPlacementUpdate();
            else if (GameState.GamePhase == GamePhase.Production)  ProductionUpdate();
        }
        
        void ProductionUpdate()
        {
            if (_heldPlacementGhost is not null)
            {
                _heldPlacementGhost.transform.position = new Vector3(UIController.PointerPositionInWorld.x, UIController.PointerPositionInWorld.y, -0.2f);
            }
            if (UIController.PointerInputStatus == InputStatus.Pressed && !UIController.PointerIsOverUI)
            {
                if (_heldPlacementGhost is not null && UIController.HoveredOverTile is not null && UIController.HoveredOverTile.mapCountry is not null)
                {
                    ProductionActionData productionAction = ProductionActionData.BuildUnitAction(UIController.HoveredOverTile.ID, _heldPlacementGhost.UnitType.IdAndInitiative);
                    (bool valid, string reason) = UIController.ProductionAction.TestParameter(productionAction);
                    if (valid)
                    {
                        _placedUnits.Add(new PlacementGhostReference(_heldPlacementGhost, productionAction));
                        UIController.ProductionAction.AddParameter(productionAction);
                        _heldPlacementGhost.Purpose = UnitGhostPurpose.Build;
                        _heldPlacementGhost.Tile = UIController.HoveredOverTile;
                        _heldPlacementGhost.MapCountry = UIController.HoveredOverTile.mapCountry;
                        
                        // Duplicate the placement ghost before dropping it
                        _heldPlacementGhost = MapCadrePlacementGhost.Create("Placement Ghost", UIController.MapRenderer,
                            null, PlayerFaction.leader, _heldPlacementGhost.UnitType, UnitGhostPurpose.Held);
                        UIController.ProductionInfoWindow.Refresh();
                    }
                    else
                    {
                        Debug.Log(reason);
                    }
                }
                else if (UIController.HoveredMapObject is MapCadrePlacementGhost placementGhost)
                {
                    foreach (var placedUnitRef in _placedUnits)
                    {
                        if (placedUnitRef.PlacementGhost == placementGhost)
                        {
                            UIController.ProductionAction.RemoveParameter();
                            break;
                        }
                    }
                }
                else if (UIController.HoveredMapObject is MapCadre mapCadre)
                {
                    if (UIController.ModifierInputStatus != InputStatus.Held)
                    {
                        if (mapCadre.Pips + mapCadre.ProjectedPips < mapCadre.MaxPips)
                        {
                            ProductionActionData addPipsAction = ProductionActionData.ReinforceUnitAction(mapCadre.ID);
                            UIController.ProductionAction.AddParameter(addPipsAction);
                            mapCadre.ProjectedPips += 1;
                            UIController.ProductionInfoWindow.Refresh();
                        }
                    }
                    else
                    {
                        ProductionActionData addPipsAction = ProductionActionData.ReinforceUnitAction(mapCadre.ID);
                        if (UIController.ProductionAction.RemoveParameter(addPipsAction))
                        {
                            mapCadre.ProjectedPips -= 1;
                            UIController.ProductionInfoWindow.Refresh();
                        }
                    }
                }
            }

            if (UIController.ModifierInputStatus == InputStatus.Pressed ||
                UIController.ModifierInputStatus == InputStatus.Releasing)
            {
                foreach (var mapCadre in MapRenderer.MapCadresByID)
                {
                    if (mapCadre is not null) mapCadre.RecalculateAppearance();
                }
            }
        }
        
        void InitialPlacementUpdate()
        {
            if (UIController.PointerInputStatus == InputStatus.Pressed)
            {
                if (_selectedPlacementButton is not null && UIController.HoveredMapObject is MapCadrePlacementGhost ghost)
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
            if (_heldPlacementGhost is not null)
            {
                _heldPlacementGhost.DestroyMapObject();
                _heldPlacementGhost = null;
            }
        }

        private class UnitButton : MonoBehaviour
        {
            [NonSerialized] public UnitPlacementWindow placementWindowObject;
            [NonSerialized] public UnitType UnitType;
            [NonSerialized] public Image Image;
            [NonSerialized] public bool Selected = false;

            public void OnClick()
            {
                if (Selected)
                {
                    placementWindowObject.SetSelectedPlacementButton(null);
                }
                else
                {
                    placementWindowObject.SetSelectedPlacementButton(this);
                }
            }
        }
    }
}