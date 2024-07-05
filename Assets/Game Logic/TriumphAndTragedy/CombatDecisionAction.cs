using GameLogic;
using Izzy.ForcedInitialization;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class CombatDecisionAction : PlayerAction
    {
        static CombatDecisionAction ()
        {
            RegisterPlayerActionType<CombatDecisionAction>();
        }

        public override void Execute()
        {
            throw new System.NotImplementedException();
        }

        public override (bool, string) TestParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override void AddParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override bool RemoveParameter(params object[] parameter)
        {
            throw new System.NotImplementedException();
        }

        public override void SetAllParameters(params object[] parameters)
        {
            throw new System.NotImplementedException();
        }

        public override object[] GetParameters()
        {
            throw new System.NotImplementedException();
        }

        public override object[] GetData()
        {
            throw new System.NotImplementedException();
        }

        public override (bool, string) Validate()
        {
            throw new System.NotImplementedException();
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            throw new System.NotImplementedException();
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}