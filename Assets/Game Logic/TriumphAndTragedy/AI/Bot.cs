using GameLogic;
using GameSharedInterfaces;

namespace Game_Logic.TriumphAndTragedy.AI
{
    public abstract class Bot
    {
        public bool Thinking { get; set; }
        protected TTGameState GameState { get; private set; }

        public Bot(TTGameState gameState)
        {
            this.GameState = gameState;
        }
        
        // AI Logic
        public abstract PlayerAction AI_InitialUnits(TTGameState gameState);
        public abstract PlayerAction AI_Production(TTGameState gameState);
        public abstract PlayerAction AI_Cardplay(TTGameState gameState);
        public abstract PlayerAction AI_SelectCommandsCards(TTGameState gameState);
        public abstract PlayerAction AI_Commands(TTGameState gameState);
        public abstract PlayerAction AI_SelectCombats(TTGameState gameState);
        public abstract PlayerAction AI_SelectSupports(TTGameState gameState);
        public abstract PlayerAction AI_SelectNextCombat(TTGameState gameState);
        public abstract PlayerAction AI_CombatDecision(TTGameState gameState);
        public abstract void Rebuild (TTGameState gameState);
        
        // Utility functions
        protected int iPlayerFaction => GameState.iPlayer;
        protected GameFaction playedFaction => GameState.GetEntity<GameFaction>(iPlayerFaction);
        
    }
}