using Unity.Collections;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    public struct CombatRoll
    {
        public uint UID;
        public int iShooter;
        public UnitCategory TargetedCategory;
        public int iTarget;
        public int iDieRoll;
        public bool IsHit;

        public static CombatRoll Recreate(ref DataStreamReader message)
        {
            return new CombatRoll
            {
                UID = message.ReadUInt(),
                iShooter = message.ReadInt(),
                TargetedCategory = (UnitCategory)message.ReadInt(),
                iTarget = message.ReadInt(),
                iDieRoll = message.ReadInt(),
                IsHit = message.ReadByte() == 1,
            };
        }

        public void Write(ref DataStreamWriter message)
        {
            message.WriteUInt(UID);
            message.WriteInt(iShooter);
            message.WriteInt((int)TargetedCategory);
            message.WriteInt(iTarget);
            message.WriteInt(iDieRoll);
            message.WriteByte((byte)(IsHit == true ? 1 : 0));
        }
    }
}