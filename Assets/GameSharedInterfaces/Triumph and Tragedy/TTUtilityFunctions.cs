using System;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    
    public static class TTUtilityFunctions
    {
        public static FactionMembershipStatus InfluenceToMembershipStatus(int AppliedInfluence)
        {
            switch (AppliedInfluence)
            {
                case 0: return FactionMembershipStatus.Unaligned;
                case 1: return FactionMembershipStatus.Associate;
                case 2: return FactionMembershipStatus.Protectorate;
                case >= 3: return FactionMembershipStatus.Ally;
                case -1: return FactionMembershipStatus.InitialMember;
                default: throw new NotImplementedException();
            }
        }

        public static int MembershipStatusToInfluence(FactionMembershipStatus membershipStatus)
        {
            switch (membershipStatus)
            {
                case FactionMembershipStatus.Unaligned:
                    return 0;
                case FactionMembershipStatus.Associate:
                    return 1;
                case FactionMembershipStatus.Protectorate:
                    return 2;
                case FactionMembershipStatus.Ally:
                    return 3;
                case FactionMembershipStatus.InitialMember:
                    return -1;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsFullMember(FactionMembershipStatus occupierMembershipStatus)
        {
            return occupierMembershipStatus == FactionMembershipStatus.Ally ||
                   occupierMembershipStatus == FactionMembershipStatus.InitialMember;
        }
    }
}