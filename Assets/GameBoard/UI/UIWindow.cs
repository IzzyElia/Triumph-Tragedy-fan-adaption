using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameBoard.UI
{
    
    public abstract class UIWindow : UIComponent
    {
        [SerializeField] protected RectTransform rectTransform;
        [FormerlySerializedAs("AppearanceDirection")] [SerializeField] protected Direction appearanceDirection;
        
        public bool Active { get; private set; }
        public void SetActive(bool active, bool force = false)
        {
            if (force)
            {
                gameObject.SetActive(active);
                if (UseDefaultWindowAppearanceAnimations) _animatingTransition = true;
                if (active) OnActive(); else OnHidden();
            }
            else
            {
                if (active && !Active)
                {
                    Active = true;
                    gameObject.SetActive(true);
                    if (UseDefaultWindowAppearanceAnimations) _animatingTransition = true;
                    OnActive();
                }
                else if (Active)
                {
                    Active = false;
                    if (UseDefaultWindowAppearanceAnimations) _animatingTransition = true;
                    OnHidden();
                }
            }
        }

        public abstract bool WantsToBeActive { get; }
        protected abstract void OnActive();
        protected abstract void OnHidden();

        protected bool UseDefaultWindowAppearanceAnimations = true;
        private bool _animatingTransition;
        private Vector2 _basePosition;
        private float _momentum = 0;
        public override void UIUpdate()
        {
            if (_animatingTransition)
            {
                if (Active)
                {
                    rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, _basePosition, 0.02f);
                    if (Vector2.Distance(rectTransform.anchoredPosition, _basePosition) <= 5)
                    {
                        rectTransform.anchoredPosition = _basePosition;
                        _animatingTransition = false;
                    }
                }
                else
                {
                    if (_momentum < 5) _momentum += 0.05f;
                    switch (appearanceDirection)
                    {
                        case Direction.up:
                            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + _momentum);
                            break;
                        case Direction.down:
                            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - _momentum);
                            break;
                        case Direction.right:
                            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x + _momentum, rectTransform.anchoredPosition.y);
                            break;
                        case Direction.left:
                            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x - _momentum, rectTransform.anchoredPosition.y);
                            break;
                    }

                    if (rectTransform.rect.xMin > UIController.Canvas.pixelRect.xMax ||
                        rectTransform.rect.xMax < UIController.Canvas.pixelRect.xMin ||
                        rectTransform.rect.yMin > UIController.Canvas.pixelRect.yMax ||
                        rectTransform.rect.yMax < UIController.Canvas.pixelRect.yMin)
                    {
                        _animatingTransition = false;
                        _momentum = 0;
                    }
                }
            }
        }


        private void Awake()
        {
            if (rectTransform is null) Debug.LogError($"No rect transform on {this.globalName}");
            _basePosition = rectTransform.anchoredPosition;
        }

        public enum Direction
        {
            up,
            down,
            left,
            right
        }
    }
}