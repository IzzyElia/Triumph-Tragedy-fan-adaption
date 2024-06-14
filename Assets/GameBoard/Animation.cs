using System;
using System.Collections.Generic;

namespace GameBoard
{
    public delegate IEnumerator<AnimationState> AnimationFunction(params object[] parameters);

    struct Animation : IDisposable
    {
        public int AnimationID { get; private set; }
        private IEnumerator<AnimationState> _animation;
        public Animation(int animationID, AnimationFunction animationFunction, params object[] parameters)
        {
            _animation = animationFunction.Invoke(parameters);
            AnimationID = animationID;
        }

        public AnimationState DoStep()
        {
            _animation.MoveNext();
            AnimationState animationState = _animation.Current;
            if (animationState == AnimationState.Exit)
                Dispose();
            return animationState;
        }

        public void Dispose()
        {
            _animation?.Dispose();
        }
    }
}