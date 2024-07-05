using System;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace GameBoard.UI.SpecializeComponents.CombatPanel.Effects
{
    public class MuzzleFlash : CombatPanelEffect
    {
        private static GameObject _muzzleFlashPrefab;
        
        [SerializeField] private SpriteRenderer image;
        [SerializeField] private float speed;
        private float StartTime;
        [SerializeField] private float debug_T;
        
        public static MuzzleFlash Create(CombatPanel combatPanel, CombatAnimationData animationData, float startTime)
        {
            if (_muzzleFlashPrefab is null)
                _muzzleFlashPrefab = Resources.Load<GameObject>("Prefabs/CombatPanel/Effects/MuzzleFlash");
            
            MuzzleFlash muzzleFlash = CombatPanelEffect.Generate<MuzzleFlash>(_muzzleFlashPrefab, combatPanel, animationData);
            muzzleFlash.StartTime = startTime;
            return muzzleFlash;
        }


        private bool _isReset = false;
        protected override void UpdateAnimationState(AnimationTimeData timeData)
        {
            if (timeData.Time >= StartTime)
            {
                float t = (timeData.Time - StartTime) * speed;
                image.material.SetFloat("_T", debug_T);
                if (t >= 1) Kill();
            }
            else if (!_isReset)
            {
                _isReset = true;
                image.material.SetFloat("_T", 1);
            }
        }
    }
}