using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard.UI.SpecializeComponents;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms.GameCenter;
using Object = UnityEngine.Object;

namespace GameBoard.UI
{
    public enum InputType
    {
        Touch,
        Mouse
    }
    public enum PointerInputStatus
    {
        Inactive,
        Pressed,
        Held,
        Releasing
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
    public class UIController : MonoBehaviour, IDisposable
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
            uiController.ProductionAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("Production");
            uiController.GovernmentAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("GovernmentAction");
            uiController.CombatAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("CombatDecision");
            uiController.CombatSelectionAction = mapRenderer.GameState.GenerateClientsidePlayerActionByName("CombatSelection");

            return uiController;
        }
        
        // Resources
        public GameObject unitGhostPrefab;
        public Material uiIconMaterial;
            
            
            
        private UIWindow _unitPlacementWindow;
        private UICardHand _cardHand;
        private DebugTextbox _debugTextbox;
        [NonSerialized] public Map MapRenderer;
        public InputType inputType;
        public Camera mainCamera;
        public bool PointerIsOverUI { get; private set; }
        private UIUnitMover _unitMover = null;
        public List<CadrePlacementGhost> InitialPlacementGhosts = new ();
        public List<CadrePlacementGhost> ProductionPlacementGhosts = new ();
        private List<UIComponent> _uiComponents = new();
        private Dictionary<string, UIComponent> _uiComponentsByName = new Dictionary<string, UIComponent>();
        private List<UIWindow> _uiWindows = new List<UIWindow>();
        /// <summary>
        /// MapObject()'s_, Not GameObject's
        /// </summary>
        [NonSerialized] public readonly IList<MapObject> MapObjectsUnderPointer = new List<MapObject>();
        [NonSerialized] public GameObject UIObjectAtPointer;
        [NonSerialized] public GameObject UICardUnderPointer;
        [NonSerialized] public GameObject UICardRegionUnderPointer;
        public Vector3 PointerPositionInWorld { get; private set; }
        public Vector3 PointerPositionOnScreen { get; private set; }
        public PointerInputStatus PointerInputStatus { get; private set; } = PointerInputStatus.Inactive;

        public MapObject SelectedMapObject
        {
            get => MapRenderer.SelectedObject;
            set => MapRenderer.SelectedObject = value;
        }

        public MapObject PrevSelectedMapObject;

        public MapObject HoveredMapObject
        {
            get => MapRenderer.HoveredMapObject;
            set => MapRenderer.HoveredMapObject = value;
        }

        public MapObject PrevHoveredMapObject;

        public MapTile HoveredOverTile
        {
            get => MapRenderer.HoveredOverTile;
            set => MapRenderer.HoveredOverTile = value;
        }

        public MapTile PrevHoveredOverTile;
        public IPlayerAction MovementAction { get; private set; }
        public IPlayerAction InitialPlacementAction { get; private set; }
        public IPlayerAction ProductionAction { get; private set; }
        public IPlayerAction GovernmentAction { get; private set; }

        public IPlayerAction CombatAction { get; private set; }
        public IPlayerAction CombatSelectionAction { get; private set; }
        // These values are compared against the game state's current values to see if a refresh is needed
        public GamePhase knownGamephase = GamePhase.None;
        public Season knownSeason = Season.None;
        public bool knownIsMyTurn;
        private bool IsMyTurn => GameState.IsWaitingOnPlayer(iPlayer);
        public ITTGameState GameState => MapRenderer.GameState;
        public int iPlayer => GameState.iPlayer;
        public MapFaction PlayerMapFaction => iPlayer < MapRenderer.MapFactionsByID.Length ? MapRenderer.MapFactionsByID[iPlayer] : null;
        public GameObject RootUIObject;
        public bool UnresolvedStateChange;
        public bool UnresolvedResync;
        public bool ActiveLocally;

        private void Start()
        {
            //Load Resources
            unitGhostPrefab = Resources.Load<GameObject>("Prefabs/CadreGhost");
            uiIconMaterial = Resources.Load<Material>("Shaders/UI/UIIcon");
            
            // Connect objects
            if (mainCamera is null) throw new InvalidOperationException("UIController not hooked up to a camera");
            GameObject canvasObject = GameObject.Find("Main Canvas");
            if (canvasObject is null) throw new InvalidOperationException("Could not find the main canvas. Make sure there is a canvas in the scene and its name is 'Main Canvas'");

            GameObject playerUIPrefab = Resources.Load<GameObject>("PlayerUI");
            if (playerUIPrefab is null) throw new InvalidOperationException($"Could not find the PlayerUI prefab");
            
            // Create UI and register UI Components found in the prefab
            RootUIObject = Instantiate(playerUIPrefab, canvasObject.transform);
            foreach (var uiComponent in RootUIObject.GetComponentsInChildren<UIComponent>())
            {
                RegisterUIComponent(uiComponent);
            }

            _unitPlacementWindow = GetUIComponent<UIWindow>("Unit Placement Window");
            _debugTextbox = GetUIComponent<DebugTextbox>("Debug Textbox");
            _cardHand = GetUIComponent<UICardHand>("Card Hand");
        }

