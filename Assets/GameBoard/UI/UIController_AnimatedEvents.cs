using System.Collections.Generic;
using UnityEngine;

namespace GameBoard.UI
{
    // Animation manager
    public partial class UIController
    {
        public List<AnimatedEvent> _simultaniousAnimatedEvents = new List<AnimatedEvent>();
        public Queue<AnimatedEvent> _animationQueue = new Queue<AnimatedEvent>();
        
        public void RegisterAnimatedEvent(AnimatedEvent animatedEvent, bool simultaneous)
        {
            if (simultaneous)
            {
                _simultaniousAnimatedEvents.Add(animatedEvent);
            }
            else
            {
                _animationQueue.Enqueue(animatedEvent);
            }
        }

        private List<AnimatedEvent> _deadAnimations = new List<AnimatedEvent>();
        void AnimationsUpdate()
        {
            foreach (var animatedEvent in _simultaniousAnimatedEvents)
            {
                AnimationState animationState = animatedEvent.DoStep(deltaTime: Time.deltaTime);
                if (animationState == AnimationState.Exit)
                {
                    _deadAnimations.Add(animatedEvent);
                }
            }

            if (_deadAnimations.Count > 0)
            {
                foreach (var deadAnimation in _deadAnimations)
                {
                    // The animation callback is called by the animated event object during DoStep
                    _simultaniousAnimatedEvents.Remove(deadAnimation);
                }
                _deadAnimations.Clear();
            }

            if (_animationQueue.TryPeek(out AnimatedEvent queuedAnimatedEvent))
            {
                AnimationState animationState = queuedAnimatedEvent.DoStep(deltaTime: Time.deltaTime);
                if (animationState == AnimationState.Exit) _animationQueue.Dequeue();
            }
        }
    }
}