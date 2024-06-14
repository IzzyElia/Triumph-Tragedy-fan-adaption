using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [RegisterEntityAsBaseType]
    public class InvestmentCard : GameCard, IInvestmentCard
    {
        static InvestmentCard()
        {
            RegisterCardType<InvestmentCard>();
        }

        private List<Tech> _techs = new List<Tech>();
        public IReadOnlyList<Tech> Techs => _techs;
        public int FactoryValue { get; set; }

        protected override void Init()
        {
            base.Init();
            CardType = CardType.Investment;
        }
        
        protected override void WriteCardDetails(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteByte((byte)FactoryValue);
            outgoingMessage.WriteByte((byte)Techs.Count);
            for (int i = 0; i < _techs.Count; i++)
            {
                outgoingMessage.WriteByte((byte)_techs[i]);
            }
        }

        protected override void ReadCardDetails(ref DataStreamReader incomingMessage)
        {
            FactoryValue = incomingMessage.ReadByte();
            byte techsLength = incomingMessage.ReadByte();
            _techs.Clear();
            for (int i = 0; i < techsLength; i++)
            {
                _techs[i] = (Tech)incomingMessage.ReadByte();
            }
        }

        public override int HashFullState(int asPlayer)
        {
            if (IsVisibleToPlayer(asPlayer))
            {
                int hash = HashCode.Combine(base.HashFullState(asPlayer), FactoryValue);
                unchecked
                {
                    for (int i = 0; i < Techs.Count; i++)
                    {
                        hash *= _techs[i].GetHashCode();
                    }
                }

                return hash;
            }
            else
            {
                int hash = base.HashFullState(asPlayer);
                return hash;
            }
        }
    }
}