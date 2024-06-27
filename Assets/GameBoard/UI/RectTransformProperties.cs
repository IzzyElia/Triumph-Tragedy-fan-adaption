using UnityEngine;

namespace GameBoard.UI
{
    public struct RectTransformProperties
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 OffsetMin;
        public Vector2 OffsetMax;

        public RectTransformProperties(RectTransform rectTransform)
        {
            this.AnchorMax = rectTransform.anchorMax;
            this.AnchorMin = rectTransform.anchorMin;
            this.OffsetMin = rectTransform.offsetMin;
            this.OffsetMax = rectTransform.offsetMax;
        }
    }
}