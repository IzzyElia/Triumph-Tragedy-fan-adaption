using System.Collections.Generic;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IGameCombat
    {
        public int CombatUID { get; }
        public int iTile{ get; }
        public List<CombatRoll> CombatRolls { get; }
        public int[] iSupportingCadres{ get; }
        public int iAttackerFaction{ get; }
        public int iDefenderFaction{ get; }
        public int initiative{ get; }
        public int numDiceAvailable { get; }
        public int iPhasingPlayer { get; }
        public bool DecidingDice { get; }
        public CombatDiceDistribution ProvidedCombatDiceDistribution { get; }
        int StageCounter { get; }
        public IGameCadre[] CalculateInvolvedCadreInterfaces();
    }
}