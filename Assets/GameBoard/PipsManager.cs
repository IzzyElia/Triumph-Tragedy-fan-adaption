using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameBoard
{
    [ExecuteAlways]
    public class PipsManager : MonoBehaviour
    {
        [SerializeField] private float pipWidth;
        [SerializeField] private float pipSpacing;
        [SerializeField] private Gradient gradient;
        [SerializeField] private SpriteRenderer spriteRenderer;
        public int maxPips;
        public int pips;
        public int projectedPips;
        [SerializeField] private SpriteRenderer[] pipRenderers = Array.Empty<SpriteRenderer>();

        [SerializeField] private int constructedPips = -1;
        public void Rebuild()
        {
            Sprite pipSprite = Resources.Load<Sprite>("Icons/Misc/Pip");
            Material pipMaterial = Resources.Load<Material>("Shaders/PipMaterial");
            
            constructedPips = maxPips;
            
            foreach (SpriteRenderer pipRenderer in pipRenderers)
            {
                DestroyImmediate(pipRenderer.gameObject);
            }
            
            pipRenderers = new SpriteRenderer[maxPips];
            if (maxPips > 0)
            {
                spriteRenderer.enabled = true;
                pipSpacing = 0.96f / maxPips;
                pipWidth = pipSpacing - 0.02f;
                float totalWidth = pipSpacing * (maxPips - 1);
                float startPosition = -(totalWidth / 2f);
                for (int i = 0; i < maxPips; i++)
                {
                    pipRenderers[i] = new GameObject($"Pip {i}", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                    pipRenderers[i].transform.SetParent(this.transform);
                    pipRenderers[i].sharedMaterial = pipMaterial;
                    pipRenderers[i].sprite = pipSprite;
                    pipRenderers[i].sortingLayerName = "HolographicUIForeground";
                    pipRenderers[i].transform.localScale = new Vector3(pipWidth, 0.23f, 1);
                    pipRenderers[i].transform.localPosition = new Vector3(startPosition + (pipSpacing * i), 0, 0);
                    pipRenderers[i].transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                spriteRenderer.enabled = false;
            }

            
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < constructedPips; i++)
            {
                if (i < pips)
                {
                    pipRenderers[i].gameObject.SetActive(true);
                    pipRenderers[i].color = gradient.Evaluate((float)(pips-1) / (float)maxPips);
                }
                else if (i < pips + projectedPips)
                {
                    pipRenderers[i].gameObject.SetActive(true);
                    Color.RGBToHSV(gradient.Evaluate((float)(pips + projectedPips - 1)), out float hue,
                        out float saturation, out float value);
                    saturation *= 0.6f;
                    value *= 0.8f;
                    pipRenderers[i].color = Color.HSVToRGB(hue, saturation, value);
                }
                else
                {
                    pipRenderers[i].gameObject.SetActive(false);

                }
            }
        }

        public void SetPips(int pips)
        {
            this.pips = pips;
            Refresh();
        }

        public void SetProjectedPips(int projectedPips)
        {
            this.projectedPips = projectedPips;
            Refresh();
        }
        public void SetMaxPips(int maxPips)
        {
            this.maxPips = maxPips;
            Rebuild();
        }
    }
}

