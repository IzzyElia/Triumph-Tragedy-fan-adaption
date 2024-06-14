using System.Collections.Generic;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IActionCard : ICard
    {
        public IReadOnlyList<int> Countries { get; }
        public int Initiative { get; }
        public int NumActions { get; }
        
    }
}