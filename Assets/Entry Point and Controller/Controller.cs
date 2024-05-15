using System;
using System.Collections.Generic;
using System.Threading;
using GameBoard;
using GameLogic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private static Controller instance;
    private bool supressDestroyWarning = false;
    private static MapRendererConfig _mapRendererConfig;
    public static GameObject MapRendererPrefab => _mapRendererConfig.mapPrefab;
    public static int ActiveLocalPlayer;
    public static UnityServer ActiveServer;
    public static List<UnityClient> ActiveClients = new ();
    public static List<IDisposable> ThreadRisks = new List<IDisposable>();

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Attempted to create multiple controllers");
            supressDestroyWarning = true;
            Destroy(gameObject);
        }
        instance = this;
    }

    private void Start()
    {
        _mapRendererConfig = Resources.Load("MapRendererConfig") as MapRendererConfig;
        if (_mapRendererConfig == null)
            Debug.LogError("No MapRendererConfig found in a Resources folder. Add one with right-click -> Custom/MapRendererConfig.");
        
        // Setup test game with players
        ActiveServer = new UnityServer();
        ActiveClients.Add( new UnityClient(port:8889));
        ActiveClients.Add( new UnityClient(port:8890));
        ActiveServer.Start();
        Thread.Sleep(1000);
        //ActiveClients[0].DiscoverServers();
        ActiveClients[1].Connect("127.0.0.1", NetProtocol.DefaultPort, "", -1);
    }

    private void Update()
    {
        ActiveServer.Monitor();
        foreach (var client in ActiveClients)
        {
           client.Monitor();
        }
    }

    private void OnApplicationQuit()
    {
        foreach (IDisposable threadRisk in new List<IDisposable>(ThreadRisks))
        {
            threadRisk.Dispose();
        }

        supressDestroyWarning = true;
    }

    private void OnDestroy()
    {
        if (!supressDestroyWarning)
        {
            Debug.LogError("The Controller object was destroyed. This should never happen.");
        }

        instance = null;
    }
}
