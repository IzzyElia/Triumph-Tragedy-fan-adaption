using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelUnitGroup : UIComponent, ICombatPanelAnimationParticipant
    {
        private static GameObject _cachedPrefab;
        public static CombatPanelUnitGroup Create(UIController uiController, CombatPanel combatPanel, ITTGameState gameState, CombatPanelFactionGroup parent, UnitType unitType, IList<IGameCadre> cadres, MapFaction faction, CombatSide side)
        {
            if (_cachedPrefab is null)
            {
                _cachedPrefab = Resources.Load<GameObject>("Prefabs/CombatPanel/UnitGroup");
                if (_cachedPrefab is null) throw new InvalidOperationException("Missing unit group prefab");
            }

            CombatPanelUnitGroup unitGroup = Instantiate(_cachedPrefab, parent.transform).GetComponent<CombatPanelUnitGroup>();
            unitGroup.backgroundAspectRatioFitter = unitGroup.background.GetComponent<AspectRatioFitter>();
            unitGroup._baseColor = unitGroup.background.color;
            unitGroup._overlayBaseColor = unitGroup.overlay.color;
            if (unitGroup is null) throw new InvalidOperationException("Unit group prefab is missing the unit group component");
            uiController.RegisterUIComponent(unitGroup);
            unitGroup._gameState = gameState;
            unitGroup._combatPanel = combatPanel;
            combatPanel.RegisterAnimationParticipant(unitGroup);
            unitGroup.SetSide(side);
            unitGroup.Refresh(gameState, unitType, cadres, faction);

            return unitGroup;
        }

        private CombatPanel _combatPanel;
        private ITTGameState _gameState;
        private UnitType _unitType;
        private int iFaction;
        private List<ICombatPanelUnit> _panelUnits = new List<ICombatPanelUnit>();
        [SerializeField] private Image background;
        [SerializeField] private Image overlay;
        private Color _baseColor;
        private Color _overlayBaseColor;
        private AspectRatioFitter backgroundAspectRatioFitter;
        [SerializeField] public GridLayoutGroup gridLayout;

        public void Refresh(ITTGameState gameState, UnitType unitType, IList<IGameCadre> cadres, MapFaction faction)
        {
            this._unitType = unitType;
            this.iFaction = faction.ID;
            Dictionary<string, int> countryProminance = new Dictionary<string, int>();
            for (int i = 0; i < Math.Max(cadres.Count, _panelUnits.Count); i++)
            {
                if (i < cadres.Count)
                {
                    IGameCadre cadre = cadres[i];
                    MapCountry country = MapRenderer.MapCountriesByID[cadre.iCountry];
                    if (countryProminance.TryGetValue(country.name, out int prominance))
                    {
                        countryProminance[country.name]++;
                    }
                    else
                    {
                        countryProminance.Add(country.name, 1);
                    }
                    if (i < _panelUnits.Count)
                    {
                        _panelUnits[i].SetBaseValues(pips:cadre.Pips, maxPips:cadre.MaxPips, unitType:cadre.UnitType, country:country);
                    }
                    else
                    {
                        CombatPanelUnit panelUnit = CombatPanelUnit.Create(unitGroup:this, pips:cadre.Pips, maxPips:cadre.MaxPips, unitType:cadre.UnitType, country:country);
                        _panelUnits.Add(panelUnit);
                    }
                }
                else if (i < _panelUnits.Count)
                {
                    _panelUnits[i].DestroyUIComponent();
                }
            }
            if (_panelUnits.Count > cadres.Count)
            {
                _panelUnits.RemoveRange(cadres.Count, _panelUnits.Count - cadres.Count);
            }
            
            (string country, int prominance) mostProminantCountry = (faction.leader.name, 0);
            foreach ((string country, int prominance) in countryProminance)
            {
                if (prominance > mostProminantCountry.prominance) mostProminantCountry = (country, prominance);
            }
            
            background.sprite = unitType.GetCombatPanelSprite(
                firstChoiceCountry:mostProminantCountry.country, 
                secondChoiceCountry:faction.leader.name);
            float aspectRatio = background.sprite is null ? 1 : (float)background.sprite.texture.width / (float)background.sprite.texture.height;
            backgroundAspectRatioFitter.aspectRatio = aspectRatio;
        }
        
        public void OnCombatStateUpdated()
        {
            if (_combatPanel.ActiveCombat.initiative == _unitType.IdAndInitiative && _combatPanel.ActiveCombat.iPhasingPlayer == iFaction)
            {
                background.color = _baseColor;
            }
            else
            {
                background.color = _baseColor * 0.7f;
            }
        }
        
        public void SetSide(CombatSide side)
        {
            switch (side)
            {
                case CombatSide.Attacker:
                    gridLayout.childAlignment = TextAnchor.UpperLeft;
                    background.transform.localScale = new Vector3(
                        -Mathf.Abs(background.transform.localScale.x),
                        background.transform.localScale.y, 
                        background.transform.localScale.z);
                    break;
                case CombatSide.Defender:
                    gridLayout.childAlignment = TextAnchor.UpperRight;
                    background.transform.localScale = new Vector3(
                        background.transform.localScale.x,
                        background.transform.localScale.y, 
                        background.transform.localScale.z);
                    break;
                default: throw new NotImplementedException();
            }
        }

        public override void OnGamestateChanged()
        {
            
        }

        public override void OnResyncEnded()
        {
            
        }

        public override void UIUpdate()
        {
            
        }

        protected override void OnBeingDestroyed()
        {
            base.OnBeingDestroyed();
            
        }

        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData)
        {
            switch (timeData.AnimationState)
            {
                case AnimationState.FirstFrame:
                    overlay.gameObject.SetActive(true);
                    break;
                case AnimationState.LastFrame:
                    overlay.gameObject.SetActive(false);
                    break;
            }
            if (animationData.firingUnitType == this._unitType)
            {
                overlay.color = _overlayBaseColor;
            }
            else
            {
                overlay.color = new Color(_overlayBaseColor.r, _overlayBaseColor.g, _overlayBaseColor.b, _overlayBaseColor.a * timeData.DarkenProgress);
            }
        }
    }
}