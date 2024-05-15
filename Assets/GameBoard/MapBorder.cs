using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;
using UnityEditor;

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
        [SerializeField] public Vector3[] points = new Vector3[0];
        [SerializeField] [HideInInspector] private int prevNumPoints;
        public List<MapTile> connectedMapTiles = new List<MapTile>();
        public BorderType borderType;
        [SerializeField] private VertexShare shareFirstVertex;
        [SerializeField] private VertexShare shareLastVertex;
        string FilePath => $"{Application.dataPath}/map/{name}.txt";

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
            EditorUtility.SetDirty(this);
            //Save();
        }
        
        private void OnValidate()
        {
            /*
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
            */
            Recalculate();
        }

        public void Save()
        {
            
            StreamWriter fileWriter = File.CreateText(FilePath);;
            fileWriter.WriteLine(points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                fileWriter.WriteLine($"{points[i].x},{points[i].y}");
            }
            fileWriter.Close();
        }

        public bool TryLoad()
        {
            StreamReader fileReader = new StreamReader(FilePath);
            string sPointsLength = fileReader.ReadLine();
            Vector3[] newPoints;
            if (int.TryParse(sPointsLength, out int pointsLength))
            {
                
                newPoints = new Vector3[pointsLength];
                int i = 0;
                while (!fileReader.EndOfStream)
                {
                    string[] sPoint = fileReader.ReadLine().Split(',');
                    if (float.TryParse(sPoint[0], out float x) && float.TryParse(sPoint[1], out float y))
                    {
                        newPoints[i] = new Vector3(x, y, 0);
                    }
                    else
                    {
                        fileReader.Close();
                        return false;
                    }
                    i++;
                }
            }
            else
            {
                fileReader.Close();
                return false;
            }

            fileReader.Close();
            points = newPoints;
            return true;
        }

        public void MoveVertex(int vertex, Vector3 position)
        {
            points[vertex] = position;
            foreach (var mapTile in connectedMapTiles)
            {
                //mapTile.RecalculateMesh();
            }
            EditorUtility.SetDirty(this);
        }

        private void OnEnable()
        {
            //TryLoad();
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
        Impassable,
        Unspecified,
    }
}
