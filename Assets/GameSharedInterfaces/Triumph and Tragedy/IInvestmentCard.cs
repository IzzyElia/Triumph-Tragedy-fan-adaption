using System.Collections.Generic;
using System.ComponentModel.Design;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public interface IInvestmentCard : ICard
    {
        public IReadOnlyList<Tech> Techs { get; }
        public int FactoryValue { get; }
    }
}