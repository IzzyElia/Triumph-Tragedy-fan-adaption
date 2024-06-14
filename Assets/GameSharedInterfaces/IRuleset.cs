namespace GameSharedInterfaces
{
    public interface IRuleset
    {
        public UnitType[] UnitTypes { get; }
        public int GetIDOfNamedUnitType(string name);

        public UnitType GetNamedUnitType(string name);
        public int SeaTransportUnitType { get; }
    }
}