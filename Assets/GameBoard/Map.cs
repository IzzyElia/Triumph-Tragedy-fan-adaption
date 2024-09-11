using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace GameBoard
{
    public enum SelectionStatus
    {
        Selected,
        Unselected,
        Dragged
    }

    public enum MapMode
    {
        Political,
        Diplomacy,
        Supply,
        Trade,
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
        public GameObject mapBackground;
        public int mapBackgroundSubdivisions;
        private bool _fullRecalculationCalled = false;
        private bool _bordersRecalculationCalled = false;
        private bool _objectListRecalculationCalled = false;
        public Mesh fallbackMapTileMesh;
        public int borderMeshWidth;
        public int MaxCadres = byte.MaxValue;
        [NonSerialized] public ITTGameState GameState;
        public Ruleset Ruleset => GameState.Ruleset;
        public int iPlayer => GameState.iPlayer;

        public Mesh CadreBlockMesh;
        [NonSerialized] public MapObject SelectedObject = null; // Set by UIController
        [NonSerialized] public MapObject HoveredMapObject;
        [NonSerialized] public IUIController UIController;
        public MapMode MapMode { get; private set; }
        
        public HashSet<int> GreenlitTiles = new HashSet<int>();
        public HashSet<int> PrevGreenlitTiles = new HashSet<int>();
        public Color GreenlitColor = new Color(0.4f, 1f, 0.6f);
        public HashSet<int> YellowlitTiles = new HashSet<int>();
        public HashSet<int> PrevYellowlitTiles = new HashSet<int>();
        public Color YellowlitColor = new Color(1, 0.8f, 0.6f);
        public HashSet<int> RedlitTiles = new HashSet<int>();
        public HashSet<int> PrevRedlitTiles = new HashSet<int>();
        public Color RedlitColor = new Color(0.6f, 0.2f, 0.1f);
        public HashSet<int> BluelitTiles = new HashSet<int>();
        public HashSet<int> PrevBluelitTiles = new HashSet<int>();
        public Color BluelitColor = new Color(0.2f, 0.6f, 0.9f);
        private HashSet<int> _highlightChangedTiles = new HashSet<int>();

        enum TileHighlightColor
        {
            None,
            Green,
            Yellow,
            Red,
            Blue
        }
        
        void FlushTileHighlighting(Color greenlitColor = default, Color yellowlitColor = default, Color redlitColor = default, Color bluelitColor = default)
        {
            PrevGreenlitTiles.Clear();
            foreach (var tile in GreenlitTiles) PrevGreenlitTiles.Add(tile);
            GreenlitTiles.Clear();
            GreenlitColor = greenlitColor;

            PrevYellowlitTiles.Clear();
            foreach (var tile in YellowlitTiles) PrevYellowlitTiles.Add(tile);
            YellowlitTiles.Clear();
            YellowlitColor = yellowlitColor;

            PrevRedlitTiles.Clear();
            foreach (var tile in RedlitTiles) PrevRedlitTiles.Add(tile);
            RedlitTiles.Clear();
            RedlitColor = redlitColor;
            
            PrevBluelitTiles.Clear();
            foreach (var tile in BluelitTiles) PrevBluelitTiles.Add(tile);
            BluelitTiles.Clear();
            BluelitColor = bluelitColor;
            
            _highlightChangedTiles.Clear();
        }

        void SetTileHighlightStatus(int iTile, TileHighlightColor highlighState)
        {
            switch (highlighState)
            {
                case TileHighlightColor.None:
                    if (PrevGreenlitTiles.Contains(iTile) ||
                        PrevYellowlitTiles.Contains(iTile) ||
                        PrevRedlitTiles.Contains(iTile) ||
                        PrevBluelitTiles.Contains(iTile))
                    {
                        _highlightChangedTiles.Add(iTile);
                    }

                    break;
                case TileHighlightColor.Green:
                    if (!PrevGreenlitTiles.Contains(iTile)) _highlightChangedTiles.Add(iTile);
                    GreenlitTiles.Add(iTile);
                    break;
                case TileHighlightColor.Blue:
                    if (!PrevBluelitTiles.Contains(iTile)) _highlightChangedTiles.Add(iTile);
                    BluelitTiles.Add(iTile);
                    
                    break;
                case TileHighlightColor.Red:
                    if (!PrevRedlitTiles.Contains(iTile)) _highlightChangedTiles.Add(iTile);
                    RedlitTiles.Add(iTile);
                    break;
                case TileHighlightColor.Yellow:
                    if (!PrevYellowlitTiles.Contains(iTile)) _highlightChangedTiles.Add(iTile);
                    YellowlitTiles.Add(iTile);
                    break;
            }
        }
        
        
        
        public void SetMapMode(MapMode mapMode)
        {
            if (mapMode == MapMode) return;
            MapMode = mapMode;
            RecalculateMapMode();
        }

        public void RecalculateMapMode()
        {
            MapTile selectedMapTile = SelectedObject as MapTile;
            switch (MapMode)
            {
                case MapMode.Political:
                    FlushTileHighlighting();
                    for (int i = 0; i < MapTilesByID.Length; i++) 
                        SetTileHighlightStatus(i, TileHighlightColor.None);
                    // Default map mode. Nothing else needed
                    break;
                case MapMode.Diplomacy:
                    FlushTileHighlighting(
                        greenlitColor:new Color(0.4f, 1f, 0.6f), 
                        yellowlitColor:new Color(1, 0.8f, 0.6f), 
                        redlitColor:new Color(0.9f, 0.2f, 0.2f),
                        bluelitColor:new Color(0f, 0.3f, 0.9f));
                    if (selectedMapTile is not null)
                    {
                        MapCountry selectedMapCountry = selectedMapTile.mapCountry;
                        MapFaction selectedMapFaction = selectedMapCountry is null ? null : 
                            (selectedMapCountry.colonialOverlord is null ? 
                                selectedMapCountry.Faction 
                                : 
                                selectedMapCountry.colonialOverlord.Faction);
                        IGameFaction selectedGameFaction = selectedMapFaction is null
                            ? null
                            : GameState.GetFaction(selectedMapFaction.ID);
                        if (selectedMapCountry is not null && selectedMapFaction is null)
                        {
                            foreach (var mapTile in MapTilesByID)
                            {
                                if (mapTile.mapCountry is not null)
                                {
                                    if (mapTile.mapCountry.Faction is not null && 
                                        GameState.GetFaction(mapTile.mapCountry.Faction.ID).IsAtWarWithCountry(selectedMapCountry.ID))
                                    {
                                        SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Red);
                                    }
                                    else if (mapTile.mapCountry == selectedMapCountry)
                                    {
                                        SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Green);
                                    }
                                    else if (mapTile.mapCountry.colonialOverlord == selectedMapCountry)
                                    {
                                        SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Blue);
                                    }
                                    else if (mapTile.mapCountry is not null)
                                    {
                                        SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Yellow);
                                    }
                                } 

                            }
                        }
                        else if (selectedGameFaction is not null)
                        {
                            foreach (var mapTile in MapTilesByID)
                            {
                                if (mapTile.mapCountry is not null)
                                {
                                    if (mapTile.mapCountry.colonialOverlord is not null &&
                                             mapTile.mapCountry.colonialOverlord.Faction == selectedMapFaction)
                                        SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Blue);
                                    else if (mapTile.mapCountry.Faction is not null)
                                    {

                                        if (mapTile.mapCountry.Faction == selectedMapFaction ||
                                                 (mapTile.mapCountry.colonialOverlord is not null &&
                                                  mapTile.mapCountry.colonialOverlord.Faction == selectedMapFaction))
                                            SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Green);
                                        else if (selectedGameFaction.IsAtWarWithFaction(mapTile.mapCountry.Faction.ID))
                                            SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Red);
                                        else SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Yellow);
                                    }
                                    else if (selectedGameFaction.IsAtWarWithCountry(mapTile.mapCountry.ID))
                                    {
                                        SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Red);
                                    }
                                    else
                                    {
                                        SetTileHighlightStatus(mapTile.ID, TileHighlightColor.Yellow);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case MapMode.Supply:
                    FlushTileHighlighting(
                        greenlitColor:new Color(0.4f, 1f, 0.6f), 
                        yellowlitColor:new Color(1, 0.8f, 0.6f), 
                        redlitColor:new Color(0.6f, 0.2f, 0.1f));
                    if (selectedMapTile is not null)
                    {
                        MapCountry selectedMapCountry = selectedMapTile.mapCountry;
                        MapFaction selectedMapFaction = selectedMapCountry is null ? null : selectedMapCountry.Faction;
                        if (selectedMapFaction is not null)
                        {
                            IGameFaction faction = GameState.GetFaction(selectedMapFaction.ID);
                            for (int i = 0; i < MapTilesByID.Length; i++)
                            {
                                switch (faction.TileSupplyStatus[i])
                                {
                                    case SupplyStatus.InSupply:
                                        SetTileHighlightStatus(i, TileHighlightColor.Green);
                                        break;
                                    case SupplyStatus.NotInSupply:
                                        SetTileHighlightStatus(i, TileHighlightColor.Red);
                                        break;
                                    default: throw new NotImplementedException();
                                }
                            }
                        }
                    }
                    break;
                case MapMode.Trade:
                    FlushTileHighlighting(
                        greenlitColor:new Color(0.4f, 1f, 0.6f), 
                        yellowlitColor:new Color(1, 0.8f, 0.6f), 
                        redlitColor:new Color(0.6f, 0.2f, 0.1f),
                        bluelitColor:new Color(1f, 0.6f, 0.2f));
                    if (selectedMapTile is not null)
                    {
                        MapCountry selectedMapCountry = selectedMapTile.mapCountry;
                        MapFaction selectedMapFaction = selectedMapCountry is null ? null : selectedMapCountry.Faction;
                        if (selectedMapFaction is not null)
                        {
                            IGameFaction faction = GameState.GetFaction(selectedMapFaction.ID);
                            for (int i = 0; i < MapTilesByID.Length; i++)
                            {
                                if (faction.PredictedTileTradeStatus[i] == TradeStatus.FullyAccessible)
                                    SetTileHighlightStatus(i, TileHighlightColor.Green);
                                else if (faction.PredictedTileTradeStatus[i] == TradeStatus.Blockaded_ColonialAccessible)
                                    SetTileHighlightStatus(i, TileHighlightColor.Yellow);
                                else if (faction.PredictedTileTradeStatus[i] == TradeStatus.FullyBlockaded)
                                    SetTileHighlightStatus(i, TileHighlightColor.Red);
                                else SetTileHighlightStatus(i, TileHighlightColor.Red);
                            }
                        }
                    }
                    break;
                default: throw new NotImplementedException();
            }
            
            foreach (var mapTile in MapTilesByID)
            {
                if (_highlightChangedTiles.Contains(mapTile.ID)) mapTile.RecalculateMaterialDuringRuntime();
            }
        }


        private static int guidCounter = 0;
        private int guid;
        private void Start()
        {
            guidCounter++;
            guid = guidCounter;
            Debug.Log($"Creating map #{guid}");
            CadreBlockMesh = Resources.Load<Mesh>("Meshes/CadreBlock");
            MapCadresByID = new MapCadre[MaxCadres];
            // MapFactionsByID = new MapFaction[] // Set in TTGameState when building the map
            //RecalculateMapObjectLists();
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            MapCadresByID = new MapCadre[MaxCadres];
        }
