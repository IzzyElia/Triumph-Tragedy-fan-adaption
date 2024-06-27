namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IGameFaction : IGameEntity
    {
        public int Production { get; }
        public int CommandsAvailable { get; }
        public int CommandInitiative { get; }
        public int ProductionAvailable { get; }
        public int Population { get; }
        public int Resources { get; }
        public int Industry { get; }
        public int FactoriesNeededForIndustryUpgrade { get; }

    }
}