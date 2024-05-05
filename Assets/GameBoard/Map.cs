using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameBoard
{
    [ExecuteAlways]
    public class Map : MonoBehaviour
    {
        private Dictionary<string, MapBorder> _mapBorders = new Dictionary<string, MapBorder>();
        private Dictionary<string, MapTile> _mapSpaces = new Dictionary<string, MapTile>();
        private Dictionary<string, Country> _countries = new Dictionary<string, Country>();
        public GameObject mapBorderWrapper;
        public GameObject countriesWrapper;
        private bool _fullRecalculationCalled = false;
        private bool _bordersRecalculationCalled = false;
        private bool _appearanceRecalculationCalled = false;

        private void OnEnable()
        {
            //RecalculateMapObjectLists();
        }

        private void OnValidate()
        {
            //RecalculateMapObjectLists();
        }

        private void Update()
        {
            if (_fullRecalculationCalled)
            {
                Debug.Log("Full recalculation");
                _mapBorders.Clear();
                _mapSpaces.Clear();
                _countries.Clear();
                MapBorder[] mapBorders = mapBorderWrapper.GetComponentsInChildren<MapBorder>();
                Country[] countries = countriesWrapper.GetComponentsInChildren<Country>();
                MapTile[] mapSpaces = countriesWrapper.GetComponentsInChildren<MapTile>();

                foreach (var border in mapBorders)
                {
                    border.Map = this;
                    border.Recalculate();
                    _mapBorders.Add(border.gameObject.name, border);
                }
            
                foreach (var mapSpace in mapSpaces)
                {
                    if (mapSpace.TryGetComponent<Country>(out Country country))
                        mapSpace.country = country;
                    else
                        mapSpace.country = null;
                    mapSpace.Map = this;
                    mapSpace.Recalculate();
                    _mapSpaces.Add(mapSpace.gameObject.name, mapSpace);
                }
            }

            if (_bordersRecalculationCalled)
            {
                Debug.Log("recalculating map connections");
                foreach (var mapSpace in this._mapSpaces.Values)
                {
                    mapSpace.connectedSpaces.Clear();
                }
                foreach (var border in _mapBorders.Values)
                {
                    border.connectedMapTiles.Clear();
                }

                HashSet<MapBorder> referencedBorders = new HashSet<MapBorder>();
                foreach ((string tileName, MapTile tile) in _mapSpaces)
                {
                    foreach (var borderRef in new List<MapTile.BorderReference>(tile.connectedBorders))
                    {
                        bool foundBorderObject = false;
                        foreach ((string borderName, MapBorder border) in _mapBorders)
                        {
                            if (borderName.Split('-').Contains(tileName) && border == borderRef.border)
                            {
                                foundBorderObject = true;
                                break;
                            }
                        }
                        if (!foundBorderObject)
                            tile.connectedBorders.Remove(borderRef);
                    }

                    foreach ((string borderName, MapBorder border) in _mapBorders)
                    {
                        foreach (string connectedTileName in borderName.Split('-'))
                        {
                            if (connectedTileName == tileName)
                            {
                                bool foundBorderReference = false;
                                foreach (var borderRef in tile.connectedBorders)
                                {
                                    if (borderRef.border == border)
                                    {
                                        foundBorderReference = true;
                                        break;
                                    }
                                }
                                if (!foundBorderReference)
                                    tile.connectedBorders.Add(new MapTile.BorderReference(border));
                            }
                        }
                    }
                }

                foreach (var tile in _mapSpaces.Values)
                {
                    foreach (var borderRef in tile.connectedBorders)
                    {
                        borderRef.border.connectedMapTiles.Add(tile);
                    }
                }

                foreach (var tile in _mapSpaces.Values)
                {
                    foreach (var borderRef in tile.connectedBorders)
                    {
                        foreach (var connectedTile in borderRef.border.connectedMapTiles)
                        {
                            if (connectedTile != tile)
                                tile.connectedSpaces.Add(connectedTile);
                        }
                    }
                }
            }

            if (_appearanceRecalculationCalled)
            {
                foreach (var mapTile in _mapSpaces.Values)
                {
                    mapTile.RecalculateAppearance();
                }
                foreach (var border in _mapBorders.Values)
                {
                    border.RecalculateAppearance();
                }
            }

            _fullRecalculationCalled = false;
            _bordersRecalculationCalled = false;
            _appearanceRecalculationCalled = false;
        }

        public void RecalculateMapObjectLists()
        {
            _fullRecalculationCalled = true;
            _bordersRecalculationCalled = true;
            _appearanceRecalculationCalled = true;
        }

        public void RecalculateMapConnections()
        {
            _bordersRecalculationCalled = true;
            _appearanceRecalculationCalled = true;
        }

        public void RecalculateMapAppearance()
        {
            _appearanceRecalculationCalled = true;
        }
    }
}
