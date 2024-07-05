using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public interface ICombatPanelUnit : IUIComponent
    {
        public void SetPips(int pips, bool animateChange);
        public void SetBaseValues(int pips, int maxPips, UnitType unitType, MapCountry country);
    }
}