using System.Collections.Generic;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    // ReSharper disable once InconsistentNaming
    public interface ITTGameState : IGameState
    {
        public int Year { get; }
        public Season Season { get; }
        public GamePhase GamePhase { get; }
        public int PositionInTurnOrder { get; }
        public int[] PlayerOrder { get; }
        public bool IsWaitingOnPlayer(int iPlayer);
        public bool IsSynced { get; }
        public Ruleset Ruleset { get; }
        public ICard GetCard(int id, CardType cardType);
        public IGameCadre GetCadre(int id);
        public (int iTile, int iCountry, int startingCadres)[] GetStartingUnits(int iPlayer);
        public IGameFaction GetFaction(int iFaction);
        
        // Pathfinding
        public int[] CalculateAccessibleTilesAdjecentTo(int iCadre, int iTile, MoveType moveType, int from = -1, UnitType calculatingAsType = null);
        public int[] CalculateAccessibleTiles(int iCadre, MoveType moveType, int from = -1, UnitType calculatingAsType = null);

        public int[] CalculateSupplyAccessibleTiles(int iStartingTile, SupplyType supplyType, int iFaction,
            bool allowHornOfAfrica);
        
        // Combat
        public CombatOption[] GetCombatOptions();
        public IGameCombat GetActiveCombat();
        public bool IsCombatHappening { get; }
        public List<int> ForcedCombats { get; }
        public List<CombatOption> CommittedCombats { get; }
        public Dictionary<int, int> CombatSupports { get; }
        int PhaseUID { get; }
        int PlayerPositionInTurnOrder(int i);
    }
}