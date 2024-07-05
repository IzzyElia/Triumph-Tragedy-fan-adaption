using System.Collections.Generic;
using GameSharedInterfaces.Triumph_and_Tragedy;

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
        CommitCombats,
        SelectSupport,
        Combat,
        SelectNextCombat
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
        public List<ICard> GetCardsInHand(int iPlayer);
    }
}