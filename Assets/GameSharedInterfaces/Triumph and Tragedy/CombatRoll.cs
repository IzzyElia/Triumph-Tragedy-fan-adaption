using Unity.Collections;
using Izzy;

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

        public int HashCode_MurmurHash3
        {
            get
            {
                int hash = Hashing.MurmurHash3_Combine(iShooter, (int)TargetedCategory, iTarget, iDieRoll);
                hash = Hashing.CombineHashes(hash, Hashing.MurmurHash3(UID));
                hash = Hashing.CombineHashes(hash, Hashing.MurmurHash3(IsHit));
                return hash;
            }
        }

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