using Unity.Collections;

namespace GameSharedInterfaces
{
    public enum SpecialCombatDecisionType
    {
        FireAndRetreat,
        SubmergeSubs
    }

    public struct SpecialCombatDecisionData
    {
        public SpecialCombatDecisionType DecisionType;

        public SpecialCombatDecisionData(SpecialCombatDecisionType decisionType)
        {
            this.DecisionType = decisionType;
        }
    }
    public struct CombatDiceDecisionData
    {
        public int GroundDice;
        public int AirDice;
        public int SeaDice;
        public int SubDice;

        public CombatDiceDecisionData(int groundDice, int airDice, int seaDice, int subDice)
        {
            this.GroundDice = groundDice;
            this.AirDice = airDice;
            this.SeaDice = seaDice;
            this.SubDice = subDice;
        }
        
        public static CombatDiceDecisionData Recreate(ref DataStreamReader message)
        {
            int groundDice = (int)message.ReadShort();
            int airDice = (int)message.ReadShort();
            int seaDice = (int)message.ReadShort();
            int subDice = (int)message.ReadShort();
            return new CombatDiceDecisionData(groundDice: groundDice, airDice: airDice, seaDice:seaDice, subDice: subDice);
        }
        public void Write(ref DataStreamWriter message)
        {
            message.WriteShort((short)GroundDice);
            message.WriteShort((short)AirDice);
            message.WriteShort((short)SeaDice);
            message.WriteShort((short)SubDice);
        }
    }
}