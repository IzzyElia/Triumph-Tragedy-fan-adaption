using System;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.UI;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public class CombatPanelUnit : UIComponent, ICombatPanelUnit
    {
        private static GameObject _cachedPrefab;
        public static CombatPanelUnit Create(CombatPanelUnitGroup unitGroup, int pips, int maxPips, UnitType unitType, MapCountry country)
        {
            if (_cachedPrefab is null) _cachedPrefab = Resources.Load<GameObject>("Prefabs/CombatPanel/Unit");

            GameObject unitObject = Instantiate(_cachedPrefab, unitGroup.gridLayout.transform);
            if (unitObject is null) throw new InvalidOperationException("Unit prefab not found");
            CombatPanelUnit unitController = unitObject.GetComponent<CombatPanelUnit>();
            if (unitController is null) throw new InvalidOperationException($"Combat panel unit prefab does not have the required component");
            unitGroup.UIController.RegisterUIComponent(unitController);
            unitController.UIController = unitGroup.UIController;
            unitController.SetBaseValues(pips:pips, maxPips:maxPips, unitType:unitType, country:country);
            return unitController;
        }

        [SerializeField] private Image flagImage;
        [SerializeField] private Image unitIconImage;
        [SerializeField] private GameObject pipsLayout;
        [SerializeField] private Gradient gradient;

        private UnitType unitType;
        private MapCountry country;
        private int maxPips;
        private int pips;
        private Image[] _pipImages = Array.Empty<Image>();

        void Refresh()
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

        public void SetPips(int pips, bool animateChange)
        {
            this.pips = pips;
            Refresh();
        }

        public void SetBaseValues(int pips, int maxPips, UnitType unitType, MapCountry country)
        {
            this.pips = pips;
            this.maxPips = maxPips;
            this.unitType = unitType;
            this.country = country;
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