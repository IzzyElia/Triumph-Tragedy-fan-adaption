namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IGameFaction : IGameEntity
    {
        public int Production { get; }
        public int CommandsAvailable { get; }
        public int CommandInitiative { get; }
        public SupplyStatus[] TileSupplyStatus { get; }
        public TradeStatus[] TileTradeStatus { get; }
        public TradeStatus[] PredictedTileTradeStatus { get; }
        public int ProductionAvailable { get; }
        public int Population { get; }
        public int Resources { get; }
        public int Industry { get; }
        public int FactoriesNeededForIndustryUpgrade { get; }
        public bool IsAtWarWithCountry(int iCountry);
        public bool IsAtWarWithFaction(int iFaction);

    }
}