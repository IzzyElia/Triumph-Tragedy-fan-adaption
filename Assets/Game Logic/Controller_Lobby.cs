
using System;
using System.Net;
using Game_Logic.TriumphAndTragedy;
using Game_Logic.TriumphAndTragedy.Lobby;
using GameLogic;
using GameSharedInterfaces.Triumph_and_Tragedy;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public partial class Controller
{
    // Lobby control
    [SerializeField] GameObject rootWrapper;
    [SerializeField] GameObject gameLobbyWrapper;
    [SerializeField] GameObject hostOrJoinWrapper;
    public GameObject factionsListParent;
        
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TMP_Dropdown scenarioDropdown;
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private TMP_InputField joinGameIPAddressInputField;
    [SerializeField] private Button StartGameButton;
    [SerializeField] private TextMeshProUGUI ipaddressText;
    private UnityClient _remoteClient;

    private LobbyFaction[] _lobbyFactions = Array.Empty<LobbyFaction>();

    private Scenario[] _scenarios = Array.Empty<Scenario>();
    void RefreshMenu()
    {
        Scenario[] scenarios = Resources.LoadAll<Scenario>("Scenarios");
        _scenarios = new Scenario[scenarios.Length];
        scenarioDropdown.ClearOptions();
        scenarioDropdown.onValueChanged.RemoveAllListeners();
        scenarioDropdown.onValueChanged.AddListener(RefreshScenarioDropdown);
        for (int i = 0; i < scenarios.Length; i++)
        {
            _scenarios[i] = scenarios[i];
            scenarioDropdown.options.Add(new TMP_Dropdown.OptionData(image:_scenarios[i].scenarioThumbnail, text:_scenarios[i].scenarioDisplayName));
        }
        RefreshScenarioDropdown(scenarioDropdown.value);
    }

    void RefreshScenarioDropdown(int value)
    {
        scenarioDropdown.captionText.text = scenarioDropdown.options[scenarioDropdown.value]?.text;
        //scenarioDropdown.image.sprite = scenarioDropdown.options[scenarioDropdown.value]?.image; // needs an aspect ratio fitter
    }
    
    void RefreshLobby()
    {
        string hostName = Dns.GetHostName();
        string ipAddress = "No network connection";
        IPAddress[] addresses = Dns.GetHostAddresses(hostName); // Get all IP addresses associated with the hostname
        foreach (IPAddress address in addresses)
        {
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ipAddress = address.ToString();
            }
        }
        ipaddressText.text = ipAddress;
        UnityClient localClient = null;
        if (LocalClients.Count > 0) localClient = LocalClients[0];

        if (localClient is null)
        {
            Debug.LogError("No local client exists");
        }
        else
        {
            GameFaction[] factions = localClient.GameState.GetEntitiesOfType<GameFaction>();
            if (_lobbyFactions.Length != factions.Length)
            {
                // Rebuild the game objects
                for (int i = 0; i < _lobbyFactions.Length; i++) Destroy(_lobbyFactions[i].gameObject);
                _lobbyFactions = new LobbyFaction[factions.Length];
                for (int i = 0; i < factions.Length; i++) _lobbyFactions[i] = LobbyFaction.Create(this, factions[i], localClient);
            }
            else
            {
                for (int i = 0; i < factions.Length; i++)
                {
                    _lobbyFactions[i].Refresh(factions[i], localClient);
                }
            }
        }

    }
    
    void LobbyStart()
    {
        // Setup buttons
        hostGameButton.onClick.AddListener(HostGame);
        joinGameButton.onClick.AddListener(JoinGame);
        StartGameButton.onClick.AddListener(StartGame);
        RefreshMenu();
    }
    
    void LobbyUpdate()
    {
        StartGameButton.interactable = IsHost;
        if (LocalClients.Count > 0 && LocalClients[0].Connected)
        {
            if (_lobbyState != LobbyState.InGame && LocalClients[0].GameStarted)
            {
                gameLobbyWrapper.SetActive(false);
                hostOrJoinWrapper.SetActive(false);
                rootWrapper.SetActive(false);
                _lobbyState = LobbyState.InGame;
                Debug.LogWarning("YIPPEE");
            }
            else if (_lobbyState != LobbyState.Lobby && !LocalClients[0].GameStarted)
            {
                rootWrapper.SetActive(true);
                gameLobbyWrapper.SetActive(true);
                hostOrJoinWrapper.SetActive(false);
                _lobbyState = LobbyState.Lobby;
            }

        }
        else if (_lobbyState != LobbyState.HostOrJoin)
        {
            rootWrapper.SetActive(true);
            gameLobbyWrapper.SetActive(false);
            hostOrJoinWrapper.SetActive(true);
            _lobbyState = LobbyState.HostOrJoin;
        }
    }

    void CleanupNetworkMembers()
    {
        if (ActiveServer is not null) ActiveServer.Dispose();
        foreach (var client in LocalClients)
        {
            client.Dispose();
        }
        LocalClients.Clear();
    }
    
    public void HostGame()
    {
        const ushort serverPort = NetProtocol.DefaultPort;
        const string password = "";
        string playerName = playerNameInputField.text;
        Scenario scenario = _scenarios[scenarioDropdown.value];
        CleanupNetworkMembers();
        ActiveServer = new UnityServer(new TTGameState(), port:serverPort, password:password);
        ((TTGameState)ActiveServer.GameState).BuildFromMapPrefab(rulesetName:scenario.mapName, scenarioName:scenario.name);
        ActiveServer.Start();
        LocalClients.Add(new UnityClient(GetFirstAvailablePort()));
        LocalClients[0].Connect(address:"127.0.0.1", port:serverPort, password:password, desiredPlayerSlot:-1, playerName:playerName);
    }

    public bool ValidateJoinGame()
    {
        string[] splt = joinGameIPAddressInputField.text.Split(':');
        string ipAddress = splt[0];
        string sPort = "8888";
        if (splt.Length > 1) sPort = joinGameIPAddressInputField.text.Split(':')[1];
        if (!ushort.TryParse(sPort, out ushort port)) return false;

        return true;
    }

    public void JoinGame()
    {
        string[] splt = joinGameIPAddressInputField.text.Split(':');
        string address = splt[0];
        ushort port = NetProtocol.DefaultPort;
        if (splt.Length > 1) port = ushort.Parse(joinGameIPAddressInputField.text.Split(':')[1]);
        JoinGame(address:address, port:port);
    }

    void JoinGame(string address, ushort port)
    {
        string playerName = playerNameInputField.text;
        CleanupNetworkMembers();
        LocalClients.Add(new UnityClient(port:8889));
        LocalClients[0].Connect(address:address, port:port, password:"", desiredPlayerSlot:-1, playerName:playerName);
    }

    public void StartGame()
    {
        if (IsHost)
        {
            ActiveServer.StartGame();
        }
        else
        {
            throw new InvalidOperationException("Not the host");
        }
    }
}