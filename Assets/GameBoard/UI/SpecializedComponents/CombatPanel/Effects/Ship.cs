using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents.CombatPanel.Effects
{
    public class Ship : CombatPanelEffect
    {
        struct Shot
        {
            public MuzzleFlash MuzzleFlash;
            public MuzzleFlash HitFlash;
            public float StartTime;
            public float EndTime;
            public Vector3 TargetPosition;
        }
        [SerializeField] private MuzzleFlash[] guns = new MuzzleFlash[0];
        private Shot[] shots = new Shot[0];
        private Vector3 _startPosition;
        private Vector3 _endPosition;
        

        private static GameObject _shipPrefab;
        public static Ship Create(CombatPanel combatPanel, CombatAnimationData animationData, AnimationTimeData timeData, float xPosition, CombatRoll[] assignedRolls)
        {
            if (_shipPrefab is null)
                _shipPrefab = Resources.Load<GameObject>("Prefabs/CombatPanel/Effects/Battleship");
            
            Ship ship = CombatPanelEffect.Generate<Ship>(_shipPrefab, combatPanel, animationData);
            ship._startPosition = ship.Translate01ToStageBounds(new Vector3(xPosition, 0.1f, 0.8f));
            ship._endPosition = ship.Translate01ToStageBounds(new Vector3(xPosition + 0.01f, 0.1f, 0.8f));
            ship.RandomizeFiring(timeData, assignedRolls);
            return ship;
        }
        protected override void UpdateAnimationState(AnimationTimeData timeData)
        {
            transform.localPosition =
                Vector3.Lerp(_startPosition, _endPosition, timeData.AnimationProgress);
        }

        public void RandomizeFiring(AnimationTimeData timeData, CombatRoll[] assignedRolls)
        {
            float minStartTime = timeData.TotalAnimationTime * 0.2f;
            float maxStartTime = timeData.TotalAnimationTime * 0.6f;
            float startTimeRange = maxStartTime - minStartTime;
            shots = new Shot[assignedRolls.Length];
            for (int i = 0; i < assignedRolls.Length; i++)
            {
                float startTime = minStartTime + Random.value * startTimeRange;
                float endTime = startTime + timeData.TotalAnimationTime * 0.4f;
                MuzzleFlash gun = guns[Random.Range(0, guns.Length)];
                CombatRoll roll = assignedRolls[i];
                MuzzleFlash shotFlash = MuzzleFlash.Create(CombatPanel, AnimationData, startTime);
                shotFlash.transform.position = gun.transform.position;
                MuzzleFlash hitFlash = MuzzleFlash.Create(CombatPanel, AnimationData, endTime);
                Vector3 targetPosition = Translate01ToStageBounds(new Vector3(
                    x: Random.value - 0.5f,
                    y: 0,
                    z: roll.IsHit ? (Random.value * 0.1f) + 0.9f : Random.value * 0.2f));
                hitFlash.transform.localPosition = targetPosition;
                shots[i] = new Shot()
                {
                    MuzzleFlash = shotFlash,
                    HitFlash = hitFlash,
                    TargetPosition = targetPosition,
                    StartTime = startTime,
                    EndTime = endTime
                };
            }
        }
    }
}