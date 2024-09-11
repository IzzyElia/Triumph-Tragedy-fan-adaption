using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameBoard.UI.SpecializedComponents
{
    public class UINotificationText : UIComponent
    {
        [SerializeField] private TextMeshProUGUI textMesh;
        private Color _color;
        private int _activePriority;
        private float _endTime;
        private float _duration;
        private float FadeOutTime => _duration / 3f;
        
        

        public void PushNotification(string text, float duration = 1.5f, Color color = default, int priority = 0)
        {
#if DEBUG
            if (duration == 0) throw new ArgumentException();
#endif
            if (priority < _activePriority) return;
            
            if (color == default) color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            _color = color;
            textMesh.color = color;
            _endTime = Time.time + duration;
            _duration = duration;
            _activePriority = priority;
            textMesh.text = text;
        }

        public override void UIUpdate()
        {
            float tMinus = _endTime - Time.time;
            float brightness = tMinus / FadeOutTime;
            if (tMinus <= _duration)
            {
                textMesh.color = _color * new Color(1, 1, 1, brightness);
            }
        }
        
        public override void OnGamestateChanged()
        {
            
        }

        public override void OnResyncEnded()
        {
            
        }
    }
}
