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
        private readonly Action _callback;
        protected readonly UIController UIController;
        protected readonly Map MapRenderer;
        protected float timeSinceAnimationStart { get; private set; }

        public AnimationState DoStep(float deltaTime)
        {
            timeSinceAnimationStart += deltaTime;
            AnimationState animationState = OnStep(deltaTime);
            if (animationState == AnimationState.Exit)
            {
                _callback?.Invoke();
            }

            return animationState;
        }
        protected abstract AnimationState OnStep(float deltaTime);
        
        
        /// <param name="callback">The callback method can optionally take the given UIController as a parameter, or can be null</param>
        protected AnimatedEvent(UIController uiController, Action callback = null, bool simultaneous = false)
        {
            UIController = uiController;
            MapRenderer = uiController.MapRenderer;
            _callback = callback;

            uiController.RegisterAnimatedEvent(animatedEvent:this, simultaneous:simultaneous);
        }
    }
}