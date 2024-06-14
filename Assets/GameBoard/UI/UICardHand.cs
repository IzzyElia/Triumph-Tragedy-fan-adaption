using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameBoard.UI
{
    //[ExecuteAlways]
    public class UICardHand : UIWindow
    {
        private void Start()
        {
            CardPreviewPrefab = Resources.Load<GameObject>("Prefabs/CardPreview");
            CardPreviewPrefab = Resources.Load<GameObject>("Prefabs/ActionCard");
            CardPreviewPrefab = Resources.Load<GameObject>("Prefabs/InvestmentCard");
            ActionCardBack = Resources.Load<Sprite>("Graphics/ActionCardBack");
            InvestmentCardBack = Resources.Load<Sprite>("Graphics/InvestmentCardBack");
            ActionCardBack = Resources.Load<Sprite>("Graphics/ActionCardBack");
            UniversalCardBack = Resources.Load<Sprite>("Graphics/UniversalCardBack");
        }

        public GameObject CardPreviewPrefab;
        public GameObject ActionCardPrefab;
        public GameObject InvestmentCardPrefab;
        public Sprite ActionCardBack;
        public Sprite InvestmentCardBack;
        public Sprite UniversalCardBack;

        [SerializeField] private List<UICard> cards = new ();
        private UICard heldCard = null;
        [SerializeField] private UICard hoveredCard = null;
        public RectTransform relativeStartPoint;
        public RectTransform relativeEndPoint;
        public RectTransform activeCardPosition; // The position to place the selected card while targeting with it
        public float curveIntensity = 2f;
        public float rotationIntensity = 10f;
        public float hoveredSpread = 0.2f; 

        public float returnSpeed = 1;

        private bool _selectingCardTarget = false;

        void RefreshCards()
        {
            
        }
        
        
        public override void OnGamestateChanged()
        {
            RefreshCards();
        }

        public override void OnResyncEnded()
        {
            RefreshCards();
        }

        public override void UIUpdate()
        {
            if (UIController.PointerInputStatus == PointerInputStatus.Releasing)
            {
                heldCard = null;
                _selectingCardTarget = false;
            }
            else if (UIController.PointerInputStatus == PointerInputStatus.Pressed && UIController.UICardUnderPointer is not null)
            {
                    heldCard = UIController.UICardUnderPointer.GetComponent<UICard>();
                    _selectingCardTarget = false;
            }
            
            if (heldCard is null && UIController.UICardUnderPointer is not null)
            {
                if (hoveredCard is null || hoveredCard.gameObject != UIController.UICardUnderPointer)
                {
                    hoveredCard = UIController.UICardUnderPointer.GetComponent<UICard>();
                }
            }
            else
            {
                hoveredCard = null;
            }

            if (heldCard is not null && UIController.UICardRegionUnderPointer is null)
            {
                _selectingCardTarget = true;
            }
            
            HandleCardPositioning();
        }

        void HandleCardPositioning()
        {
            // Card positioning and logic
            Vector3 controlPoint = Vector3.Lerp(relativeStartPoint.position, relativeEndPoint.position, 0.5f) + new Vector3(0, curveIntensity, 0);
            int numCards = cards.Count;
            int indexOfHeldCard = cards.IndexOf(heldCard);
            int indexOfHoveredCard = cards.IndexOf(hoveredCard);

            for (int i = 0; i < numCards; i++)
            {
                (Vector2 preferredPosition, float preferredRotationZ, float preferredScale) = PickCardPosition(i,
                    numCards: numCards,
                    indexOfHeldCard: indexOfHeldCard,
                    indexOfHoveredCard: indexOfHoveredCard,
                    controlPoint: controlPoint);

                Quaternion preferredRotation = Quaternion.Euler(0, 0, preferredRotationZ);
/*
#if UNITY_EDITOR
                cards[i].rectTransform.localRotation = preferredRotation;
                cards[i].rectTransform.anchoredPosition = preferredPosition;
                cards[i].rectTransform.localScale = Vector2.One * preferredScale;
#else
*/
                cards[i].rectTransform.localRotation = Quaternion.Lerp(
                    cards[i].rectTransform.localRotation,
                    preferredRotation,
                    Mathf.Min(returnSpeed * Time.deltaTime, 1)
                );
                
                cards[i].rectTransform.position = Vector2.Lerp(cards[i].rectTransform.position, 
                    preferredPosition,
                    Mathf.Min(returnSpeed * Time.deltaTime, 1));

                cards[i].rectTransform.localScale = Vector3.Lerp(cards[i].rectTransform.localScale,
                    Vector3.one * preferredScale,
                    Mathf.Min(returnSpeed * Time.deltaTime * 1.5f, 1));
//#endif

            }
        }

        (Vector2 preferredPosition, float preferredRotation, float preferredScale) PickCardPosition(int i, int numCards, int indexOfHeldCard,
            int indexOfHoveredCard, Vector2 controlPoint)
        {
            if (indexOfHeldCard == i)
            {
                if (_selectingCardTarget)
                {
                    return (activeCardPosition.position, 0, 1.5f);
                }
                else
                {
                    return (UIController.PointerPositionOnScreen, 0, 1.5f);
                }
            }
            else
            {
                if (indexOfHoveredCard == -1)
                {
                    float t = i / (float)(numCards - 1); // Normalize the step along the curve
                    // If the card is NOT being held (not being dragged)
                    return (CalculateBezierPoint(t, relativeStartPoint.position, controlPoint, relativeEndPoint.position),
                        Mathf.Lerp(rotationIntensity, -rotationIntensity, t),
                        1f);
                }
                else if (indexOfHoveredCard == i)
                {
                    float t = i / (float)(numCards - 1); // Normalize the step along the curve
                    // If the card is NOT being held (not being dragged)
                    Vector3 positionOffset = new Vector3(0, 60f);
                    return (positionOffset + CalculateBezierPoint(t, relativeStartPoint.position, controlPoint, relativeEndPoint.position),
                    Mathf.Lerp(rotationIntensity, -rotationIntensity, t),
                    1.5f);
                }
                else
                {
                    float t = i / (float)(numCards - 1);
                    float distance = Mathf.Abs(i - indexOfHoveredCard);
                    if (i > indexOfHoveredCard)
                    {
                        t += hoveredSpread * Mathf.Exp(-distance);
                    }
                    else
                    {
                        t -= hoveredSpread * Mathf.Exp(-distance);
                    }
                    t = Mathf.Clamp(t, 0, 1); 

                    return (CalculateBezierPoint(t, relativeStartPoint.position, controlPoint, relativeEndPoint.position),
                        Mathf.Lerp(rotationIntensity, -rotationIntensity, t),
                        1);
                }
            }
        }

        public override bool WantsToBeActive => true;
        protected override void OnActive()
        {
            Debug.LogWarning("OnActive needs implementation");
        }

        protected override void OnHidden()
        {
            throw new System.NotImplementedException();
        }
        
        // Utility Functions
        // One control point
        Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            Vector3 p = uu * p0; //first term
            p += 2 * u * t * p1; //second term
            p += tt * p2; //third term
            return p;
        }
        // Two control points
        Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            Vector3 p = uuu * p0; //first term
            p += 3 * uu * t * p1; //second term
            p += 3 * u * tt * p2; //third term
            p += ttt * p3; //fourth term
            return p;
        }
    }
}