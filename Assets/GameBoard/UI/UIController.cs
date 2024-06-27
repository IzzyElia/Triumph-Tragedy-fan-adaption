using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard.UI.SpecializeComponents;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameBoard.UI
{
    public enum InputType
    {
        Touch,
        Mouse
    }

    public enum UIFlag
    {
        InitialPlacement,
        Production,
        DiplomacyCardplay,
        CommandCardplay,
        Movement,
        Combat,
    }
    
    /// <summary>
    /// There are two mechanisms for hooking up UI objects. Through 
    /// </summary>
    public partial class UIController : MonoBehaviour, IUIController, IDisposable
    {
        public List<MapTile> debugMapTilesUnderPointer = new List<MapTile>();
        
        public static List<UIController> UIControllers = new List<UIController>();
        public static UIController Create(Map mapRenderer)
        {
            GameObject uiControllerObject = new GameObject("UI Controller", typeof(UIController));
            UIController uiController = uiControllerObject.GetComponent<UIController>();
            UIControllers.Add(uiController);
            uiController.MapRenderer = mapRenderer;
            uiController.mainCamera = Camera.main;
            uiController.inputType = InputType.Mouse;
            
            // Setup actions
            uiController.MovementAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("MoveUnits");
            uiController.InitialPlacementAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("BuildInitialUnits");
            uiController.ProductionAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("ProductionAction");
            uiController.CardplayAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("CardplayAction");
            uiController.CombatAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("CombatDecision");
            uiController.CombatSelectionAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("CombatSelection");

            return uiController;
        }
        
        // Resources
        public GameObject unitGhostPrefab;
        public Material uiIconMaterial;
            
            
            
        [NonSerialized] public UIWindow UnitPlacementWindow;
        [NonSerialized] public UICardHand CardHand;
        [NonSerialized] public UIProductionInfo ProductionInfoWindow;
        [NonSerialized] public UICommandsInfo CommandsInfoWindow;
        [NonSerialized] public UIPlayerStartTurnPanel PlayerStartTurnScreen;
        [NonSerialized] public DebugTextbox DebugTextbox;
        [NonSerialized] public UICardPlayArea CardPlayArea;
        [NonSerialized] public Map MapRenderer;
        public InputType inputType;
        public Camera mainCamera;
        private UIUnitMover _unitMover = null;
        public List<MapCadrePlacementGhost> InitialPlacementGhosts = new ();
        public List<MapCadrePlacementGhost> ProductionPlacementGhosts = new ();
        private List<UIComponent> _uiComponents = new();
        private Dictionary<string, UIComponent> _uiComponentsByName = new Dictionary<string, UIComponent>();
        private List<UIWindow> _uiWindows = new List<UIWindow>();
        
        // Externally set public values
        
        
        // Internally set public values --------------------
        [NonSerialized] public readonly IList<MapObject> MapObjectsUnderPointer = new List<MapObject>();
        public GameObject UIObjectAtPointer { get; private set; }
        public GameObject UICardUnderPointer { get; private set; }
        public GameObject UICardRegionUnderPointer { get; private set; }
        public GameObject UICardPlayPanelUnderPointer { get; private set; }
        public GameObject UICardEffectUnderPointer { get; private set; }
        public bool PointerIsOverUI { get; private set; }
        public Vector3 PointerPositionInWorld { get; private set; }
        public Vector3 PointerPositionOnScreen { get; private set; }
        public InputStatus PointerInputStatus { get; private set; } = InputStatus.Inactive;
        public InputStatus ModifierInputStatus { get; private set; } = InputStatus.Inactive;
        public float TimeSincePointerPressed { get; private set; }
        public Vector3 PointerPressedAtScreenPosition { get; private set; }
        public float DeltaDragSincePointerPressed { get; private set; }
        public const float DragVsClickThreshold = 25;
        private bool MultipleMapObjectSelectionsAllowed => GameState.GamePhase == GamePhase.SelectSupport || GameState.GamePhase == GamePhase.GiveCommands;
        public List<MapObject> SelectedMapObjects => MapRenderer.SelectedObjects;
        public List<MapObject> ScrubQueue { get; private set; } = new List<MapObject>();
        public bool SelectionChanged = false;
        public MapObject HoveredMapObject
        {
            get => MapRenderer.HoveredMapObject;
            set => MapRenderer.HoveredMapObject = value;
        }

        public MapObject PrevHoveredMapObject;
        public bool HoveredMapObjectChanged { get; private set; }
        public MapTile HoveredOverTile { get; private set; }

        public MapTile PrevHoveredOverTile;
        public bool HoveredOverTileChanged { get; private set; }
        public IPlayerAction MovementAction { get; private set; }
        public IPlayerAction InitialPlacementAction { get; private set; }
        public IPlayerAction ProductionAction { get; private set; }
        public IPlayerAction CardplayAction { get; private set; }
        public IPlayerAction CombatAction { get; private set; }
        public IPlayerAction CombatSelectionAction { get; private set; }
        // These values are compared against the game state's current values to see if a refresh is needed
        public GamePhase knownGamephase = GamePhase.None;
        public Season knownSeason = Season.None;
        public bool knownIsMyTurn;
        public bool IsMyTurn => GameState.IsWaitingOnPlayer(iPlayer);
        public ITTGameState GameState => MapRenderer.GameState;
        public int iPlayer => GameState.iPlayer;
        public MapFaction PlayerMapFaction => iPlayer < MapRenderer.MapFactionsByID.Length ? MapRenderer.MapFactionsByID[iPlayer] : null;
        public GameObject RootUIObject;
        public bool UnresolvedStateChange; // Should be triggered when the game state or the players queued action state changes
        public bool UnresolvedResync;
        public bool ActiveLocally;
        public Canvas Canvas;
        public bool Initialized = false;

        private void Start()
        {
            //Load Resources
            unitGhostPrefab = Resources.Load<GameObject>("Prefabs/CadreGhost");
            uiIconMaterial = Resources.Load<Material>("Shaders/UI/UIIcon");
            
            // Connect objects
            if (mainCamera is null) throw new InvalidOperationException("UIController not hooked up to a camera");
            GameObject canvasObject = GameObject.Find("Main Canvas");
            if (canvasObject is null) throw new InvalidOperationException("Could not find the main canvas. Make sure there is a canvas in the scene and its name is 'Main Canvas'");
            this.Canvas = canvasObject.GetComponent<Canvas>();
            
            GameObject playerUIPrefab = Resources.Load<GameObject>("PlayerUI");
            if (playerUIPrefab is null) throw new InvalidOperationException($"Could not find the PlayerUI prefab");
            
            // Create UI and register UI Components found in the prefab
            RootUIObject = Instantiate(playerUIPrefab, canvasObject.transform);
            foreach (var uiComponent in RootUIObject.GetComponentsInChildren<UIComponent>())
            {
                RegisterUIComponent(uiComponent);
                if (uiComponent is UIWindow window)
                {
                    window.SetActive(window.WantsToBeActive, force:true);
                }
            }

            UnitPlacementWindow = GetUIComponent<UIWindow>("Unit Placement Window");
            DebugTextbox = GetUIComponent<DebugTextbox>("Debug Textbox");
            CardHand = GetUIComponent<UICardHand>("Card Hand");
            CardPlayArea = GetUIComponent<UICardPlayArea>();
            ProductionInfoWindow = GetUIComponent<UIProductionInfo>("Production Info Window");
            CommandsInfoWindow = GetUIComponent<UICommandsInfo>("Commands Info Window");
            PlayerStartTurnScreen = GetUIComponent<UIPlayerStartTurnPanel>("Start Turn Screen");

            Initialized = true;
        }

        public void RecalculateUIMode()
        {
            if (GameState.GamePhase == GamePhase.InitialPlacement)
            {
                RefreshInitialPlacement();
            }
        }

        void CleanUIStragglers()
        {
            if (GameState.GamePhase != GamePhase.InitialPlacement)
            {
                DestroyInitialPlacementGhosts();
            }

            if (GameState.GamePhase != GamePhase.Production)
            {
                DestroyProductionPlacementGhosts();
            }
        }

        void Scrub()
        {
            foreach (var destroyedObject in ScrubQueue)
            {
                SelectedMapObjects.Remove(destroyedObject);
                if (PrevHoveredOverTile == destroyedObject) PrevHoveredOverTile = null;
                if (PrevHoveredMapObject == destroyedObject) PrevHoveredOverTile = null;
                if (HoveredMapObject == destroyedObject) HoveredMapObject = null;
                if (HoveredOverTile == destroyedObject) HoveredOverTile = null;
            }
            ScrubQueue.Clear();
        }

        private const int _stateCheckTime = 300;
        private int _stateCheckTimer = _stateCheckTime;
        private void Update()
        {
            if (!ActiveLocally) return;

            Scrub();
            
            _stateCheckTimer -= 1;
            if (_stateCheckTimer <= 0)
            {
                _stateCheckTimer = _stateCheckTime;
                CleanUIStragglers();
            }
            
            RecalculateUserInputValues();

            // Resolve resync logic before gamestate logic, as it may be order dependant
            // (ie OnResyncEnded usually reconstructs the skeleton of the object
            // while OnGamestateChanged applies values to that skeleton)
            if (UnresolvedResync && RootUIObject.activeSelf)
            {
                foreach (var uiComponent in new List<UIComponent>(_uiComponents))
                {
                    uiComponent.OnResyncEnded();
                }

                UnresolvedResync = false;
            }
            if (UnresolvedStateChange && GameState.IsSynced && RootUIObject.activeSelf)
            {
                foreach (var uiComponent in new List<UIComponent>(_uiComponents))
                {
                    uiComponent.OnGamestateChanged();
                    if (uiComponent is UIWindow window)
                    {
                        if (window.Active && !window.WantsToBeActive) window.SetActive(false);
                        else if (!window.Active && window.WantsToBeActive) window.SetActive(true);
                    }
                }
                RecalculateUIMode();
                UnresolvedStateChange = false;
            }

            UpdateHoveringAndSelectionChanges();
            
            // Call all other UIComponent updates
            foreach (var uiComponent in new List<UIComponent>(_uiComponents))
            {
                uiComponent.UIUpdate();
            }
            
            MovementUpdate();
        }

        void RecalculateUserInputValues()
        {
            // Get the pointer position and retrieve what's under the cursor
            Vector2 pointerPosition;
            TimeSincePointerPressed += Time.deltaTime;
            switch (inputType)
            {
                case InputType.Touch:
                    Touch touch = Input.GetTouch(0);
                    pointerPosition = touch.position;
                    throw new NotImplementedException("Still need to check whether the touch is a new tap or a drag");
                    break;
                case InputType.Mouse:
                    pointerPosition = (Vector2)Input.mousePosition;
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        PointerInputStatus = InputStatus.Pressed;
                        TimeSincePointerPressed = 0;
                        PointerPressedAtScreenPosition = pointerPosition;
                    }
                    else if (Input.GetKeyUp(KeyCode.Mouse0))
                    {
                        PointerInputStatus = InputStatus.Releasing;
                    }
                    else if (Input.GetKey(KeyCode.Mouse0))
                    {
                        PointerInputStatus = InputStatus.Held;
                        DeltaDragSincePointerPressed = Vector2.Distance(pointerPosition, PointerPressedAtScreenPosition);
                    }
                    else PointerInputStatus = InputStatus.Inactive;

                    if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                        Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        if (ModifierInputStatus == InputStatus.Inactive)
                            ModifierInputStatus = InputStatus.Pressed;
                        else ModifierInputStatus = InputStatus.Held;
                    }
                    else
                    {
                        if (ModifierInputStatus == InputStatus.Held)
                            ModifierInputStatus = InputStatus.Releasing;
                        else ModifierInputStatus = InputStatus.Inactive;
                    }
                    break;
                default:
                    throw new NotImplementedException($"Input type {inputType.ToString()} still needs to be implemented");
                    break;
            }
            PointerPositionOnScreen = pointerPosition;
            PointerPositionInWorld = ScreenToZPlane(mainCamera, pointerPosition, 0);
        }
        
        void UpdateHoveringAndSelectionChanges()
        {
            GameObject cadreAtCursor;
            GameObject tileAtCursor;
            GameObject[] uiObjectsAtCursor;
            GameObject topWorldObjectAtCursor;
            GameObject topUIObjectAtCursor;
            (uiObjectsAtCursor, cadreAtCursor, tileAtCursor) =
                GetObjectUnderScreenPoint(PointerPositionOnScreen, mainCamera);
            
            // Set the PointerIsOverUI property
            PointerIsOverUI = uiObjectsAtCursor.Length > 0;
            UICardUnderPointer = null;
            UICardRegionUnderPointer = null;
            UICardPlayPanelUnderPointer = null;
            UICardEffectUnderPointer = null;
            HoveredMapObject = null;
            HoveredOverTile = null;
            SelectionChanged = false;
            
            if (PointerIsOverUI)
            {
                UIObjectAtPointer = uiObjectsAtCursor[0];
                foreach (var uiObject in uiObjectsAtCursor)
                {
                    if (uiObject.CompareTag("UICard") && UICardUnderPointer is null)
                    {
                        UICardUnderPointer = uiObject;
                    }
                    if (uiObject.CompareTag("UICardRegion") && UICardRegionUnderPointer is null)
                    {
                        UICardRegionUnderPointer = uiObject;
                    }

                    if (uiObject.CompareTag("UICardPlayPanel") && UICardPlayPanelUnderPointer is null)
                    {
                        UICardPlayPanelUnderPointer = uiObject;
                    }
                    
                    if (uiObject.CompareTag("UICardEffect") && UICardEffectUnderPointer is null)
                    {
                        UICardEffectUnderPointer = uiObject;
                    }
                }
            }
            else
            {
                if (tileAtCursor is null) HoveredOverTile = null;
                else HoveredOverTile = tileAtCursor.GetComponent<MapTile>();

                if (cadreAtCursor is null) HoveredMapObject = null;
                else HoveredMapObject = cadreAtCursor.GetComponent<MapObject>();
            }
            
            if (PointerInputStatus == InputStatus.Pressed && !PointerIsOverUI)
            {
                if (!MultipleMapObjectSelectionsAllowed)
                {
                    foreach (var mapObject in SelectedMapObjects) mapObject.OnSelectionStatusChanged(SelectionStatus.Unselected);
                    SelectedMapObjects.Clear();
                    SelectionChanged = true;
                }

                if (HoveredMapObject is not null)
                {
                    if (SelectedMapObjects.Contains(HoveredMapObject))
                    {
                        SelectedMapObjects.Remove(HoveredMapObject);
                        HoveredMapObject.OnSelectionStatusChanged(SelectionStatus.Unselected);
                        SelectionChanged = true;
                    }
                    else
                    {
                        if (HoveredMapObject.IsSelectable)
                        {
                            SelectedMapObjects.Add(HoveredMapObject);
                            HoveredMapObject.OnSelectionStatusChanged(SelectionStatus.Selected);
                            SelectionChanged = true;
                        }
                    }
                }
            }

            HoveredMapObjectChanged = false;
            if (HoveredMapObject != PrevHoveredMapObject)
            {
                HoveredMapObjectChanged = true;
                if (PrevHoveredMapObject is not null && !PrevHoveredMapObject.IsDestroyed)
                {
                    PrevHoveredMapObject.OnHoveredStatusChanged(false);
                }

                if (HoveredMapObject is not null && !HoveredMapObject.IsDestroyed)
                {
                    HoveredMapObject.OnHoveredStatusChanged(true);
                }

                PrevHoveredMapObject = HoveredMapObject;
            }

            HoveredOverTileChanged = false;
            if (HoveredOverTile != PrevHoveredOverTile)
            {
                HoveredOverTileChanged = true;
                if (PrevHoveredOverTile is not null)
                {
                    PrevHoveredOverTile.OnHoveredStatusChanged(false);
                }
                if (HoveredOverTile is not null)
                {
                    HoveredOverTile.OnHoveredStatusChanged(true);
                }

                PrevHoveredOverTile = HoveredOverTile;
            }
        }


        private int _knownStartingUnitHash = 0;
        void RefreshInitialPlacement()
        {
            if (PlayerMapFaction is null) return;
            int hash = 17;
            foreach (var startingUnitInfo in PlayerMapFaction.startingUnits)
            {
                hash ^= startingUnitInfo.GetHashCode();
            }

            if (hash == _knownStartingUnitHash)
            {
                return;
            }
            _knownStartingUnitHash = hash;
                
            DestroyInitialPlacementGhosts();
            foreach (var startingUnitInfo in GameState.GetStartingUnits(iPlayer))
            {
                for (int i = 0; i < startingUnitInfo.startingCadres; i++)
                {
                    MapTile mapTile = MapRenderer.MapTilesByID[startingUnitInfo.iTile];
                    MapCountry mapCountry = MapRenderer.MapCountriesByID[startingUnitInfo.iCountry];
                    MapCadrePlacementGhost placementGhost = MapCadrePlacementGhost.Create("Initial Placement Ghost", MapRenderer, mapTile, mapCountry, GameState.Ruleset.GetNamedUnitType("Infantry"), UnitGhostPurpose.InitialPlacement);
                    InitialPlacementGhosts.Add(placementGhost);
                }
            }
            
        }

        public void SetActive(bool isActive)
        {
            if (isActive && !ActiveLocally)
            {
                PlayerStartTurnScreen.Setup(PlayerMapFaction);
            }
            
            ActiveLocally = isActive;
            RootUIObject.SetActive(isActive);
            MapRenderer.gameObject.SetActive(isActive);
        }
        
        public void DestroyInitialPlacementGhosts()
        {
            foreach (var placementGhost in InitialPlacementGhosts)
            {
                placementGhost.DestroyMapObject();
            }
            InitialPlacementGhosts.Clear();
        }

        public void DestroyProductionPlacementGhosts()
        {
            foreach (var placementGhost in ProductionPlacementGhosts)
            {
                placementGhost.DestroyMapObject();
            }
            ProductionPlacementGhosts.Clear();
        }
        
        
        
        
        
        
        
        
        
        
        // Utility Functions
        private T InstantiateUIObject<T>(GameObject prefab) where T : UIComponent
        {
            T component = Instantiate(prefab).GetComponent<T>();
            RegisterUIComponent(component);
            return component;
        }

        public void RegisterUIComponent(UIComponent component)
        {
            if (component.globalName != null && component.globalName != String.Empty)
                if (!_uiComponentsByName.TryAdd(component.globalName, component)) Debug.LogError($"Duplicate use of component global name {component.globalName}");
            component.UIController = this;
            _uiComponents.Add(component);
            if (component is UIWindow window) _uiWindows.Add(window);
            component.OnRegistered();
        }
        public void DeregisterUIObject(UIComponent uiComponent)
        {
            _uiComponentsByName.Remove(uiComponent.globalName);
            _uiComponents.Remove(uiComponent);
        }
        
        
        public void RegisterAnimatedEvent(AnimatedEvent animatedEvent, bool simultaneous)
        {
            throw new NotImplementedException();
        }

        public T GetUIComponent<T>(string name = null) where T : UIComponent
        {
            if (name == null)
            {
                return RootUIObject.GetComponentInChildren<T>();
            }
            else
            {
                if (_uiComponentsByName.TryGetValue(name, out UIComponent uiComponent))
                {
                    return (T)uiComponent;
                }
                Debug.LogError($"UI Component {name} of type {typeof(T).Name} not found");
                return null;
            }
        }
        public bool TryGetUIComponent<T>(string name, out T component) where T : UIComponent
        {
            if (_uiComponentsByName.TryGetValue(name, out UIComponent uiComponent))
            {
                component = (T)uiComponent;
                return true;
            }

            component = null;
            return false;
        }
        /// <summary>
        /// Get the object under a screen point, which might be a mouse position or a touch position
        /// </summary>
        /// <param name="point">The screen point</param>
        /// <param name="camera">Main camera if null</param>
        /// <returns></returns>
        public static (GameObject[] uiObjects, GameObject cadre, GameObject tile) GetObjectUnderScreenPoint(Vector2 point, Camera camera)
        {
            // Check UI objects
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(point.x, point.y);
    
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            GameObject[] uiObjects = new GameObject[results.Count];
            for (int i = 0; i < results.Count; i++)
            {
                    // Return the first UI object hit by the raycast
                    uiObjects[i] = results[i].gameObject;
            }
            
            // If no UI object, check for objects in the world
            Ray ray = camera.ScreenPointToRay(point);
            RaycastHit hit;
            GameObject cadre = null;
            GameObject tile = null;
            if (Physics.Raycast(ray, out hit, 10000, LayerMask.GetMask("Cadres")))
            {
                cadre = hit.transform.gameObject;
            }

            if (Physics.Raycast(ray, out hit, 10000, LayerMask.GetMask("Tiles")))
            {
                tile = hit.transform.gameObject;
            }
            //hits = hits.OrderBy(hit => hit.distance).ToArray();
            

            return (uiObjects, cadre, tile);
        }
        public static Vector3 ScreenToZPlane(Camera camera, Vector3 screenPosition, float flatWorldZ)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);
            Plane xy = new Plane(Vector3.forward, new Vector3(0, 0, flatWorldZ));
            float distance;
            xy.Raycast(ray, out distance);
            return ray.GetPoint(distance);
        }

        public void Dispose()
        {
            Destroy(RootUIObject);
            Destroy(this.gameObject);
        }
    }
}