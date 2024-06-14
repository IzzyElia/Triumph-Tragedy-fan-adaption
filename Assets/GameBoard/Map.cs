using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameSharedInterfaces;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace GameBoard
{
    public enum SelectionStatus
    {
        Selected,
        Unselected,
        Dragged
    }
    [ExecuteAlways]
    public class Map : MonoBehaviour
    {
        public static GameObject LoadMap(string path)
        {
            return Resources.Load<GameObject>(Path.Combine("Maps", path));
        }
        
        [SerializeField] private string fileName;
        public int startingYear = 1936;
        public MapBorder[] MapBordersByID;
        public MapTile[] MapTilesByID;
        public MapCountry[] MapCountriesByID;
        public MapCadre[] MapCadresByID;
        public MapFaction[] MapFactionsByID = Array.Empty<MapFaction>();
        public List<MapObject> MapObjects = new List<MapObject>();
        public List<IMapToken> MapTokens = new List<IMapToken>();
        private List<MapObject> ObjectsNeedingAnimation = new List<MapObject>();
        public AnimationCurve unitMovementAnimationCurve;
        public GameObject mapBorderWrapper;
        public GameObject countriesWrapper;
        private bool _fullRecalculationCalled = false;
        private bool _bordersRecalculationCalled = false;
        private bool _objectListRecalculationCalled = false;
        public Mesh fallbackMapTileMesh;
        public float borderMeshWidth;
        public int MaxCadres = byte.MaxValue;
        [NonSerialized] public ITTGameState GameState;
        public IRuleset Ruleset => GameState.Ruleset;
        public Mesh CadreBlockMesh;
        [NonSerialized] public MapObject SelectedObject; // Set by UIController
        [NonSerialized] public MapObject HoveredMapObject;
        [NonSerialized] public MapTile HoveredOverTile; // Set by UIController
        [NonSerialized] public SelectionStatus SelectionStatus; // Set by UIController



        private void Start()
        {
            CadreBlockMesh = Resources.Load<Mesh>("Meshes/CadreBlock");
            MapCadresByID = new MapCadre[MaxCadres];
            RecalculateMapObjectLists();
        }
        private void OnValidate()
        {
            MapCadresByID = new MapCadre[MaxCadres];
        }

        
        
        public T[] GetTokensOnTile<T>(MapTile tile) where T : IMapToken
        {
            List<T> objects = new List<T>();
            foreach (IMapToken mapToken in MapTokens)
            {
                T obj = (T)mapToken;
                if (obj.Tile == tile)
                {
                    objects.Add(obj);
                }
            }
            return objects.ToArray();
        }
        public T[] GetTokensOnTileExcept<T>(MapTile tile, IMapToken exception) where T : IMapToken
        {
            List<T> objects = new List<T>();
            foreach (IMapToken mapToken in MapTokens)
            {
                T obj = (T)mapToken;
                if (obj.Tile == tile && mapToken != exception)
                {
                    objects.Add(obj);
                }
            }
            return objects.ToArray();
        }

        public MapCadre GetCadreByID(int id)
        {
            try
            {
                return MapCadresByID[id];

            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"No Cadre renderer with id {id}");
            }
        }

        public void DestroyCadreWithID(int id)
        {
            try
            {
                MapCadre cadre = MapCadresByID[id];
                MapCadresByID[id] = null;
                Destroy(cadre.gameObject);
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidOperationException($"No Cadre renderer with id {id}");
            }
        }
        
        public void SaveToFile()
        {
            StreamWriter writer = File.CreateText(Application.dataPath + '/' + fileName + ".txt");
            Debug.Log($"Saving map to {Application.dataPath + '/' + fileName + ".txt"}");
            writer.WriteLine("!Tiles");
            foreach (MapTile mapTile in MapTilesByID)
            {
                writer.WriteLine($"@{mapTile.name}#{mapTile.ID}");
                if (!(mapTile.mapCountry is null)) writer.WriteLine($"country-{mapTile.mapCountry.ID}");
                writer.WriteLine($"resources-{mapTile.resources}");
                writer.WriteLine($"colonialResources-{mapTile.colonialResources}");
                writer.WriteLine($"citySize-{mapTile.citySize}");
                writer.WriteLine($"terrainType-{mapTile.terrainType}");
                foreach (var borderReference in mapTile.connectedBorders)
                {
                    MapTile otherTile = null;
                    foreach (var connectedTile in borderReference.border.connectedMapTiles)
                    {
                        if (connectedTile != mapTile)
                        {
                            otherTile = connectedTile;
                            break;
                        }
                    }

                    if (!(otherTile is null))
                    {
                        writer.WriteLine($"border-{borderReference.border.ID}");
                    }
                }
                writer.WriteLine();
            }
            writer.WriteLine("!Borders");
            foreach (MapBorder mapBorder in MapBordersByID)
            {
                writer.WriteLine($"@{mapBorder.name}#{mapBorder.ID}");
                writer.WriteLine($"borderType-{mapBorder.borderType}");
            }
            writer.WriteLine("!Countries");
            foreach (MapCountry mapCountry in MapCountriesByID)
            {
                writer.WriteLine($"@{mapCountry.name}#{mapCountry.ID}");
                if (!(mapCountry.colonialOverlord is null))
                    writer.WriteLine($"colonyOf-{mapCountry.colonialOverlord.name}");
            }
            // Write map data here
            writer.Close();
        }
        
        void RecalculateMapObjectLists()
        {
            MapObjects.Clear();
            MapObjects.Clear();
            MapTokens.Clear();
            List<MapTile> indexedMapTiles = new List<MapTile>();
            List<MapCountry> indexedMapCountries = new List<MapCountry>();
            List<MapBorder> indexedMapBorders = new List<MapBorder>();
            foreach (MapObject mapObject in GetComponentsInChildren<MapObject>())
            {
                MapObjects.Add(mapObject);
                if (mapObject is IMapToken mapToken) MapTokens.Add(mapToken);
                mapObject.Map = this;
                if (mapObject is MapBorder border)
                {
                    indexedMapBorders.Add(border);
                }
                else if (mapObject is MapTile mapSpace)
                {
                    MapCountry country = mapSpace.GetComponentInParent<MapCountry>();
                    mapSpace.mapCountry = country;
                    indexedMapTiles.Add(mapSpace);
                }
                else if (mapObject is MapCountry mapCountry)
                {
                    MapFaction faction = mapCountry.GetComponentInParent<MapFaction>();
                    mapCountry.faction = faction;
                    indexedMapCountries.Add(mapCountry);
                }

            }

            MapTilesByID = indexedMapTiles.ToArray();
            MapBordersByID = indexedMapBorders.ToArray();
            MapCountriesByID = indexedMapCountries.ToArray();
            for (int i = 0; i < MapTilesByID.Length; i++)
            {
                MapTilesByID[i].ID = i;
            }

            for (int i = 0; i < MapBordersByID.Length; i++)
            {
                MapBordersByID[i].ID = i;
            }

            for (int i = 0; i < MapCountriesByID.Length; i++)
            {
                MapCountriesByID[i].ID = i;
            }
        }

        private void Update()
        {
            foreach (var mapObject in ObjectsNeedingAnimation)
            {
                mapObject.Animate();
            }
            
            // Editor
#if UNITY_EDITOR
            if (_objectListRecalculationCalled)
            {
                RecalculateMapObjectLists();
            }
            
            if (_fullRecalculationCalled)
            {
                Debug.Log("Full recalculation");
                foreach (var border in MapBordersByID)
                {
                    border.Recalculate();
                }
                foreach (var mapTile in MapTilesByID)
                {
                    mapTile.Recalculate();
                }


            }

            if (_bordersRecalculationCalled)
            {
                Debug.Log("recalculating map connections");
                foreach (var mapSpace in this.MapTilesByID)
                {
                    mapSpace.connectedSpaces.Clear();
                }
                foreach (var border in MapBordersByID)
                {
                    border.connectedMapTiles.Clear();
                }

                HashSet<MapBorder> referencedBorders = new HashSet<MapBorder>();
                foreach (MapTile tile in MapTilesByID)
                {
                    foreach (var borderRef in new List<MapTile.BorderReference>(tile.connectedBorders))
                    {
                        bool foundBorderObject = false;
                        foreach (MapBorder border in MapBordersByID)
                        {
                            if (border.name.Split('-').Contains(tile.name) && border == borderRef.border)
                            {
                                foundBorderObject = true;
                                break;
                            }
                        }
                        if (!foundBorderObject)
                            tile.connectedBorders.Remove(borderRef);
                    }

                    foreach (MapBorder border in MapBordersByID)
                    {
                        foreach (string connectedTileName in border.name.Split('-'))
                        {
                            if (connectedTileName == tile.name)
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

                foreach (var tile in MapTilesByID)
                {
                    foreach (var borderRef in tile.connectedBorders)
                    {
                        borderRef.border.connectedMapTiles.Add(tile);
                    }
                }

                foreach (var tile in MapTilesByID)
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

            
            if (_fullRecalculationCalled)
            {
                // TEMP - auto-set border types
                foreach (MapBorder border in MapBordersByID)
                {
                    if (border.connectedMapTiles.Count != 2)
                    {
                        border.borderType = BorderType.Impassable;
                        continue;
                    }

                    MapTile firstTile = border.connectedMapTiles[0];
                    MapTile secondTile = border.connectedMapTiles[1];
                    // Is it impassible?
                    if (firstTile.terrainType == MapTile.TerrainType.NotInPlay ||
                        secondTile.terrainType == MapTile.TerrainType.NotInPlay)
                    {
                        border.borderType = BorderType.Impassable;
                        continue;
                    }
                    
                    // Is it a strait?
                    if (firstTile.terrainType == MapTile.TerrainType.Strait ||
                        secondTile.terrainType == MapTile.TerrainType.Strait)
                    {
                        border.borderType = BorderType.Strait;
                        continue;
                    }
                    
                    // Is it water-to-water?
                    if (
                        (firstTile.terrainType == MapTile.TerrainType.Sea || firstTile.terrainType == MapTile.TerrainType.Ocean) &&
                        (secondTile.terrainType == MapTile.TerrainType.Sea || secondTile.terrainType == MapTile.TerrainType.Ocean))
                    {
                        border.borderType = BorderType.Sea;
                        continue;
                    }
                    
                    // Is it a coast?
                    if (
                            (
                                (firstTile.terrainType == MapTile.TerrainType.Sea || firstTile.terrainType == MapTile.TerrainType.Ocean) 
                                &&
                                (secondTile.terrainType != MapTile.TerrainType.Sea && secondTile.terrainType != MapTile.TerrainType.Ocean)
                            )
                            ||
                            (
                                (firstTile.terrainType != MapTile.TerrainType.Sea && firstTile.terrainType != MapTile.TerrainType.Ocean) 
                                &&
                                (secondTile.terrainType == MapTile.TerrainType.Sea || secondTile.terrainType == MapTile.TerrainType.Ocean)
                                
                            )
                        )
                    {
                        border.borderType = BorderType.Coast;
                        continue;
                    }
                    
                    // It must be land. Leave it be
                    continue;
                }
            }
            
            


#endif
            _fullRecalculationCalled = false;
            _bordersRecalculationCalled = false;
            _objectListRecalculationCalled = false;
        }
        
        public void FullyRecalculate()
        {
            _fullRecalculationCalled = true;
            _bordersRecalculationCalled = true;
            _objectListRecalculationCalled = true;
        }

        public void RecalculateMapConnections()
        {
            _bordersRecalculationCalled = true;
        }

        public void RegisterObject(MapObject mapObject)
        {
            MapObjects.Add(mapObject);

            if (mapObject is IMapToken mapToken)
            {
                MapTokens.Add(mapToken);
            }
        }

        public void DeregisterObject(MapObject mapObject) // Should only ever be called through MapObject.Deregister
        {
            MapObjects.Remove(mapObject);
            ObjectsNeedingAnimation.Remove(mapObject);
            if (mapObject is MapCadre cadre)
            {
                MapCadresByID[cadre.ID] = null;
            }
            
            if (mapObject is MapFaction faction)
            {
                MapFactionsByID[faction.ID] = null;
            }

            if (mapObject is IMapToken mapToken)
            {
                MapTokens.Remove(mapToken);
            }
        }

        public void AddObjectToAnimationList(MapObject mapObject)
        {
            if (!ObjectsNeedingAnimation.Contains(mapObject))
                ObjectsNeedingAnimation.Add(mapObject);
        }

        public void RemoveObjectFromAnimationList(MapObject mapObject)
        {
            ObjectsNeedingAnimation.Remove(mapObject);
            mapObject.ConcludeAnimation();
        }

        public void RecalculateAppearanceAfterResync()
        {
            foreach (var mapCountry in MapCountriesByID)
            {
                mapCountry.RecalculateFlag();
            }

            foreach (var mapCadre in MapCadresByID)
            {
                if (mapCadre is not null) mapCadre.RecalculateAppearance();
            }
        }
    }
}
