using System.Collections.Generic;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IActionCard : ICard
    {
        public List<int> Countries { get; }
        public List<SpecialDiplomacyAction> SpecialDiplomacyActions { get; }
        public int Initiative { get; }
        public int NumActions { get; }
        public Season Season { get; }
        
    }
}