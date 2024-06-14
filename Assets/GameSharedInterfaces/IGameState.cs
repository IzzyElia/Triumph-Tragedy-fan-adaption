using System.Collections.Generic;

namespace GameSharedInterfaces
{
    public enum GamePhase
    {
        None,
        InitialPlacement,
        Production,
        Diplomacy,
        SelectCommandCards,
        GiveCommands,
        Combat,
    }
    public enum Season
    {
        None,
        NewYear,
        Spring,
        Summer,
        Fall,
        Winter,
    }
    public interface IGameState
    {
        public IPlayerAction GenerateClientsidePlayerActionByName(string name);
        public int iPlayer { get; }
        public int ActivePlayer { get; }
        public bool IsWaitingOnNetworkReply { get; }
    }

    // ReSharper disable once InconsistentNaming
    public interface ITTGameState : IGameState
    {
        public int Year { get; }
        public Season Season { get; }
        public GamePhase GamePhase { get; }
        public int PositionInTurnOrder { get; }
        public int[] PlayerOrder { get; }
        public int ActivePlayer { get; }
        public bool IsWaitingOnPlayer(int iPlayer);
        public bool IsSynced { get; }
        public IRuleset Ruleset { get; }
        public List<ICard> GetCardsInHand(int iPlayer);
        public ICard GetCard(int id, CardType cardType);
    }
}