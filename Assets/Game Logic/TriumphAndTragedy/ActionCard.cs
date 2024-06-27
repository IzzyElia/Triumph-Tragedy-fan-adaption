using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [RegisterEntityAsBaseType]
    public class ActionCard : GameCard, IActionCard
    {
        static ActionCard()
        {
            RegisterCardType<ActionCard>();
        }

        public List<int> Countries { get; set; } = new List<int>();
        public List<SpecialDiplomacyAction> SpecialDiplomacyActions { get; set; } = new List<SpecialDiplomacyAction>();
        public int Initiative { get; set; }
        public int NumActions { get; set; }
        public Season Season { get; set; }

        public List<int> GetDiploCountryTargets()
        {
            List<int> iCountries = new List<int>();
            foreach (var country in Countries)
            {
                iCountries.Add(country);
            }

            foreach (var specialDiplomacyAction in SpecialDiplomacyActions)
            {
                SpecialDiplomacyActionFactionDefinition factionDefinition =
                    specialDiplomacyAction.GetFactionDefinition(HoldingPlayer);
                if (!factionDefinition.IsInsurgentAction)
                {
                    foreach (var iCountry in factionDefinition.iCountries)
                    {
                        iCountries.Add(iCountry);
                    }
                }
            }

            return iCountries;
        }

        public List<int> GetInsurgentCountryTargets()
        {
            List<int> iCountries = new List<int>();

            foreach (var specialDiplomacyAction in SpecialDiplomacyActions)
            {
                SpecialDiplomacyActionFactionDefinition factionDefinition =
                    specialDiplomacyAction.GetFactionDefinition(HoldingPlayer);
                if (factionDefinition.IsInsurgentAction)
                {
                    foreach (var iCountry in factionDefinition.iCountries)
                    {
                        iCountries.Add(iCountry);
                    }
                }
            }

            return iCountries;
        }
        
        public List<int> GetInsurgentTileTargets()
        {
            List<int> iTiles = new List<int>();

            foreach (var specialDiplomacyAction in SpecialDiplomacyActions)
            {
                SpecialDiplomacyActionFactionDefinition factionDefinition =
                    specialDiplomacyAction.GetFactionDefinition(HoldingPlayer);
                if (factionDefinition.IsInsurgentAction)
                {
                    foreach (var iTile in factionDefinition.iTiles)
                    {
                        iTiles.Add(iTile);
                    }
                }
            }

            return iTiles;
        }

        protected override void Init()
        {
            base.Init();
            CardType = CardType.Action;
        }

        public void SetCountries(params int[] countries)
        {
            Countries.Clear();
            for (int i = 0; i < countries.Length; i++)
            {
                Countries.Add(countries[i]);
            }
        }
        
        protected override void WriteCardDetails(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteByte((byte)Initiative);
            outgoingMessage.WriteByte((byte)NumActions);
            outgoingMessage.WriteByte((byte)Season);
            outgoingMessage.WriteByte((byte)Countries.Count);
            for (int i = 0; i < Countries.Count; i++)
            {
                outgoingMessage.WriteShort((short)Countries[i]);
            }
            outgoingMessage.WriteByte((byte)SpecialDiplomacyActions.Count);
            for (int i = 0; i < SpecialDiplomacyActions.Count; i++)
            {
                outgoingMessage.WriteShort((short)SpecialDiplomacyActions[i].ID);
            }
        }

        protected override void ReadCardDetails(ref DataStreamReader incomingMessage)
        {
            Initiative = incomingMessage.ReadByte();
            NumActions = incomingMessage.ReadByte();
            Season = (Season)incomingMessage.ReadByte();
            byte countriesCount = incomingMessage.ReadByte();
            Countries.Clear();
            for (int i = 0; i < countriesCount; i++)
            {
                Countries.Add(incomingMessage.ReadShort());
            }
            byte specialDiplomacyActionsCount = incomingMessage.ReadByte();
            SpecialDiplomacyActions.Clear();
            for (int i = 0; i < specialDiplomacyActionsCount; i++)
            {
                int iSpecialDiplomacyAction = incomingMessage.ReadShort();
                SpecialDiplomacyActions.Add(GameState.Ruleset.specialDiplomacyActionDefinitions[iSpecialDiplomacyAction]);
            }
        }

        public override int HashFullState(int asPlayer)
        {
            if (IsVisibleToPlayer(asPlayer))
            {
                int hash = HashCode.Combine(base.HashFullState(asPlayer), Initiative, NumActions, Season);
                unchecked
                {
                    for (int i = 0; i < Countries.Count; i++)
                    {
                        hash *= Countries[i].GetHashCode();
                    }
                    for (int i = 0; i < SpecialDiplomacyActions.Count; i++)
                    {
                        hash *= SpecialDiplomacyActions[i].GetStateHashCode();
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


        // Utility functions


    }
}