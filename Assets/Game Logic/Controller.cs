using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Game_Logic.TriumphAndTragedy;
using GameBoard;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using GameLogic;
using GameSharedInterfaces;
using Izzy.ForcedInitialization;
using IzzysConsole;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public partial class Controller : MonoBehaviour
{
    [ConsoleCommand("server")]
    static TTGameState Console_Server() => (TTGameState)ActiveServer.GameState;
    [ConsoleCommand("dump")]
    static void Console_DumpEntityData(int clientid = -1)
    {
        string timestamp = Time.time.ToString(CultureInfo.InvariantCulture);
        List<(string, GameState)> allActiveGamestates = new ();
        allActiveGamestates.Add(("Server", ActiveServer.GameState));
        for (int i = 0; i < LocalClients.Count; i++)
        {
            if (LocalClients[i] != null && LocalClients[i].GameState != null)
                allActiveGamestates.Add(($"Client #{i}", LocalClients[i].GameState));
        }
        foreach ((string key, GameState gameState) in allActiveGamestates)
        {
            string path = Path.Join(Application.dataPath,"Dump", $"dump-{key}-{timestamp}.txt");
            StreamWriter writer = File.CreateText(path);
            foreach (var entity in ActiveServer.GameState.GetAllEntities())
            {
                writer.WriteLine($"{entity.GetType().Name} #{entity.ID}");
                // Iterate over all the entity's fields and print their names and values
                foreach (var field in entity.GetType().GetFields())
                {
                    writer.WriteLine($"\t{field.Name} = {field.GetValue(entity).ToString()}");
                }

                // Some properties rely on an active map renderer and so only work clientside
                if (!gameState.IsServer)
                {
                    foreach (var property in entity.GetType().GetProperties())
                    {
                        // If the property is not a reference type, write it
                        if (!property.PropertyType.IsClass)
                        {
                            writer.WriteLine($"\t{property.Name} = {property.GetValue(entity).ToString()}");
                        }
                    }
                }
            }
            writer.Close();
        }

        ConsoleManager.Log($"Dumped entities state");
    }
    
    private static Controller instance;
    private bool supressDestroyWarning = false;
    private static MapRendererConfig _mapRendererConfig;
    public static UnityClient ActiveLocalClient;
    private static int _lastActivePlayer;
    public static UnityServer ActiveServer;
    public static List<UnityClient> LocalClients = new ();
    public static bool IsHost => ActiveServer is not null;
    public static bool UnresolvedStateChange;
    public static bool UnresolvedResync;

    public static int _forcePlayerView = -1;
    [SerializeField] private GameObject _syncingOverlay;
    
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Attempted to create multiple controllers");
            supressDestroyWarning = true;
            Destroy(gameObject);
        }
        else
        {
            ForceInitializer.InitializeUninitializedTypes();
        }
        instance = this;
    }

    private void Start()
    {
        LobbyStart();
        // Setup test game with players
        //ActiveServer = new UnityServer(new TTGameState(), port:8888);
        //((TTGameState)ActiveServer.GameState).BuildFromMapPrefab(rulesetName:"Triumph_And_Tragedy", scenarioName:"Europe_1936");
        //ActiveClients.Add( new UnityClient(port:8889, botType:"Chamberlain"));
        //ActiveClients.Add( new UnityClient(port:8890, botType:"Chamberlain"));
        //ActiveClients.Add( new UnityClient(port:8891));
        //ActiveServer.Start();
        Thread.Sleep(1000);
        //ActiveClients[0].DiscoverServers();
        /*
        for (int i = 0; i < ActiveClients.Count; i++)
        {
            ActiveClients[i].DebuggingID = i;
            ActiveClients[i].Connect("127.0.0.1", NetProtocol.DefaultPort, "", -1);
        }
        */

        //ActiveServer.StartGame();
        
        //gameState.JumpTo(GamePhase.GiveCommands);
    }

    enum LobbyState
    {
        Uninitialized,
        HostOrJoin,
        Lobby,
        InGame
    }

    private LobbyState _lobbyState = LobbyState.Uninitialized;

    private void OnStateChange()
    {
        foreach (var localClient in LocalClients)
        {
            if (!localClient.GameState.IsSynced) return;
        }
        UnresolvedResync = false;
        UnresolvedStateChange = false;
        RefreshLobby();
    }
    private void Update()
    {
        if (UnresolvedResync || UnresolvedStateChange)
        {
            OnStateChange();
        }
        
        LobbyUpdate();
        
        ActiveServer?.DoMonitor();
        bool showSyncingOverlay = false;
        foreach (var client in LocalClients)
        {
            if (!client.Connected && !client.AttemptingConnenction) client.Reconnect();
            else client.DoMonitor();
            if (!client.GameState.IsSynced) showSyncingOverlay = true;
        }
        _syncingOverlay.SetActive(showSyncingOverlay);

        // Force the active local player if the associated number key is pressed
        if (_lobbyState == LobbyState.InGame)
        {
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), "F" + i.ToString())))
                {
                    if (_forcePlayerView == i-1) _forcePlayerView = -1;
                    else _forcePlayerView = i-1;
                    Debug.Log($"Showing player {i-1}");
                }
            }
        }

        
        // Focus the active local player
        if (IsHost)
        {
            if (ActiveLocalClient is not null &&
                (CombatPanel.AnimationOngoing ||
                 ActiveLocalClient.GameState.UIController.UnresolvedStateChange)) return;
            int activeLocalPlayer = -1;
            if (ActiveLocalClient == null || CombatPanel.ShowingFinalResult == false)
            {
                if (_forcePlayerView == -1)
                {
                    foreach (var client in LocalClients)
                    {
                        if (client.GameState.IsWaitingOnPlayer(client.GameState.iPlayer))
                        {
                            if (!ClientReadyToBeActivated(client)) break;
                            activeLocalPlayer = client.GameState.iPlayer;
                            break;
                        }
                    }
                }
                else
                {
                    activeLocalPlayer = _forcePlayerView;
                }
                foreach (var client in LocalClients)
                {
                    if (client.GameState.iPlayer == activeLocalPlayer)
                    {
                        if (!ClientReadyToBeActivated(client)) break;
                        if (client.IsBot && _forcePlayerView != client.GameState.iPlayer) // Is an ai and is ready to do its turn
                        {
                            Debug.Log("Running bot");
                            client.GameState.RunBot();
                        }
                        else // Is a human and is ready to do their turn
                        {
                            ActiveLocalClient = client;
                            client.GameState.UIController.SetActive(true);
                            foreach (var otherClient in LocalClients)
                            {
                                // TODO This breaks when there are unconnected/uninitialized clients
                                if (otherClient != client)
                                {
                                    if (otherClient.GameState.IsSynced) otherClient.GameState.UIController.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (LocalClients.Count > 0) ActiveLocalClient = LocalClients[0];
        }
    }

    bool ClientReadyToBeActivated(UnityClient client)
    {
        return client.GameStarted && client.GameState.UIController.Initialized && client.GameState.IsSynced;
    }

    public static ushort GetFirstAvailablePort()
    {
        for (ushort i = NetProtocol.DefaultPort; i < NetProtocol.DefaultPort + 500; i++)
        {
            ushort port = i;
            if (i >= 10000) port = (ushort)(i - 10000);
            try
            {
                // Create a TcpListener on the specified port
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
            }
            catch (SocketException)
            {
                Debug.Log($"Port {i} in use");
                continue; // Port is not available
            }
            
            try
            {
                UdpClient udpClient = new UdpClient(port);
                udpClient.Close();
            }
            catch (SocketException)
            {
                Debug.Log($"Port {i} in use");
                continue;
            }

            Debug.Log($"Port {i} is available");
            return i;
        }

        throw new InvalidOperationException("No open ports in range");
    }

    private void OnDestroy()
    {
        if (!supressDestroyWarning)
        {
            Debug.LogError("The Controller object was destroyed. This should never happen.");
        }

        instance = null;
    }

    private void OnApplicationQuit()
    {
        supressDestroyWarning = true;
        SharedData.SupressDestroyWarningGlobally = true;
        Disposer.DisposeAll(); 
    }
}
