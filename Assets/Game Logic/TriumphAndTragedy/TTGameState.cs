using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GameBoard;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
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
    public partial class TTGameState : GameState, ITTGameState
    {
        // Fields and Properties
        const byte UpdatingGlobalFieldsHeader = 0;
        private const byte UpdatingCombatStateHeader = 1;

        // Synced fields
        public int Year { get; set; }
        public Season Season { get; set; }
        public GamePhase GamePhase { get; set; }
        public int PositionInTurnOrder { get; set; }
        public int[] PlayerOrder { get; set; }
        public bool[] PlayerCommitted { get; set; } // for tracking which players committed to a simultaneous action, etc initial placement
        public bool[] PlayerPassed { get; set; } // for tracking which players have passed during cardplayµø
        public GameCombat ActiveCombat = null;
        public List<int> ForcedCombats { get; private set; } = new List<int>(); // integer is the tile id
        public List<CombatOption> CommittedCombats { get; private set; } = new List<CombatOption>();
        public Dictionary<int, int> CombatSupports { get; private set; } = new Dictionary<int, int>();

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
        public bool HaveAllPlayersPassed
        {
            get
            {
                for (int i = 0; i < PlayerCount; i++)
                {
                    if (!PlayerPassed[i]) return false;
                }
                return true;
            }
        }
        public override int ActivePlayer => GamePhase == GamePhase.InitialPlacement || GamePhase == GamePhase.None ? -1 : PlayerOrder[PositionInTurnOrder];
        
        public override bool IsWaitingOnPlayer(int iPlayer)
        {
            if (iPlayer >= PlayerCount) return false;
            if (GamePhase == GamePhase.InitialPlacement || GamePhase == GamePhase.SelectSupport)
            {
                return !PlayerCommitted[iPlayer];
            }
            else if (GamePhase == GamePhase.Combat && ActiveCombat != null)
            {
                return ActiveCombat.iPhasingPlayer == iPlayer;
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
            // Assumes PlayerCount is constant after being synced
            for (int i = 0; i < PlayerCount; i++)
            {
                message.WriteByte((byte)PlayerOrder[i]);
            }

            for (int i = 0; i < PlayerCount; i++)
            {
                message.WriteByte((byte)(PlayerCommitted[i] == true ? 1 : 0));
            }

            for (int i = 0; i < PlayerCount; i++)
            {
                message.WriteByte((byte)(PlayerPassed[i] == true ? 1 : 0));
            }

            message.WriteUShort((ushort)ForcedCombats.Count);
            for (int i = 0; i < ForcedCombats.Count; i++)
            {
                message.WriteUShort((ushort)ForcedCombats[i]);
            }

            message.WriteUShort((ushort)CommittedCombats.Count);
            for (int i = 0; i < CommittedCombats.Count; i++)
            {
                CommittedCombats[i].Write(ref message);
            }

            message.WriteUShort((ushort)CombatSupports.Count);
            foreach ((int iCadre, int iTile) in CombatSupports)
            {
                message.WriteUShort((ushort)iCadre);
                message.WriteShort((short)iTile);
            }
            
            
            PushGameStateUpdate(ref message, iPlayer);
            NetworkMember.NetworkingLog("Pushing TTGameState global update");
        }

        private List<int> prevUnitsInvolvedInCombat = new List<int>();
        public void PushCombatState()
        {
            HashSet<int> unitsToPush = new HashSet<int>();
            foreach (var iCadre in prevUnitsInvolvedInCombat)
            {
                unitsToPush.Add(iCadre);
            }
            prevUnitsInvolvedInCombat.Clear();
            if (ActiveCombat is not null)
            {
                foreach (var ICadre in ActiveCombat.CalculateInvolvedCadreInterfaces())
                {
                    prevUnitsInvolvedInCombat.Add(ICadre.ID);
                    unitsToPush.Add(ICadre.ID);
                }
            }

            foreach (var iCadre in unitsToPush)
            {
                GetEntity<GameCadre>(iCadre).PushFullState();
            }
            foreach (int iPlayer in Players)
            {
                PushCombatState(iPlayer);
            }
            
        }

        public void PushCombatState(int iPlayer)
        {
            if (!IsPlayerSyncedOrBeingResynced(iPlayer)) return;
            DataStreamWriter message = StartGameStateUpdate(iPlayer);
            message.WriteByte(UpdatingCombatStateHeader);
            message.WriteByte((byte)(IsCombatHappening ? 1 : 0));
            if (IsCombatHappening)
            {
                foreach (var cadre in ActiveCombat.CalculateInvolvedCadreInterfaces())
                {
                    cadre.PushFullState();
                }
                ActiveCombat.WriteFullState(ref message);
            }
            PushGameStateUpdate(ref message, iPlayer);
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
                    
                    PlayerPassed = new bool[PlayerCount];
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        PlayerPassed[i] = message.ReadByte() != 0;
                    }
                    
                    ForcedCombats.Clear();
                    int forcedCombatsLength = message.ReadUShort();
                    for (int i = 0; i < forcedCombatsLength; i++)
                    {
                        int forcedCombat = message.ReadUShort();
                        ForcedCombats.Add(forcedCombat);
                    }
                    
                    CommittedCombats.Clear();
                    int committedCombatsLength = message.ReadUShort();
                    for (int i = 0; i < committedCombatsLength; i++)
                    {
                        CombatOption committedCombat = CombatOption.Recreate(ref message);
                        CommittedCombats.Add(committedCombat);
                    }
                    
                    CombatSupports.Clear();
                    int combatSupportsLength = message.ReadUShort();
                    for (int i = 0; i < combatSupportsLength; i++)
                    {
                        int iCadre = message.ReadUShort();
                        int iTile = message.ReadShort();
                        CombatSupports.Add(iCadre, iTile);
                    }
                    break;
                
                case UpdatingCombatStateHeader:
                    bool combatIsHappening = message.ReadByte() == 1;
                    if (combatIsHappening)
                    {
                        if (ActiveCombat is null) ActiveCombat = new GameCombat();
                        ActiveCombat.ReceiveFullState(this, ref message);
                    }
                    else
                    {
                        ActiveCombat = null;
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
            if (PlayerPassed == null) PlayerPassed = new bool[PlayerCount];
            for (int i = 0; i < PlayerCount; i++)
            {
                PlayerPassed[i] = false;
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
            GameFaction[] factions = GetEntitiesOfType<GameFaction>();
            for (int i = 0; i < factions.Length; i++)
            {
                factions[i].ProductionAvailable = factions[i].Production;
                factions[i].PushFullState();
            }
        }

        public void EndCardplay()
        {
            // End cardplay
            foreach (var country in GetEntitiesOfType<GameCountry>())
            {
                (int iFaction, int influence) highestInfluence = (-1, 0);
                (int iFaction, int influence) secondHighestInfluence = (-1, 0);
                for (int iFaction = 0; iFaction < country.influencePlayedByPlayer.Length; iFaction++)
                {
                    if (country.influencePlayedByPlayer[iFaction] > highestInfluence.influence)
                    {
                        secondHighestInfluence = highestInfluence;
                        highestInfluence = (iFaction, country.influencePlayedByPlayer[iFaction]);
                    }
                    
                    country.influencePlayedByPlayer[iFaction] = 0;
                }

                if (highestInfluence.influence > 0)
                {
                    int cancelledInfluence = secondHighestInfluence.influence;
                    int uncancelledInfluence = highestInfluence.influence - cancelledInfluence;
                    int iFaction = highestInfluence.iFaction;
                    GameFaction faction = GetEntity<GameFaction>(iFaction);
                    for (int i = 0; i < uncancelledInfluence; i++)
                    {
                        if (country.iFaction == iFaction)
                        {
                            country.AppliedInfluence++;
                            Debug.Log($"Added a {faction.Name} influence to #{country.ID}");
                        }
                        else if (country.iFaction == -1)
                        {
                            country.iFaction = iFaction;
                            country.AppliedInfluence++;
                            Debug.Log($"Added a {faction.Name} influence to #{country.ID}");

                        }
                        else
                        {
                            country.AppliedInfluence--;
                            Debug.Log($"Removed a {country.Faction.Name} influence from #{country.ID}");
                        }
                    }
                }
            }
            ResetPlayerStatuses(false);
            GamePhase = GamePhase.SelectCommandCards;
            PositionInTurnOrder = 0;
        }

        public void ResetPlayerStatuses(bool push)
        {
            if (!IsServer) throw new InvalidOperationException();
            for (int i = 0; i < PlayerPassed.Length; i++)
            {
                PlayerPassed[i] = false;
            }
            for (int i = 0; i < PlayerCommitted.Length; i++)
            {
                PlayerCommitted[i] = false;
            }
            if (push) PushGlobalFields();
        }

        public void EndCommandCardSelection()
        {
            ResetPlayerStatuses(false);

            GamePhase = GamePhase.GiveCommands;
            PositionInTurnOrder = 0;
        }

        public void AdvanceToCombatIfAllPlayersDoneWithSupport(bool push)
        {
            for (int i = 0; i < PlayerCommitted.Length; i++)
            {
                if (!PlayerCommitted[i]) return;
            }
            
            // All players selected support
            GamePhase = GamePhase.SelectNextCombat;
            if (push) PushGlobalFields();
        }
        
        /// <summary>
        /// Make sure to call PushGlobalFields()
        /// </summary>
        /// <returns>Returns true if advancing the turn marker turned past the last player in the turn order (usually meaning it's time for a new phase, but could also happen when ex looping back to the first player during cardplay)</returns>
        public bool AdvanceTurnMarker()
        {
            PositionInTurnOrder++;
            if (PositionInTurnOrder >= PlayerOrder.Length)
            {
                PositionInTurnOrder = 0;
                return true;
            }
            else
            {
                return false;
            }
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
            Random.InitState(DateTime.UtcNow.Ticks.GetHashCode());
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
        public Izzy.HashsetDictionary<int, GameCadre> CadresByTileID = new Izzy.HashsetDictionary<int, GameCadre>();
        public Queue<int> FreedCadreIDs = new Queue<int>();
        public int NextCadreID = 0;
        public Queue<int> FreedCombatIDs = new Queue<int>();
        public int NextCombatID = 0;
        public int NumBorders;

        private List<GameCombat> c_combats = new List<GameCombat>();
        public CombatOption[] GetCombatOptions()
        {
            if (GamePhase == GamePhase.SelectNextCombat || GamePhase == GamePhase.SelectSupport)
            {
                return CommittedCombats.ToArray();
            }
            else if (GamePhase == GamePhase.CommitCombats)
            {
                List<CombatOption> combatOptions = new List<CombatOption>();
                foreach (var tile in GetEntitiesOfType<GameTile>())
                {
                    bool[] factionHasUnits = new bool[EntitySlotsForType<GameFaction>()];
                    foreach (var cadre in tile.GetCadresOnTile())
                        factionHasUnits[cadre.Faction.ID] = true;
                    if (!factionHasUnits[ActivePlayer]) continue;
                    else
                    {
                        for (int iFaction = 0; iFaction < factionHasUnits.Length; iFaction++)
                        {
                            if (iFaction == ActivePlayer) continue;
                            if (!factionHasUnits[iFaction]) continue;
                            combatOptions.Add(new CombatOption(iTile:tile.ID, iAttacker:ActivePlayer, iDefender:iFaction, !ForcedCombats.Contains(tile.ID)));
                        }
                    }
                }

                return combatOptions.ToArray();
            }
            else
            {
                return Array.Empty<CombatOption>();
            }
        }
        
        public ICard GetCard(int id, CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Action: return GetOrCreateEntity<ActionCard>(id);
                case CardType.Investment: return GetOrCreateEntity<InvestmentCard>(id);
                default: throw new NotImplementedException($"Card Type {cardType.ToString()} needs implementation");
            }
        }

        public IGameFaction GetFaction(int iFaction)
        {
            return GetEntity<GameFaction>(iFaction) as IGameFaction;
        }

        public bool IsCombatHappening => ActiveCombat != null;
        public IGameCombat GetActiveCombat()
        {
            if (!IsCombatHappening) return null;
            return (IGameCombat)ActiveCombat;
        }

        public (int iTile, int iCountry, int startingCadres)[] GetStartingUnits(int iPlayer)
        {
            GameFaction faction = GetEntity<GameFaction>(iPlayer);
            return faction.startingUnits;
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
                border.Active = true;
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
                        mapCountry.SetFaction(-1, FactionMembershipStatus.Unaligned);
                    }
                }
                mapFaction.DestroyMapObject();
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
            Ruleset ruleset = Ruleset.LoadRuleset(rulesetName);
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
                tile.Resources = mapTile.resources;
                tile.ColonialResources = mapTile.colonialResources;
                tile.CitySize = mapTile.citySize;
                tile.TerrainType = mapTile.terrainType;
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
                    gameCountry.MembershipStatus = FactionMembershipStatus.InitialMember;
                }
            }
            
            for (int i = 0; i < scenario.factions.Count; i++)
            {

                Scenario.Faction scenarioFaction = scenario.factions[i];
                GameFaction faction = gameState.GetOrCreateEntity<GameFaction>(i);
                faction.Active = true;
                faction.Industry = scenarioFaction.startingIndustry;
                faction.iLeaderCountry = gameState.CountryIDs[scenarioFaction.leader];
                // The scenario faction name values are used farther below.
                // If the faction name is changed from the scenario faction name, update the below section/s as well
                faction.Name = scenarioFaction.name;
                
                foreach (string sCountry in scenarioFaction.countries)
                {
                    int iCountry = gameState.CountryIDs[sCountry];
                    GameCountry gameCountry = gameState.GetEntity<GameCountry>(iCountry);
                    gameCountry.iFaction = faction.ID;
                    gameCountry.MembershipStatus = FactionMembershipStatus.InitialMember;
                }
                faction.startingUnits =
                    new (int iTile, int iCountry, int startingCadres)[scenarioFaction.startingUnits.Count];
                for (int j = 0; j < faction.startingUnits.Length; j++)
                {
                    StartingUnitInfo startingUnitInfo = scenarioFaction.startingUnits[j]; 
                    MapTile mapTile = map.GetTileByName(startingUnitInfo.MapTile);
                    MapCountry mapCountry = map.GetCountryByName(startingUnitInfo.Country);
                    
                    if (mapTile is null)
                    {
                        Debug.LogError($"Invalid tile ('{startingUnitInfo.MapTile}') in {scenarioFaction.name}'s starting units #{j}");
                    }
                    if (mapCountry is null)
                    {
                        Debug.LogError($"Invalid Country ('{startingUnitInfo.Country}') in {scenarioFaction.name}'s starting units #{j}");
                    }
                    if (mapCountry is not null && mapTile is not null)
                    {
                        faction.startingUnits[j] = (
                            iTile:mapTile.ID, 
                            iCountry:mapCountry.ID,
                            startingUnitInfo.startingCadres);
                    }
                }
            }

            foreach (var specialDiplomacyAction in gameState.Ruleset.specialDiplomacyActionDefinitions)
            {
                foreach (var factionDefinition in specialDiplomacyAction.FactionDefinitions)
                {
                    for (int i = 0; i < scenario.factions.Count; i++)
                    {
                        if (scenario.factions[i].name == factionDefinition.faction)
                        {
                            factionDefinition.iFaction = i;
                            for (int j = 0; j < factionDefinition.countries.Length; j++)
                            {
                                factionDefinition.iCountries[j] = map.GetCountryByName(factionDefinition.countries[j]).ID;
                            }

                            for (int j = 0; j < factionDefinition.tiles.Length; j++)
                            {
                                factionDefinition.iTiles[j] = map.GetTileByName(factionDefinition.tiles[j]).ID;
                            }
                        }
                    }
                }
            }
            
            GenerateDeck(gameState);
            
            gameState.PlayerCount = gameState.GetEntitiesOfType<GameFaction>().Length;
            return gameState;
        }

        /// <summary>
        /// Generates a random deck for the gamestate. Assumes the gamestate has already been setup with BuildFromMapPrefab
        /// </summary>
        /// <param name="gameState"></param>
        public static void GenerateDeck(TTGameState gameState)
        {
            List<int> validCountries = new List<int>();
            GameCountry[] allCountries = gameState.GetEntitiesOfType<GameCountry>();
            for (int i = 0; i < allCountries.Length; i++)
            {
                if (allCountries[i].Faction == null) validCountries.Add(allCountries[i].ID);
            }

            for (int iSeason = 0; iSeason <= 2; iSeason++)
            {
                Season season;
                switch (iSeason)
                {
                    case 0: season = Season.Spring; break;
                    case 1: season = Season.Summer; break;
                    case 2: season = Season.Fall; break;
                    default: throw new NotImplementedException();
                }
                for (int i = 0; i < 20; i++)
                {
                    int iCard = i * (iSeason + 1);
                    ActionCard actionCard = gameState.GetOrCreateEntity<ActionCard>(iCard);
                    actionCard.Active = true;
                    actionCard.NumActions = Random.Range(minInclusive:4, maxExclusive:12);
                    actionCard.Initiative = i;
                    actionCard.Season = season;
                    int iCountry1 = Random.Range(minInclusive: 0, maxExclusive: validCountries.Count);
                    int iCountry2 = Random.Range(minInclusive: 0, maxExclusive: validCountries.Count);
                    actionCard.SetCountries(validCountries[iCountry1], validCountries[iCountry2]);
                }
            }

            Tech[] allTechs = gameState.Ruleset.techs;
            for (int i = 0; i < 60; i++)
            {
                InvestmentCard investmentCard = gameState.GetOrCreateEntity<InvestmentCard>(i);
                investmentCard.Active = true;
                investmentCard.FactoryValue = Random.Range(minInclusive: 1, maxExclusive: 5);
                int iTech1 = Random.Range(minInclusive: 0, maxExclusive: allTechs.Length);
                Tech tech1 = allTechs[Random.Range(minInclusive: 0, maxExclusive: allTechs.Length)];
                Tech tech2 = allTechs[Random.Range(minInclusive: 0, maxExclusive: allTechs.Length)];
                investmentCard.SetTechs(tech1, tech2);
            }
        }

        void AutomateInitialPlacement()
        {
            foreach (var faction in GetEntitiesOfType<GameFaction>())
            {
                foreach (var startingUnitInfo in GetStartingUnits(faction.ID))
                {
                    for (int i = 0; i < startingUnitInfo.startingCadres; i++)
                    {
                        int iUnitType = Random.Range(0, Ruleset.unitTypes.Length);
                        if (Ruleset.unitTypes[iUnitType].IdAndInitiative == Ruleset.iSeaTransportUnitType) iUnitType--;
                        GameCadre cadre = GameCadre.CreateCadre(this, iUnitType, startingUnitInfo.iCountry,
                            startingUnitInfo.iTile);
                        cadre.RecalculateDerivedValuesAndPushFullState();
                    }
                }
            }
        }
        public void JumpTo(GamePhase gamePhase)
        {
            if (!IsServer) throw new ServerOnlyException();
            if (this.GamePhase == GamePhase.InitialPlacement) AutomateInitialPlacement();
            switch (gamePhase)
            {
                case GamePhase.GiveCommands:
                    RandomizePlayerOrder();
                    PositionInTurnOrder = 0;
                    ResetPlayerStatuses(false);
                    foreach (var faction in GetEntitiesOfType<GameFaction>())
                    {
                        faction.CommandInitiative = Random.Range(0, 26);
                        faction.CommandsAvailable = Random.Range(4, 12);
                    }

                    foreach (var cadre in GetEntitiesOfType<GameCadre>())
                    {
                        if (cadre is not null)
                        {
                            cadre.Pips = Random.Range(1, cadre.MaxPips);
                            cadre.PushFullState();
                        }
                    }
                    break;
                case GamePhase.Combat:
                    RandomizePlayerOrder();
                    PositionInTurnOrder = 0;
                    ResetPlayerStatuses(false);
                    foreach (var faction in GetEntitiesOfType<GameFaction>())
                    {
                        faction.CommandInitiative = Random.Range(0, 26);
                        faction.CommandsAvailable = Random.Range(4, 12);
                    }
                    break;
                case GamePhase.InitialPlacement: throw new NotSupportedException();
                default: 
                    Debug.LogError($"Jumping to {gamePhase} implemented");
                    break;
            }

            this.GamePhase = gamePhase;
            PushGlobalFields();
        }

        public void AdvanceCommandingPhasingPlayer()
        {
            bool done = false;
            int failsafe = 10000;
            while (done == false)
            {
                failsafe--;
                if (failsafe < 0)
                {
                    throw new InvalidOperationException("Infinite loop attempting to advance turn marker");
                    break;
                }
                bool allPlayersHaveGone = AdvanceTurnMarker();
                if (allPlayersHaveGone)
                {
                    StartNewYear();
                    done = true;
                }
                else
                {
                    if (GetEntity<GameFaction>(ActivePlayer).CommandsAvailable > 0)
                    {
                        GamePhase = GamePhase.GiveCommands;
                        PushGlobalFields();
                        done = true;
                    }
                }
            }
        }
    }
}