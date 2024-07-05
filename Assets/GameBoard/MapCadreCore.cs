using System;
using System.Collections.Generic;
using GameSharedInterfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameBoard
{

    public abstract class MapCadreCore : MapObject, IMapToken
    {
        private static GameObject _arrowPrefab = null;
        public const int CadreLayer = 6;
        
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private PipsManager pipsManager;
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

        public Vector3 TokenPosition => Destination;

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
        
        public int Pips
        {
            get => pipsManager.pips;
            set
            {
                if (value != pipsManager.pips)
                {
                    pipsManager.SetPips(value);
                    RecalculateAppearance();
                }
            }
        }
        public int ProjectedPips
        {
            get => pipsManager.projectedPips;
            set
            {
                if (value != pipsManager.projectedPips)
                {
                    pipsManager.SetProjectedPips(value);
                    RecalculateAppearance();
                }
            }
        }
        public int MaxPips
        {
            get => pipsManager.maxPips;
            set
            {
                if (value != pipsManager.maxPips)
                {
                    pipsManager.SetMaxPips(value);
                    RecalculateAppearance();
                }
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

        public void SetPositionUnanimated(Vector3 position)
        {
            Destination = position;
            transform.position = position;
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
                        float distance = Vector3.Distance(point, token.TokenPosition) / tokenAvoidanceWeight;
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
            /*
            if (cascade && closestCadreToBestPoint is not null)
            {
                Vector3 storedPosition = transform.position;
                SetPositionUnanimated(bestPoint);
                closestCadreToBestPoint.ChoosePosition(tile, false);
                transform.position = storedPosition;
            } */
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

        public bool AnimatingMovement;
        protected override void OnTileChanged(MapTile tile)
        {
            base.OnTileChanged(tile);
            if (!AnimatingMovement) SetPositionUnanimated(ChoosePosition(tile));
        }


        // Appearance
        public float baseScale = 0.15f;
        private const float ZFactor = 0.35f;
        [NonSerialized] public float scaleFactor = 1;
        public float ActualScale => baseScale * scaleFactor;
        private float BaseZSize => baseScale * ZFactor;
        private float ZSize => ActualScale * ZFactor;
        public SelectionStatus HoveredOver { get; set; }
        public bool Darken;
        protected bool UseGhostMaterial;
        
        // Arrow
        private GameObject _arrow;
        private GameObject _arrowTarget;

        public void RecalculateArrowTarget()
        {
            if (Map.GameState.GamePhase == GamePhase.SelectSupport)
            {
                if (IsHoveredOver && Map.UIController.CombatMarkerSelectedForSupport is not null && Map.UIController.CombatMarkerSelectedForSupport.SupportOptions.Contains(this.ID))
                {
                    SetArrowTarget(Map.MapTilesByID[Map.UIController.CombatMarkerSelectedForSupport.CombatOption.iTile].gameObject);
                }
                else if (Map.UIController.SupportUnitSelections.TryGetValue(this.ID, out CombatOption combat))
                {
                    SetArrowTarget(Map.MapTilesByID[combat.iTile].gameObject);
                }
                else
                {
                    SetArrowTarget(null);
                }
            }
            else if (Map.GameState.GamePhase == GamePhase.SelectNextCombat)
            {
                if (Map.GameState.CombatSupports.TryGetValue(this.ID, out int iTile))
                {
                    MapTile tile = Map.MapTilesByID[iTile];
                    SetArrowTarget(tile.gameObject);
                }
                else
                {
                    SetArrowTarget(null);
                }
            }
        }
        private void SetArrowTarget(GameObject arrowTarget)
        {
            if (_arrow is null && arrowTarget is not null)
            {
                if (_arrowPrefab is null) _arrowPrefab = Resources.Load<GameObject>("Prefabs/Arrow");
                _arrow = Instantiate(_arrowPrefab);
                _arrow.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", Color.blue);
                _arrow.transform.SetParent(Map.transform);
            }
            if (arrowTarget is not null)
            {
                Vector3 midpoint = (transform.position + arrowTarget.transform.position) / 2f;
                _arrow.transform.position = new Vector3(midpoint.x, midpoint.y, -0.1f);

                Vector3 direction = arrowTarget.transform.position - transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _arrow.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle - 90f));

                float distance = Vector3.Distance(transform.position, arrowTarget.transform.position);
                _arrow.transform.localScale = new Vector3(0.12f, distance/2f, 1);

                if (arrowTarget != _arrowTarget)
                {
                    _arrow.GetComponent<MeshRenderer>().material.SetFloat("_AnimationStartTime", Time.timeSinceLevelLoad);
                }
            }
            if (_arrow is not null && arrowTarget is null)
            {
                Destroy(_arrow);
                _arrow = null;
            }
            _arrowTarget = arrowTarget;
        }
        // /Arrow/
        
        public bool IsHoveredOver;
        public override void OnHoveredStatusChanged(bool isHoveredOver)
        {
            base.OnHoveredStatusChanged(isHoveredOver);
            this.IsHoveredOver = isHoveredOver;
            RecalculateArrowTarget();
            RecalculateAppearance();
        }

        public override void OnSelectionStatusChanged(SelectionStatus selectionStatus)
        {
            base.OnSelectionStatusChanged(selectionStatus);
            RecalculateAppearance();
        }

        private bool _highlighted = false;
        public Color highlightColor;
        public Color supportHighlightColor;
        public virtual void RecalculateAppearance()
        {
            meshFilter.sharedMesh = Map.CadreBlockMesh;
            transform.localScale = new Vector3(ActualScale, ActualScale, ZSize);
            if (MapCountry is not null)
            {
                meshRenderer.sharedMaterial = MapCountry.GetMaterialForCadreUnitType(UnitType, UseGhostMaterial);
            }

            if (Darken)
                meshRenderer.material.SetColor("_BaseColor", Color.gray);
            else meshRenderer.material.SetColor("_BaseColor", Color.white);

            if (Map.SelectedObjects.Contains(this) || Map.HoveredMapObject == this)
            {
                meshRenderer.material.SetColor("_HighlightColor", highlightColor);
                if (Map.GameState.GamePhase == GamePhase.Production &&
                    Map.GameState.ActivePlayer == Map.GameState.iPlayer &&
                    Map.HoveredMapObject == this)
                {
                    meshRenderer.material.SetInt("_ShowAddPipsOverlay", 1);
                    meshRenderer.material.SetInt("_AddPipsOverlayIsPositive",
                        Map.UIController.ModifierInputStatus == InputStatus.Held ||
                        Map.UIController.ModifierInputStatus == InputStatus.Pressed
                            ? 0
                            : 1);
                    meshRenderer.material.SetInt("_AddPipsOverlayIsHighlighted", 1);
                }
            }
            else if (Map.UIController.CombatMarkerSelectedForSupport is not null && Map.UIController.CombatMarkerSelectedForSupport.SupportOptions.Contains(this.ID))
            {
                meshRenderer.material.SetColor("_HighlightColor", supportHighlightColor);
            }
            else
            {
                meshRenderer.material.SetColor("_HighlightColor", Color.clear);
                meshRenderer.material.SetInt("_ShowAddPipsOverlay", 0);
            }
        }

        public override bool IsSelectable => true;
    }
}