using System;
using System.Collections.Generic;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameBoard.UI.SpecializedComponents.CombatPanel.Effects
{
    public class EffectUnit : EffectObject
    {
        public GameObject projectile;
        public GameObject[] guns;
        public float metersPerSecond; // speed
        public bool RotateToMatchMovementVector;
        public ShotPattern shotPattern;
        public int minShotsPerUnit;
        public int MaxShotsPerUnit;
        public float burstSeperation;
        [NonSerialized] public Vector3 StartPosition;
        [NonSerialized] public Vector3 EndPosition;
        [NonSerialized] public bool HasHit;
        private float _metersToTravel;
        private float[] shotTimes;
        private int hitShot = -1;


        public override void OnCreate(CombatPanelEffect combatPanelEffect, CombatAnimationData animationData, AnimationTimeData timeData)
        {
            base.OnCreate(combatPanelEffect, animationData, timeData);
            int shotsToFire = Random.Range(minShotsPerUnit, MaxShotsPerUnit + 1);
            _metersToTravel = Vector3.Distance(StartPosition, EndPosition);
            if (RotateToMatchMovementVector)
            {
                transform.localRotation = Quaternion.Euler(0, 0, 
                    Mathf.Atan2(EndPosition.y - StartPosition.y, EndPosition.x - StartPosition.x) * Mathf.Rad2Deg);
            }

            if (HasHit) hitShot = Random.Range(0, shotsToFire);
            switch (shotPattern)
            {
                case ShotPattern.Random:
                    shotTimes = new float[shotsToFire];
                    for (int i = 0; i < shotsToFire; i++)
                    {
                        float randomTime = StartTime + Random.value * timeData.TotalAnimationTime;
                        shotTimes[i] = randomTime;
                    }
                    break;
                case ShotPattern.Barrage:
                    shotTimes = new float[shotsToFire];
                    float barrageStartTime = StartTime + (timeData.TotalAnimationTime / 3f) + (Random.value * 0.5f);
                    float barrageDuration = Random.value * 0.2f + 0.3f;
                    for (int i = 0; i < shotsToFire; i++)
                    {
                        float shotTime = barrageStartTime + Random.value * barrageDuration;
                        shotTimes[i] = shotTime;
                    }
                    break;
                case ShotPattern.Bursts:
                    shotTimes = new float[shotsToFire];
                    float burstStartTime = StartTime + (timeData.TotalAnimationTime / 3f) + (Random.value * 0.5f);
                    for (int i = 0; i < shotsToFire; i++)
                    {
                        shotTimes[i] = burstStartTime + (burstSeperation * i);
                    }
                    break;
            }
        }

        public override void UpdateAnimationState(AnimationTimeData timeData)
        {
            float secondsAlive = timeData.Time - StartTime;
            float distancedCrossed = (secondsAlive * metersPerSecond) / _metersToTravel;
            transform.position = Vector3.Lerp(StartPosition, EndPosition, distancedCrossed);
            for (int i = 0; i < shotTimes.Length; i++)
            {
                if (shotTimes[i] < 0) continue;
                
                if (timeData.Time >= shotTimes[i])
                {
                    if (shotPattern == ShotPattern.Bursts)
                    {
                        for (int j = 0; j < guns.Length; j++)
                        {
                            FireShot(guns[j], timeData, playSound:j == 0, hitShot == i);
                        }
                    }
                    else
                    {
                        FireShot(guns[Random.Range(0, guns.Length)], timeData, playSound: true, hitShot == i);
                    }
                    shotTimes[i] = -1;
                }
            }
        }

        void FireShot(GameObject gun, AnimationTimeData timeData, bool playSound, bool isHit)
        {
            EffectProjectile newProjectile = Instantiate(projectile).GetComponent<EffectProjectile>();
            newProjectile.StartPosition = gun.transform.position;
            if (isHit) newProjectile.EndPosition = CombatPanelEffect.Stage.effectNearCamera.transform.position + new Vector3(
                x: (Random.value - 0.5f) * CombatPanelEffect.Stage.effectNearCamera.orthographicSize * 2,
                y: (Random.value - 0.5f) * CombatPanelEffect.Stage.effectNearCamera.orthographicSize * 2,
                z: 0);
            else newProjectile.EndPosition = CombatPanelEffect.Stage.effectNearCamera.transform.position + new Vector3(
                x: (Random.value - 0.5f) * CombatPanelEffect.Stage.effectNearCamera.orthographicSize * 25,
                y: (Random.value - 0.5f) * CombatPanelEffect.Stage.effectNearCamera.orthographicSize * 25,
                z: 0);
            newProjectile.PlaySound = playSound;
            newProjectile.gun = gun;
            newProjectile.OnCreate(CombatPanelEffect, AnimationData, timeData);
        }
    }

    public enum ShotPattern
    {
        Random, // Projectiles fired at random times (rifle fire)
        Barrage, // Projectiles fired in a barrage, dispersed randomly between all the guns (ship bombardment)
        Bursts, // Projectiles fired in bursts from all guns simultaneously (machine gun or fighter machine guns)
    }
}