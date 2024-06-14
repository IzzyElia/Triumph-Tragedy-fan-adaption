using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using Izzy;
using IzzysConsole.Utils;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Game_Logic.TriumphAndTragedy
{
    public static class FactionIDs
    {
        public const int imperialistsID = 0;
        public const int fascistsID = 1;
        public const int communistsID = 2;
        public static Dictionary<string, int> ImperialistStartingCadresPerTile = new();
        public static Dictionary<string, int> FascistStartingCadresPerTile = new();
        public static Dictionary<string, int> CommunistStartingCadresPerTile = new();

        static FactionIDs()
        {
            ImperialistStartingCadresPerTile.Add("London", 3);
            ImperialistStartingCadresPerTile.Add("Glasgow", 1);
            ImperialistStartingCadresPerTile.Add("Suez", 1);
            ImperialistStartingCadresPerTile.Add("Dehli", 3);
            ImperialistStartingCadresPerTile.Add("Bombay", 3);
            ImperialistStartingCadresPerTile.Add("Paris", 2);
            ImperialistStartingCadresPerTile.Add("Marseilles", 3);
            ImperialistStartingCadresPerTile.Add("Algiers", 1);
            
            FascistStartingCadresPerTile.Add("Berlin", 6);
            FascistStartingCadresPerTile.Add("Ruhr", 4);
            FascistStartingCadresPerTile.Add("Munich", 2);
            FascistStartingCadresPerTile.Add("Konigsberg", 2);
            FascistStartingCadresPerTile.Add("Rome", 4);
            FascistStartingCadresPerTile.Add("Milan", 2);
            FascistStartingCadresPerTile.Add("Tripoli", 2);

            CommunistStartingCadresPerTile.Add("Moscow", 3);
            CommunistStartingCadresPerTile.Add("Leningrad", 2);
            CommunistStartingCadresPerTile.Add("Kiev", 1);
            CommunistStartingCadresPerTile.Add("Odessa", 1);
            CommunistStartingCadresPerTile.Add("Kharkov", 1);
            CommunistStartingCadresPerTile.Add("Stalingrad", 1);
            CommunistStartingCadresPerTile.Add("Urals", 1);
            CommunistStartingCadresPerTile.Add("Baku", 2);
        }
    }
    
    // ReSharper disable once InconsistentNaming
    public class TTGameState : GameState, ITTGameState
    {
        // Fields and Properties

        const byte UpdatingGlobalFieldsHeader = 0;

        public int Year { get; set; }
        public Season Season { get; set; }
        public GamePhase GamePhase { get; set; }
        public int PositionInTurnOrder { get; set; }
        public int[] PlayerOrder { get; set; }
        public bool[] PlayerCommitted { get; set; } // for tracking which players committed to a simultaneous action, etc initial placement

        public bool AllPlayersAreCommitted
        {
            get
            {
                for (int i = 0; i < PlayerCount; i++)
                {
                    if (!PlayerCommitted[i]) return false;
                }
                return true;
            }
        }
        public override int ActivePlayer => GamePhase == GamePhase.InitialPlacement || GamePhase == GamePhase.None ? -1 : PlayerOrder[PositionInTurnOrder];
        public bool AllPlayersHaveGone => PositionInTurnOrder >= PlayerOrder.Length;

        public override bool IsWaitingOnPlayer(int iPlayer)
        {
            if (iPlayer >= PlayerCount) return false;
            if (GamePhase == GamePhase.InitialPlacement)
            {
                return !PlayerCommitted[iPlayer];
            }
            else
            {
                return ActivePlayer == iPlayer;
            }
        }
        
        // Syncing
        public void PushGlobalFields()
        {
            foreach (int iPlayer in Players)
            {
               PushGlobalFields(iPlayer); 
            }
        }
        public void PushGlobalFields(int iPlayer)
        {
            if (!IsPlayerSyncedOrBeingResynced(iPlayer)) return;
            DataStreamWriter message = StartGameStateUpdate(iPlayer);
            message.WriteByte(UpdatingGlobalFieldsHeader);
            message.WriteInt(Year);
            message.WriteByte((byte)Season);
            message.WriteByte((byte)GamePhase);
            message.WriteByte((byte)PositionInTurnOrder);
            for (int i = 0; i < PlayerCount; i++)
            {
                message.WriteByte((byte)PlayerOrder[i]);
            }

            for (int i = 0; i < PlayerCount; i++)
            {
                message.WriteByte((byte)(PlayerCommitted[i] == true ? 1 : 0));
            }
            PushGameStateUpdate(ref message, iPlayer);
            NetworkMember.NetworkingLog("Pushing TTGameState global update");
        }
        
        public override void ReceiveGameStateUpdate(ref DataStreamReader message)
        {
            byte header = message.ReadByte();
            switch (header)
            {
                case UpdatingGlobalFieldsHeader:
                    NetworkMember.NetworkingLog("Received TTGameState full update");
                    InitializeFields();
                    Year = message.ReadInt();
                    Season = (Season)message.ReadByte();
                    GamePhase = (GamePhase)message.ReadByte();
                    PositionInTurnOrder = (int)message.ReadByte();
                    PlayerOrder = new int[PlayerCount];
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        PlayerOrder[i] = message.ReadByte();
                    }

                    PlayerCommitted = new bool[PlayerCount];
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        PlayerCommitted[i] = message.ReadByte() != 0;
                    }
                    break;
            }

            UIController.UnresolvedStateChange = true;
        }

        public override void OnSendingSync(int targetPlayer)
        {
            PushGlobalFields(targetPlayer);
        }

        protected override void OnSyncStarted()
        {
            RebuildMapRendererFactionsGraph();
        }

        // Game flow and logic
        public override void OnGameStart()
        {
            if (!IsServer) throw new InvalidOperationException();
            InitializeFields();
            PrepareForInitialSetup();
        }

        private void InitializeFields()
        {
            if (PlayerOrder == null)
            {
                if (PlayerCount == -1) throw new InvalidOperationException("PlayerCount not set");
                PlayerOrder = new int[PlayerCount];
            }
            if (PlayerCommitted == null) PlayerCommitted = new bool[PlayerCount];
            for (int i = 0; i < PlayerCount; i++)
            {
                PlayerCommitted[i] = false;
            }
        }

        public void PrepareForInitialSetup()
        {
            GamePhase = GamePhase.InitialPlacement;
            PushGlobalFields();
        }

        public void StartNewYear()
        {
            Year++;
            Season = Season.NewYear;
            GamePhase = GamePhase.Production;
            PositionInTurnOrder = 0;
            RandomizePlayerOrder();
        }

        public void RandomizePlayerOrder()
        {
            int[] results = RollDice(6);
            switch (results[0])
            {
                case 1:
                    PlayerOrder = new int[3]
                        { 0, 2, 1 };
                    break;
                case 2:
                    PlayerOrder = new int[3]
                        { 0, 1, 2 };
                    break;
                case 3:
                    PlayerOrder = new int[3]
                        { 1, 0, 2 };
                    break;
                case 4:
                    PlayerOrder = new int[3]
                        { 1, 2, 0 };
                    break;
                case 5:
                    PlayerOrder = new int[3]
                        { 2, 1, 0 };
                    break;
                case 6:
                    PlayerOrder = new int[3]
                        { 2, 0, 1 };
                    break;
            }
        }
        
        public int[] RollDice(params int[] dice)
        {
            int[] results = new int[dice.Length];
            string resultsString = "Rolled ";
            for (int i = 0; i < dice.Length; i++)
            {
                results[i] = Random.Range(0, dice[i]) + 1;
                resultsString += $"{results[i]}/{dice[i]}, ";
            }
            Debug.Log(resultsString);
            return results;
        }

        
        
        
        // Setup and core logic
        public Dictionary<string, int> CountryIDs = new Dictionary<string, int>();
        public Dictionary<string, int> TileIDs = new Dictionary<string, int>();

        public Dictionary<UnorderedPair<int>, GameBorder> BorderOfTiles =
            new Dictionary<UnorderedPair<int>, GameBorder>();
        public HashsetDictionary<int, GameCadre> CadresByTileID = new HashsetDictionary<int, GameCadre>();
        public Queue<int> FreedCadreIDs = new Queue<int>();
        public int NextCadreID = 0;
        public int NumBorders;

        public ICard GetCard(int id, CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Action: return GetOrCreateEntity<ActionCard>(id);
                case CardType.Investment: return GetOrCreateEntity<InvestmentCard>(id);
                default: throw new NotImplementedException($"Card Type {cardType.ToString()} needs implementation");
            }
        }
        public override void CalculateDerivedTileAndBorderValues(Map map)
        {
            TileIDs.Clear();
            for (int i = 0; i < map.MapTilesByID.Length; i++)
            {
                MapTile mapTile = map.MapTilesByID[i];
                TileIDs[mapTile.name] = i;
                GameTile tile = GetEntity<GameTile>(i);
                List<MapBorder> nonEdgeBorders = new List<MapBorder>();
                foreach (var borderReference in mapTile.connectedBorders)
                {
                    if (borderReference.border.connectedMapTiles.Count > 1)
                        nonEdgeBorders.Add(borderReference.border);
                }
                tile.ConnectedTileIDs = new int[nonEdgeBorders.Count];
                tile.ConnectedTileBorderIDs = new int[nonEdgeBorders.Count];
                for (int j = 0; j < nonEdgeBorders.Count; j++)
                {
                    MapBorder border = nonEdgeBorders[j];
                    MapTile otherTile = null;
                    foreach (var connectedTile in border.connectedMapTiles)
                    {
                        if (connectedTile != mapTile)
                        {
                            otherTile = connectedTile;
                            break;
                        }
                    }

                    // ReSharper disable once PossibleNullReferenceException
                    tile.ConnectedTileIDs[j] = otherTile.ID;
                    tile.ConnectedTileBorderIDs[j] = border.ID;
                }
            }
            for (int i = 0; i < map.MapCountriesByID.Length; i++)
            {
                MapCountry country = map.MapCountriesByID[i];
                CountryIDs[country.name] = i;
            }
            for (int i = 0; i < map.MapBordersByID.Length; i++)
            {
                MapBorder mapBorder = map.MapBordersByID[i];
                GameBorder border = GetOrCreateEntity<GameBorder>(mapBorder.ID);
                border.BorderType = mapBorder.borderType;
                if (mapBorder.connectedMapTiles.Count == 2)
                {
                    UnorderedPair<int> connectedTiles = new UnorderedPair<int>(
                        mapBorder.connectedMapTiles[0].ID,
                        mapBorder.connectedMapTiles[1].ID);
                    BorderOfTiles.TryAdd(connectedTiles, border);
                }
            }
        }
        protected override void FullyRefreshMapRenderer()
        {
            foreach (var gameTile in GetEntitiesOfType<GameTile>())
            {
                MapTile mapTile = gameTile.MapTile;
                Color factionColor = gameTile.Faction?.Color ?? gameTile.Country?.Color ?? Color.black;
                Color nationalColor = gameTile.Country?.Color ?? Color.black;
                Color overlordColor = gameTile.Country?.ColonialOverlord?.Color ?? Color.black;
                Color occupierColor = gameTile.IsOccupied ? gameTile.Occupier.Color : Color.black;
                mapTile.SetupMaterialDuringRuntime(
                    terrain:gameTile.TerrainType, 
                    nationalColor:nationalColor,
                    overlordColor:overlordColor,
                    factionColor:factionColor,
                    occupierColor:occupierColor,
                    gameTile.IsOccupied,
                    gameTile.Country?.IsColony ?? false);
            }
            RebuildMapRendererFactionsGraph();
            MapRenderer.RecalculateAppearanceAfterResync();
        }

        public void RebuildMapRendererFactionsGraph()
        {
            GameFaction[] factions = GetEntitiesOfType<GameFaction>();
            MapFaction[] existingMapFactions = new MapFaction[MapRenderer.MapFactionsByID.Length];
            MapRenderer.MapFactionsByID.CopyTo(existingMapFactions, 0);
            //trim down and destroy excess members of MapRenderer.MapFactionsByID
            for (int i = factions.Length; i < existingMapFactions.Length; i++)
            {
                MapFaction mapFaction = existingMapFactions[i];
                foreach (var mapCountry in MapRenderer.MapCountriesByID)
                {
                    if (mapCountry.faction == mapFaction)
                    {
                        mapCountry.SetFaction(-1);
                    }
                }
                Object.Destroy(mapFaction);
            }
            
            MapRenderer.MapFactionsByID = new MapFaction[factions.Length];
            for (int i = 0; i < factions.Length; i++)
            {
                GameFaction faction = factions[i];
                MapFaction mapFaction;
                if (i < existingMapFactions.Length) mapFaction = existingMapFactions[i];
                else
                {
                    mapFaction = MapFaction.Create(faction.Name, MapRenderer, i);
                }

                mapFaction.ID = i;
                MapRenderer.MapFactionsByID[i] = mapFaction;
            }
        }

        /// <summary>
        /// The serverside setup
        /// </summary>
        /// <param name="mapPrefab"></param>
        /// <param name="isForServer"></param>
        /// <returns></returns>
        public static TTGameState BuildFromMapPrefab(string mapName, string rulesetName, string scenarioName)
        {
            TTGameState gameState = new TTGameState();
            Map map = Map.LoadMap(mapName).GetComponent<Map>();
            Ruleset ruleset = GameLogic.Ruleset.LoadRuleset(rulesetName);
            Scenario scenario = Scenario.LoadScenario(scenarioName);
            gameState.Ruleset = ruleset;
            gameState.RulesetName = rulesetName;
            map.GameState = gameState;
            gameState.MapName = mapName;
            gameState.RegisterEntityType<GameTile>(map.MapTilesByID.Length);
            gameState.RegisterEntityType<GameBorder>(map.MapBordersByID.Length);
            gameState.RegisterEntityType<GameFaction>(scenario.factions.Count);
            gameState.RegisterEntityType<GameCountry>(map.MapCountriesByID.Length);
            gameState.RegisterEntityType<GameCadre>(map.MaxCadres);
            gameState.RegisterEntityType<ActionCard>(100); // TODO Actually figure out the deck size and set the cap to that
            gameState.RegisterEntityType<InvestmentCard>(100);
            gameState.NumBorders = map.MapBordersByID.Length;
            for (int i = 0; i < map.MapCountriesByID.Length; i++)
            {
                MapCountry mapCountry = map.MapCountriesByID[i];
                GameCountry country = gameState.GetOrCreateEntity<GameCountry>(mapCountry.ID);
                country.Active = true;
                gameState.CountryIDs.Add(mapCountry.name, mapCountry.ID);
                if (!(mapCountry.colonialOverlord is null))
                    country.iColonialOverlord = mapCountry.colonialOverlord.ID;
            }
            
            for (int i = 0; i < map.MapTilesByID.Length; i++)
            {
                MapTile mapTile = map.MapTilesByID[i];
                GameTile tile = gameState.GetOrCreateEntity<GameTile>(mapTile.ID);
                tile.Active = true;
                gameState.TileIDs.Add(mapTile.name, mapTile.ID);
                MapCountry mapCountry = mapTile.mapCountry;
                
                if (mapCountry == null) tile.iCountry = -1;
                else tile.iCountry = mapTile.mapCountry.ID;
                
            }


            gameState.CalculateDerivedTileAndBorderValues(map);
            
            foreach (MapCountry mapCountry in map.MapCountriesByID)
            {
                if (mapCountry.faction != null)
                {
                    GameCountry gameCountry = gameState.GetEntity<GameCountry>(mapCountry.ID);
                    gameCountry.iFaction = mapCountry.faction.ID;
                }
            }
            
            for (int i = 0; i < scenario.factions.Count; i++)
            {
                Scenario.Faction scenarioFaction = scenario.factions[i];
                GameFaction faction = gameState.GetOrCreateEntity<GameFaction>(i);
                faction.Active = true;
                faction.iLeaderCountry = gameState.CountryIDs[scenarioFaction.leader];

                faction.Name = scenarioFaction.name;
                foreach (string sCountry in scenarioFaction.countries)
                {
                    int iCountry = gameState.CountryIDs[sCountry];
                    gameState.GetEntity<GameCountry>(iCountry).iFaction = faction.ID;
                }
                faction.startingUnits =
                    new (int iTile, int iCountry, int startingCadres)[scenarioFaction.startingUnits.Count];
                for (int j = 0; j < faction.startingUnits.Length; j++)
                {
                    StartingUnitInfo startingUnitInfo = scenarioFaction.startingUnits[j];
                    faction.startingUnits[j] = (
                        startingUnitInfo.MapTile.ID, 
                        startingUnitInfo.Country.ID,
                        startingUnitInfo.startingCadres);
                }
            }

            gameState.PlayerCount = gameState.GetEntitiesOfType<GameFaction>().Length;
            return gameState;
        }
        
    }
}