#endif


        
        
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
                cadre.DestroyMapObject();
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
                    mapCountry.associatedFaction = faction;
                    indexedMapCountries.Add(mapCountry);
                }
                else if (mapObject is MapFaction mapFaction)
                {
                    Debug.LogError($"Map Faction in Map File ({mapFaction.name}). Factions should be created through the scenario file, not on the map directly");
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
            //Debugging
            if (Input.GetKeyDown(KeyCode.E))
            {
                string str = "";
                for (int i = 0; i < MapCadresByID.Length; i++)
                {
                    str += $"#{i} - {MapCadresByID[i]}\n";
                }
                Debug.Log(str);
            }
            
            foreach (var mapObject in new List<MapObject>(ObjectsNeedingAnimation))
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
                foreach (var mapCountry in MapCountriesByID)
                {
                    mapCountry.RecalculateOverlayPositioning();
                }
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
                    //border.CalculateUVDirection();
                    if (border.connectedMapTiles.Count != 2)
                    {
                        border.borderType = BorderType.Impassable;
                        continue;
                    }

                    MapTile firstTile = border.connectedMapTiles[0];
                    MapTile secondTile = border.connectedMapTiles[1];
                    // Is it impassible?
                    if (firstTile.terrainType == TerrainType.NotInPlay ||
                        secondTile.terrainType == TerrainType.NotInPlay)
                    {
                        border.borderType = BorderType.Impassable;
                        continue;
                    }
                    
                    // Is it a strait?
                    if (firstTile.terrainType == TerrainType.Strait ||
                        secondTile.terrainType == TerrainType.Strait)
                    {
                        border.borderType = BorderType.Strait;
                        continue;
                    }
                    
                    // Is it water-to-water?
                    if (
                        (firstTile.terrainType == TerrainType.Sea || firstTile.terrainType == TerrainType.Ocean) &&
                        (secondTile.terrainType == TerrainType.Sea || secondTile.terrainType == TerrainType.Ocean))
                    {
                        border.borderType = BorderType.Sea;
                        continue;
                    }
                    
                    // Is it a coast?
                    if (
                            (
                                (firstTile.terrainType == TerrainType.Sea || firstTile.terrainType == TerrainType.Ocean) 
                                &&
                                (secondTile.terrainType != TerrainType.Sea && secondTile.terrainType != TerrainType.Ocean)
                            )
                            ||
                            (
                                (firstTile.terrainType != TerrainType.Sea && firstTile.terrainType != TerrainType.Ocean) 
                                &&
                                (secondTile.terrainType == TerrainType.Sea || secondTile.terrainType == TerrainType.Ocean)
                                
                            )
                        )
                    {
                        border.borderType = BorderType.Coast;
                        firstTile.IsCoastal = (firstTile.terrainType == TerrainType.Land ||
                                               firstTile.terrainType == TerrainType.Strait);
                        secondTile.IsCoastal = (secondTile.terrainType == TerrainType.Land ||
                                                secondTile.terrainType == TerrainType.Strait);
                        continue;
                    }
                    
                    // It must be land. Leave it be
                    continue;
                }
            }

            if (_fullRecalculationCalled)
            {
                foreach (var mapCountry in MapCountriesByID)
                {
                    if (mapCountry.InternalName == null || mapCountry.InternalName == "")
                    {
                        mapCountry.InternalName = mapCountry.name;
                    }
                    else mapCountry.name = mapCountry.InternalName;
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

            if (mapObject is MapCadre cadre)
            {
                if (cadre.ID == -1) Debug.LogError("cadre ID should be set BEFORE registering");
                if (MapCadresByID[cadre.ID] is not null) Debug.LogError("Two map cadres are trying to share the same ID");
                MapCadresByID[cadre.ID] = cadre;
            }

            if (mapObject is MapFaction faction)
            {
                if (faction.ID == -1) Debug.LogError("faction ID should be set BEFORE registering");
                if (MapFactionsByID[faction.ID] is not null) Debug.LogError("Two map factions are trying to share the same ID");
                MapFactionsByID[faction.ID] = faction;
            }
            
            if (mapObject is MapCountry country)
            {
                if (MapCountriesByID[country.ID] is not null) Debug.LogError("Two map countries are trying to share the same ID");
                MapCountriesByID[country.ID] = country;
            }

            if (mapObject is IMapToken mapToken)
            {
                MapTokens.Add(mapToken);
            }
        }

        public void DeregisterObject(MapObject mapObject) // Should only ever be called through MapObject.Deregister
        {
            Debug.Log($"Deregistering {mapObject.GetType().Name} #{mapObject.ID}");
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
            
            if (mapObject is MapCountry country)
            {
                MapCountriesByID[country.ID] = null;
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
                mapCountry.RecalculateColor(alsoRecalculateBorders:false);
                mapCountry.RecalculateFlag();
            }

            foreach (var mapTile in MapTilesByID)
            {
                //mapTile.RecalculateMaterialDuringRuntime(); // Handled by GameTile.RefreshMapState
            }

            foreach (var mapBorder in MapBordersByID)
            {
                mapBorder.RecalculateMaterialRuntimeValues();
            }

            foreach (var mapCadre in MapCadresByID)
            {
                if (mapCadre is not null) mapCadre.RecalculateAppearance();
            }
        }

        public MapTile GetTileByName(string name)
        {
            int iMapTile = -1;
            for (int k = 0; k < MapTilesByID.Length; k++)
            {
                if (MapTilesByID[k].name == name)
                {
                    iMapTile = k;
                    break;
                }
            }

            if (iMapTile != -1) return MapTilesByID[iMapTile];
            else return null;
        }
        
        public MapCountry GetCountryByName(string name)
        {
            int iMapCountry = -1;
            for (int k = 0; k < MapCountriesByID.Length; k++)
            {
                if (MapCountriesByID[k].name == name)
                {
                    iMapCountry = k;
                    break;
                }
            }

            if (iMapCountry != -1) return MapCountriesByID[iMapCountry];
            else return null;
        }

        public void Dispose()
        {
            foreach (var mapObject in new List<MapObject>(MapObjects))
            {
                if (mapObject is not null) mapObject.DestroyMapObject();
                else Debug.LogWarning($"Null map object in map objects list");
            }
            Destroy(this.gameObject);
            Destroy(this);
        }
    }
}
