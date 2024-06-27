using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Game_Logic.TriumphAndTragedy;
using GameBoard;
using GameBoard.UI;
using GameLogic;
using GameSharedInterfaces;
using Izzy.ForcedInitialization;
using IzzysConsole;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Random = UnityEngine.Random;

public class Controller : MonoBehaviour
{
    [ConsoleCommand("dump")]
    static void Console_DumpEntityData(int clientid = -1)
    {
        string timestamp = Time.time.ToString(CultureInfo.InvariantCulture);
        List<(string, GameState)> allActiveGamestates = new ();
        allActiveGamestates.Add(("Server", ActiveServer.GameState));
        for (int i = 0; i < ActiveClients.Count; i++)
        {
            if (ActiveClients[i] != null && ActiveClients[i].GameState != null)
                allActiveGamestates.Add(($"Client #{i}", ActiveClients[i].GameState));
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
    public static List<UnityClient> ActiveClients = new ();
    public static bool IsHost => ActiveServer is not null;
    
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
        // Setup test game with players
        TTGameState gameState = TTGameState.BuildFromMapPrefab(mapName:"Triumph_And_Tragedy", rulesetName:"Triumph_And_Tragedy", scenarioName:"Europe_1936");
        ActiveServer = new UnityServer(gameState, port:8888);
        ActiveClients.Add( new UnityClient(port:8889));
        ActiveClients.Add( new UnityClient(port:8890));
        ActiveClients.Add( new UnityClient(port:8891));
        ActiveServer.Start();
        Thread.Sleep(1000);
        //ActiveClients[0].DiscoverServers();
        for (int i = 0; i < ActiveClients.Count; i++)
        {
            ActiveClients[i].DebuggingID = i;
            ActiveClients[i].Connect("127.0.0.1", NetProtocol.DefaultPort, "", -1);
        }

        ActiveServer.StartGame();
        gameState.JumpTo(GamePhase.GiveCommands);
    }

    private void Update()
    {
        ActiveServer.DoMonitor();
        foreach (var client in ActiveClients)
        {
           client.DoMonitor();
        }

        if (IsHost)
        {
            
            int activeLocalPlayer = -1;
            foreach (var client in ActiveClients)
            {
                if (client.GameState.IsWaitingOnPlayer(client.GameState.iPlayer))
                {
                    if (!ClientReadyToBeActivated(client)) break;
                    activeLocalPlayer = client.GameState.iPlayer;
                    break;
                }
            }
            foreach (var client in ActiveClients)
            {
                if (client.GameState.iPlayer == activeLocalPlayer)
                {
                    if (!ClientReadyToBeActivated(client)) break;
                    ActiveLocalClient = client;
                    client.GameState.UIController.SetActive(true);
                    foreach (var otherClient in ActiveClients)
                    {
                        // TODO This breaks when there are unconnected/uninitialized clients
                        if (otherClient != client) otherClient.GameState.UIController.SetActive(false);
                    }
                }
            }
        }
    }

    bool ClientReadyToBeActivated(UnityClient client)
    {
        return client.GameState.UIController.Initialized && client.GameState.IsSynced;
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
        Disposer.DisposeAll();
        supressDestroyWarning = true;
        SharedData.SupressDestroyWarningGlobally = true;
    }
}
