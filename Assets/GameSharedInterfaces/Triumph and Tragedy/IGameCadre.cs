namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IGameCadre : IGameEntity
    {
        public UnitType UnitType { get; }
        public bool UnitTypeIsHidden { get; }
        public int iCountry { get; }
        public int iTile { get; }
        public int Pips { get; }
        public int MaxPips { get; }
        public IGameFaction IFaction { get; }
    }
}