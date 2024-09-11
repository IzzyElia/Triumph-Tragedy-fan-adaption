using System;
using FMODUnity;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    public class EffectProjectile : EffectObject
    {
        public GameObject fireParticle;
        public GameObject hitParticle;
        public float metersPerSecond; // speed
        public ProjectilePathType pathType;
        [NonSerialized] public Vector3 StartPosition;
        [NonSerialized] public Vector3 EndPosition;
        [NonSerialized] public bool PlaySound;
        [NonSerialized] public GameObject gun;
        [NonSerialized] public CombatRoll? CombatRoll;
        [NonSerialized] public bool IsHit;
        private float _metersToTravel;

        public override void OnCreate(CombatPanelEffect combatPanelEffect, CombatAnimationData animationData, AnimationTimeData timeData)
        {
            base.OnCreate(combatPanelEffect, animationData, timeData);
            _metersToTravel = Vector3.Distance(StartPosition, EndPosition);
            EffectParticle createdFireParticle = Instantiate(this.fireParticle).GetComponent<EffectParticle>();
            createdFireParticle.transform.position = StartPosition;
            createdFireParticle.PlaySound = PlaySound;
            createdFireParticle.OnCreate(combatPanelEffect, animationData, timeData);
            createdFireParticle.transform.SetParent(gun.transform);
            createdFireParticle.transform.localPosition = Vector3.zero;
        }

        public override void UpdateAnimationState(AnimationTimeData timeData)
        {
            Vector3 currentPosition;
            float secondsAlive = timeData.Time - StartTime;
            float distancedCrossed = (secondsAlive * metersPerSecond) / _metersToTravel;
         
            switch (pathType)
            {
                case ProjectilePathType.Direct:
                    currentPosition = Vector3.Lerp(StartPosition, EndPosition, distancedCrossed);
                    break;
                case ProjectilePathType.Ark:
                    const float arcMinHeight = 25;
                    const float arcVariation = 5;
                    Vector3 controlPoint = new Vector3(StartPosition.x, Mathf.Lerp(StartPosition.y, EndPosition.y, 0.5f) + arcMinHeight + Random.value * arcVariation, EndPosition.z);
                    currentPosition = CalculateBezierPoint(distancedCrossed, StartPosition, controlPoint, EndPosition);
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (distancedCrossed >= 1f)
            {
                EffectParticle hit = Instantiate(this.hitParticle).GetComponent<EffectParticle>();
                hit.GetComponent<StudioEventEmitter>().SetParameter("parameter:/HitStatus", IsHit ? 1 : 0);
                hit.OnCreate(CombatPanelEffect, AnimationData, timeData);
                hit.transform.position = transform.position;
                if (CombatRoll.HasValue && IsHit)
                {
                    if (CombatPanelEffect.CombatPanelUnits.TryGetValue(CombatRoll.Value.iTarget, out ICombatPanelUnit combatPanelUnit))
                    {
                        // TODO/Note - it's safe to call AnimateHit multiple times. Duplicate hits will be ignored by the combat panel unit
                        combatPanelUnit.AnimateHit(CombatRoll.Value);
                    }
                }
                Kill();
                return;
            }

            transform.position = currentPosition;
        }

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            Vector3 p = uu * p0; //terms of Bezier formula
            p += 2 * u * t * p1; //terms of Bezier formula
            p += tt * p2; //terms of Bezier formula
            return p;
        }
    }

    public enum ProjectilePathType
    {
        Direct,
        Ark
    }
}