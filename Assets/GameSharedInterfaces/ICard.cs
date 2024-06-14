using GameSharedInterfaces.Triumph_and_Tragedy;

namespace GameSharedInterfaces
{
    public enum CardType
    {
        Action,
        Investment
    }
    public interface ICard //: IGameEntity
    {
        int HoldingPlayer { get; }
        CardType CardType { get; }
    }
}