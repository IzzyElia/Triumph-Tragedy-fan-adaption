using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameBoard
{

    public abstract class MapCadreCore : MapObject, IMapToken
    {
        public const int CadreLayer = 6;
        
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        public Vector3 Destination;

        private void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        protected MapCountry _mapCountry;
        public MapCountry MapCountry
        {
            get => _mapCountry;
            set
            {
                _mapCountry = value;
                OnCountryChanged();
            }
        }
        protected MapTile _tile;
        public MapTile Tile
        {
            get => _tile;
            set
            {
                _tile = value;
                OnTileChanged(value);
            }
        }

        protected UnitType  _unitType;
        public UnitType UnitType
        {
            get => _unitType;
            set
            {
                _unitType = value;
                RecalculateAppearance();
            }
        }
        
        protected int  _pips;
        public int Pips
        {
            get => _pips;
            set
            {
                _pips = value;
                RecalculateAppearance();
            }
        }

        private List<Vector3> _points = new List<Vector3>();
        public Vector3 ChooseRandomPosition(MapTile tile)
        {
            (Vector2 boundingBoxMin, Vector2 boundingBoxMax) = tile.GetMeshBoundingBox();
            float precision = Mathf.Max(boundingBoxMax.x - boundingBoxMin.x, boundingBoxMax.y - boundingBoxMin.y) / 30f;
            _points.Clear();
            for (float x = boundingBoxMin.x; x <= boundingBoxMax.x; x += precision)
            {
                for (float y = boundingBoxMin.y; y <= boundingBoxMax.y; y += precision)
                {
                    Vector3 point = new Vector3(x, y, 0);
                    if (!tile.IsPointInside(point))
                    {
                        _points.Add(point);
                    }
                }
            }
            return _points[Random.Range(0, _points.Count)];
        }
        public Vector3 ChoosePosition(MapTile tile, bool cascade = true)
        {
            const float tokenAvoidanceWeight = 2f;
            const float borderAvoidanceWeight = 1f;
            (Vector2 boundingBoxMin, Vector2 boundingBoxMax) = tile.GetMeshBoundingBox();
             float precision = Mathf.Max(boundingBoxMax.x - boundingBoxMin.x, boundingBoxMax.y - boundingBoxMin.y) / 30f;
            Vector3 bestPoint = Vector3.zero;
            float closestObjDistanceOfBestPoint = 0f;
            MapCadre closestCadreToBestPoint = null;
            IMapToken[] tileTokens = Map.GetTokensOnTileExcept<IMapToken>(tile, exception:this);
            Vector3 tilePosition = tile.transform.position;
            (Vector3[] borderVertices, Vector3[] holeVertices) = tile.GetVertices();

            for (float x = boundingBoxMin.x; x <= boundingBoxMax.x; x += precision)
            {
                for (float y = boundingBoxMin.y; y <= boundingBoxMax.y; y += precision)
                {
                    Vector3 point = new Vector3(x, y, 0);
                    if (!tile.IsPointInside(point))
                    {
                        continue;
                    }

                    float closestObjDistance = float.PositiveInfinity;
                    MapCadre closestCadre = null;

                    if (borderVertices.Length >= 2)
                    {
                        for (int i = 0; i < borderVertices.Length; i++)
                        {
                            Vector3 vertex;
                            Vector3 prevVertex;
                            if (i == 0)
                            {
                                vertex = borderVertices[i];
                                prevVertex = borderVertices[^1];
                            }
                            else
                            {
                                vertex = borderVertices[i];
                                prevVertex = borderVertices[i - 1];
                            }

                            // Calculate the distance to the edge created by the two vertices
                            float distance = DistancePointToSegment(point, prevVertex, vertex) / borderAvoidanceWeight;
                            if (distance < closestObjDistance)
                                closestObjDistance = distance;
                        }
                    }

                    if (holeVertices.Length >= 2)
                    {
                        for (int i = 0; i < holeVertices.Length; i++)
                        {
                            Vector3 vertex;
                            Vector3 prevVertex;
                            if (i == 0)
                            {
                                vertex = holeVertices[i];
                                prevVertex = holeVertices[^1];
                            }
                            else
                            {
                                vertex = holeVertices[i - 1];
                                prevVertex = holeVertices[i];
                            }

                            // Calculate the distance to the edge created by the two vertices
                            float distance = DistancePointToSegment(point, prevVertex, vertex) / borderAvoidanceWeight;
                            if (distance < closestObjDistance)
                                closestObjDistance = distance;
                        }
                    }

                    foreach (IMapToken token in tileTokens)
                    {
                        float distance = Vector3.Distance(point, token.transform.position) / tokenAvoidanceWeight;
                        if (distance < closestObjDistance)
                        {
                            closestObjDistance = distance;
                            if (token is MapCadre cadre)
                            {
                                closestCadre = cadre;
                            }
                        }
                    }

                    if (closestObjDistance >= closestObjDistanceOfBestPoint)
                    {
                        closestObjDistanceOfBestPoint = closestObjDistance;
                        bestPoint = point;
                        closestCadreToBestPoint = closestCadre;
                    }
                }
            }
            
            bestPoint = new Vector3(bestPoint.x, bestPoint.y, -BaseZSize * 0.5f);
            if (cascade && closestCadreToBestPoint is not null)
            {
                Vector3 storedPosition = transform.position;
                transform.position = bestPoint;
                closestCadreToBestPoint.ChoosePosition(tile, false);
                transform.position = storedPosition;
            }
            return bestPoint;
        }
        private float DistancePointToSegment(Vector3 point, Vector3 v, Vector3 w)
        {
            float l2 = (v - w).sqrMagnitude; // Length squared of the segment
            if (l2 == 0.0) return Vector3.Distance(point, v); // v == w case

            float t = Mathf.Clamp01(Vector3.Dot(point - v, w - v) / l2);
            Vector3 projection = v + t * (w - v);
            return Vector3.Distance(point, projection);
        }

        protected virtual void OnCountryChanged()
        {
            transform.SetParent(_mapCountry.transform);
            RecalculateAppearance();
        }

        protected override void OnTileChanged(MapTile tile)
        {
            base.OnTileChanged(tile);
            transform.position = ChoosePosition(tile);
        }


        // Appearance
        public float baseScale = 0.15f;
        private const float ZFactor = 0.35f;
        [NonSerialized] public float scaleFactor = 1;
        public float ActualScale => baseScale * scaleFactor;
        private float BaseZSize => baseScale * ZFactor;
        private float ZSize => ActualScale * ZFactor;
        public SelectionStatus HoveredOver { get; set; }

        private Queue<int> movementQueue = new Queue<int>();
        public void AnimateMovement(int[] path)
        {
            // TODO full movement animation
            int iTile = path[^1];
            Destination = ChoosePosition(Map.MapTilesByID[iTile], true);
            SetAnimated(true);
        }
        
        private float _momentum = 0;
        private const float MaxMomentum = 0.15f;
        public override void Animate()
        {
            bool StillNeedsAnimation = false;
            
            if (Vector3.Distance(transform.localPosition, Destination) <= 0.01f)
            {
                if (movementQueue.TryDequeue(out int iTile))
                {
                    if (movementQueue.Count > 0) Destination = ChooseRandomPosition(Map.MapTilesByID[iTile]);
                    else Destination = ChoosePosition(Map.MapTilesByID[iTile], true);
                    StillNeedsAnimation = true;
                    _momentum = 0;
                }
            }
            else
            {
                StillNeedsAnimation = true;
            }
            
            _momentum = Mathf.Min(_momentum + 0.003f, MaxMomentum);
            transform.localPosition = Vector3.Lerp(transform.localPosition, Destination, _momentum);
            
            if (!StillNeedsAnimation)
            {
                SetAnimated(false);
            }
        }

        public override void ConcludeAnimation()
        {
            transform.position = Destination;
        }

        public override void OnHoveredStatusChanged(bool isHoveredOver)
        {
            base.OnHoveredStatusChanged(isHoveredOver);
            RecalculateAppearance();
        }

        public override void OnSelectionStatusChanged(SelectionStatus selectionStatus)
        {
            base.OnSelectionStatusChanged(selectionStatus);
            RecalculateAppearance();
        }

        private bool _highlighted = false;
        public virtual void RecalculateAppearance()
        {
            meshFilter.sharedMesh = Map.CadreBlockMesh;
            transform.localScale = new Vector3(ActualScale, ActualScale, ZSize);
            if (MapCountry is not null)
            {
                meshRenderer.sharedMaterial = MapCountry.GetMaterialForCadreUnitType(UnitType);
            }

            if (Map.SelectedObject == this)
            {
                meshRenderer.material.SetInt("_Highlighted", 1);
            }
            else if (Map.HoveredMapObject == this)
            {
                meshRenderer.material.SetInt("_Highlighted", 1);
            }
            else
            {
                meshRenderer.material.SetInt("_Highlighted", 0);
            }
        }
    }
}