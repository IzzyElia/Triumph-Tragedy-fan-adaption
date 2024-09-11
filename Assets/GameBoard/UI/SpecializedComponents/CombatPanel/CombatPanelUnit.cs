using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelUnit : UIComponent, ICombatPanelUnit
    {
        private static GameObject _cachedPrefab;
        public static CombatPanelUnit Create(CombatPanelUnitGroup unitGroup, IGameCadre gameCadre)
        {
            if (_cachedPrefab is null) _cachedPrefab = Resources.Load<GameObject>("Prefabs/CombatPanel/Unit");

            GameObject unitObject = Instantiate(_cachedPrefab, unitGroup.gridLayout.transform);
            if (unitObject is null) throw new InvalidOperationException("Unit prefab not found");
            CombatPanelUnit unitController = unitObject.GetComponent<CombatPanelUnit>();
            if (unitController is null) throw new InvalidOperationException($"Combat panel unit prefab does not have the required component");
            unitGroup.UIController.RegisterUIComponent(unitController);
            unitController.UIController = unitGroup.UIController;
            unitController.SetBaseValues(pips:gameCadre.Pips, maxPips:gameCadre.MaxPips, unitType:gameCadre.UnitType, country:unitGroup.MapRenderer.MapCountriesByID[gameCadre.iCountry], id:gameCadre.ID);
            return unitController;
        }

        [SerializeField] private Image flagImage;
        [SerializeField] private Image unitIconImage;
        [SerializeField] private GameObject pipsLayout;
        [SerializeField] private Gradient gradient;

        private int cadreID;
        private UnitType unitType;
        private MapCountry country;
        private int maxPips;
        private int pips;
        public int CadreID => cadreID;
        public int Pips => pips;
        private Image[] _pipImages = Array.Empty<Image>();

        public void Refresh()
        {
            flagImage.sprite = country.FlagSprite;
            unitIconImage.sprite = unitType.GetSprite(country.name);
            if (_pipImages.Length != maxPips)
            {
                // Refresh pip objects
                for (int i = 0; i < _pipImages.Length; i++)
                {
                    Destroy(_pipImages[i].gameObject);
                }

                _pipImages = new Image[maxPips];
                for (int i = 0; i < maxPips; i++)
                {
                    _pipImages[i] = new GameObject($"pip {i}", typeof(RectTransform), typeof(CanvasRenderer),
                        typeof(Image)).GetComponent<Image>();
                    _pipImages[i].transform.SetParent(pipsLayout.transform);
                }
            }

            for (int i = 0; i < maxPips; i++)
            {
                if (i < pips)
                {
                    _pipImages[i].color = gradient.Evaluate((float)(pips - 1) / (float)maxPips);
                }
                else
                {
                    _pipImages[i].color = Color.clear;
                }
            }
        }

        private HashSet<uint> appliedHits = new HashSet<uint>();
        public void AnimateHit(CombatRoll combatRoll)
        {
            if (appliedHits.Contains(combatRoll.UID)) return;
            
            appliedHits.Add(combatRoll.UID);
            SetPips(pips - 1);
        }

        public void SetPips(int pips)
        {
            this.pips = pips;
            Refresh();
        }

        public void SetBaseValues(int pips, int maxPips, UnitType unitType, MapCountry country, int id)
        {
            this.pips = pips;
            this.maxPips = maxPips;
            this.unitType = unitType;
            this.country = country;
            this.cadreID = id;
            Refresh();
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
    }
}