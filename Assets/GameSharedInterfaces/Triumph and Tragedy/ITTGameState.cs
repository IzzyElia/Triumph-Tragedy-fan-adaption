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
        public (int iTile, int iCountry, int startingCadres)[] GetStartingUnits(int iPlayer);
        public IGameFaction GetFaction(int iFaction);
        
        // Pathfinding
        public int[] CalculateAccessibleTilesAdjecentTo(int iCadre, int iTile, bool redeployment);
        public int[] CalculateAccessibleTiles(int iCadre, bool redeployment);

    }
}