        public void RecalculateUIMode()
        {

            if (GameState.GamePhase == GamePhase.InitialPlacement)
            {
                _unitPlacementWindow.SetActive(true);
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

        private const int _stateCheckTime = 300;
        private int _stateCheckTimer = _stateCheckTime;
        private void Update()
        {
            _stateCheckTimer -= 1;
            if (_stateCheckTimer <= 0)
            {
                _stateCheckTimer = _stateCheckTime;
                CleanUIStragglers();
            }
            
            if (ActiveLocally != RootUIObject.activeSelf)
            {
                RootUIObject.SetActive(ActiveLocally);
                MapRenderer.gameObject.SetActive(ActiveLocally);
            }
            if (!ActiveLocally) return;
            
            RecalculateUserInputValues();

            // Resolve resync logic before gamestate logic, as it may be order dependant
            // (ie OnResyncEnded usually reconstructs the skeleton of the object
            // while OnGamestateChanged applies values to that skeleton)
            if (UnresolvedResync)
            {
                foreach (var uiComponent in _uiComponents)
                {
                    uiComponent.OnResyncEnded();
                }

                UnresolvedResync = false;
            }
            if (UnresolvedStateChange && GameState.IsSynced)
            {
                foreach (var uiComponent in _uiComponents)
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
            foreach (var uiComponent in _uiComponents)
            {
                uiComponent.UIUpdate();
            }
        }

        void RecalculateUserInputValues()
        {
                        // Get the pointer position and retrieve what's under the cursor
            Vector2 pointerPosition;
            
            switch (inputType)
            {
                case InputType.Touch:
                    Touch touch = Input.GetTouch(0);
                    pointerPosition = touch.position;
                    throw new NotImplementedException("Still need to check whether the touch is a new tap or a drag");
                    break;
                case InputType.Mouse:
                    pointerPosition = (Vector2)Input.mousePosition;
                    if (Input.GetKeyDown(KeyCode.Mouse0)) PointerInputStatus = PointerInputStatus.Pressed;
                    else if (Input.GetKeyUp(KeyCode.Mouse0)) PointerInputStatus = PointerInputStatus.Releasing;
                    else if (Input.GetKey(KeyCode.Mouse0)) PointerInputStatus = PointerInputStatus.Held;
                    else PointerInputStatus = PointerInputStatus.Inactive;
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
            HoveredMapObject = null;
            HoveredOverTile = null;

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
                }
            }
            else
            {
                if (tileAtCursor is null) HoveredOverTile = null;
                else HoveredOverTile = tileAtCursor.GetComponent<MapTile>();

                if (cadreAtCursor is null) HoveredMapObject = null;
                else HoveredMapObject = cadreAtCursor.GetComponent<MapObject>();
            }
            
            if (PointerInputStatus == PointerInputStatus.Pressed && !PointerIsOverUI)
            {
                SelectedMapObject = HoveredMapObject;
            }
            
            
            if (HoveredMapObject != PrevHoveredMapObject)
            {
                if (PrevHoveredMapObject is not null)
                {
                    PrevHoveredMapObject.OnHoveredStatusChanged(false);
                }

                if (HoveredMapObject is not null)
                {
                    HoveredMapObject.OnHoveredStatusChanged(true);
                }

                PrevHoveredMapObject = HoveredMapObject;
            }
            if (SelectedMapObject != PrevSelectedMapObject)
            {
                if (PrevSelectedMapObject is not null)
                {
                    PrevSelectedMapObject.OnSelectionStatusChanged(SelectionStatus.Unselected);
                }
                if (SelectedMapObject is not null)
                {
                    SelectedMapObject.OnSelectionStatusChanged(SelectionStatus.Selected);
                }

                PrevSelectedMapObject = SelectedMapObject;
            }
            if (HoveredOverTile != PrevHoveredOverTile)
            {
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
                Debug.LogWarning("hash unchanged");
                return;
            }
            Debug.LogWarning($"HASH CHANGED!! Refreshing {PlayerMapFaction.startingUnits.Count} units");
            _knownStartingUnitHash = hash;
                
            DestroyInitialPlacementGhosts();
            foreach (StartingUnitInfo startingUnitInfo in PlayerMapFaction.startingUnits)
            {
                for (int i = 0; i < startingUnitInfo.startingCadres; i++)
                {
                    CadrePlacementGhost placementGhost = CadrePlacementGhost.Create("Initial Placement Ghost", MapRenderer, startingUnitInfo.MapTile, startingUnitInfo.Country, GameState.Ruleset.GetNamedUnitType("Infantry"), UnitGhostPurpose.InitialPlacement);
                    InitialPlacementGhosts.Add(placementGhost);
                }
            }
            
        }

        public void SetActive(bool isActive)
        {
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

        private void RegisterUIComponent(UIComponent component)
        {
            if (component.globalName != null && component.globalName != String.Empty)
                if (!_uiComponentsByName.TryAdd(component.globalName, component)) Debug.LogError($"Duplicate use of component global name {component.globalName}");
            component.UIController = this;
            _uiComponents.Add(component);
            if (component is UIWindow window) _uiWindows.Add(window);
        }
        public void DerigsterUIObject(UIComponent uiComponent)
        {
            _uiComponentsByName.Remove(uiComponent.globalName);
            _uiComponents.Remove(uiComponent);
        }

        public T GetUIComponent<T>(string name) where T : UIComponent
        {
            if (_uiComponentsByName.TryGetValue(name, out UIComponent uiComponent))
            {
                return (T)uiComponent;
            }
            Debug.LogError($"UI Component {name} of type {typeof(T).Name} not found");
            return null;
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