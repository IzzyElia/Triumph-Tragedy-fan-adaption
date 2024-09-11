using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public interface ICombatPanelUnit : IUIComponent
    {
        public void AnimateHit(CombatRoll combatRoll);
        public int CadreID { get; }
        public int Pips { get; }
        public void SetPips(int pips);
        public void SetBaseValues(int pips, int maxPips, UnitType unitType, MapCountry country, int id);
    }
}