using System;

namespace GameBoard.UI
{
    public enum AnimationState
    {
        Continue,
        Exit,
    }
    public abstract class AnimatedEvent
    {
        private readonly Action<UIController> _callbackA;
        private readonly Action _callbackB;
        protected readonly UIController UIController;
        protected readonly Map MapRenderer;

        public AnimationState DoStep(float deltaTime)
        {
            AnimationState animationState = OnStep(deltaTime);
            if (animationState == AnimationState.Exit)
            {
                if (_callbackA != null) _callbackA.Invoke(UIController);
                else _callbackB?.Invoke();
            }

            return animationState;
        }
        protected abstract void Init(object[] parameters);
        protected abstract AnimationState OnStep(float deltaTime);
        
        /// <param name="callback">The callback method can optionally take the given UIController as a parameter, or can be null</param>
        protected AnimatedEvent(UIController uiController, Action<UIController> callback, bool simultaneous = false)
        {
            UIController = uiController;
            MapRenderer = uiController.MapRenderer;
            _callbackA = callback;

            uiController.RegisterAnimatedEvent(animatedEvent:this, simultaneous:simultaneous);
        }
        
        /// <param name="callback">The callback method can optionally take the given UIController as a parameter, or can be null</param>
        protected AnimatedEvent(UIController uiController, Action callback = null, bool simultaneous = false)
        {
            UIController = uiController;
            MapRenderer = uiController.MapRenderer;
            _callbackB = callback;

            uiController.RegisterAnimatedEvent(animatedEvent:this, simultaneous:simultaneous);
        }
    }
}