using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameBoard
{
    [ExecuteAlways]
    public class MapBorder : MonoBehaviour
    {
        [Serializable]
        struct VertexShare
        {
            [SerializeField] public bool enabled;
            [SerializeField] public MapBorder target;
            [SerializeField] public bool targetFirstVertex;
        }
        public Map Map;
        [SerializeField] public Vector3[] points;
        [SerializeField] [HideInInspector] private int prevNumPoints;
        public List<MapTile> connectedMapTiles = new List<MapTile>();
        public BorderType borderType;
        [SerializeField] private VertexShare shareFirstVertex;
        [SerializeField] private VertexShare shareLastVertex;

        public void Recalculate()
        {
            if (shareFirstVertex.enabled && points.Length >= 2 && shareFirstVertex.target.points.Length >= 2)
            {
                if (shareFirstVertex.targetFirstVertex)
                    points[0] = shareFirstVertex.target.points[0];
                else
                    points[0] = shareFirstVertex.target.points[^1];
            }
            if (shareLastVertex.enabled && points.Length >= 2 && shareLastVertex.target.points.Length >= 2)
            {
                if (shareLastVertex.targetFirstVertex)
                    points[^1] = shareLastVertex.target.points[0];
                else
                    points[^1] = shareLastVertex.target.points[^1];
            }
        }
        private void OnValidate()
        {
            if (points.Length != prevNumPoints)
            {
                for (int i = prevNumPoints; i < points.Length; i++)
                {
                    if (i == 0)
                        points[i] = transform.position;
                    else
                        points[i] = points[i - 1] + new Vector3(
                            0.1f,
                            0);
                }

                prevNumPoints = points.Length;
            }
            Recalculate();
        }

        public void MoveVertex(int vertex, Vector3 position)
        {
            points[vertex] = position;
            foreach (var mapTile in connectedMapTiles)
            {
                mapTile.RecalculateMesh();
            }
        }

        private void OnDisable()
        {
            Map.RecalculateMapObjectLists();
        }

        public void RecalculateAppearance()
        {
            // TODO Implement this
        }
    }

    public enum BorderType
    {
        Plains,
        Forest,
        River,
        Mountain,
        Coast,
        Sea,
        Strait,
        HornOfAfrica,
    }
}
