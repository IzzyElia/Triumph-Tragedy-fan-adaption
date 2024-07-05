using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;

namespace GameBoard.UI.SpecializeComponents.CombatPanel
{
    public struct CombatAnimationData
    {
        public CombatSide firingSide;
        public UnitType firingUnitType;
        public UnitCategory firingTargetType;
        public CombatRoll[] animatingRolls;
        
    }

    public enum AnimationState
    {
        FirstFrame,
        Ongoing,
        LastFrame,
    }
    public struct AnimationTimeData
    {
        public float Time;
        public float TotalAnimationTime;
        public float DarkenProgress;
        public AnimationState AnimationState;
        public float AnimationProgress => Time / TotalAnimationTime;
    }
    public interface ICombatPanelAnimationParticipant
    {
        public void CombatAnimation(CombatAnimationData animationData, AnimationTimeData timeData);
    }
}