using System;
using System.Collections.Generic;
using Game_Logic.TriumphAndTragedy.AI;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using IzzysConsole;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game_Logic.TriumphAndTragedy
{
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
        public int PhaseUID => ((Year & 0xFFFFFF) << 24) | (byte)GamePhase;
        
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

        public int PlayerPositionInTurnOrder(int iPlayer)
        {
            for (int i = 0; i < PlayerOrder.Length; i++)
            {
                if (PlayerOrder[i] == iPlayer) return i;
            }

            throw new InvalidOperationException($"Invalid player ID {iPlayer}");
        }

        private bool waitingOnBotActionReply = false;
        private int timeout = 0;
        public override void RunBot()
        {
            if (waitingOnBotActionReply)
            {
                timeout++;
                if (timeout >= 300)
                {
                    waitingOnBotActionReply = false;
                }
                else
                {
                    return;
                }
            }
            PlayerAction botAction;
            switch (GamePhase)
            {
                case GamePhase.InitialPlacement:
                    botAction = Bot.AI_InitialUnits(this);
                    if (botAction is not InitialUnitsAction) throw new BotException();
                    break;
                case GamePhase.Production:
                    botAction = Bot.AI_Production(this);
                    if (botAction is not ProductionAction) throw new BotException();
                    break;
                case GamePhase.Diplomacy:
                    botAction = Bot.AI_Cardplay(this);
                    if (botAction is not CardplayAction) throw new BotException();

                    break;
                case GamePhase.SelectCommandCards:
                    botAction = Bot.AI_SelectCommandsCards(this);
                    if (botAction is not CardplayAction) throw new BotException();

                    break;
                case GamePhase.GiveCommands:
                    botAction = Bot.AI_Commands(this);
                    if (botAction is not CommandsAction) throw new BotException();

                    break;
                case GamePhase.CommitCombats:
                    botAction = Bot.AI_SelectCombats(this);
                    if (botAction is not SelectCombatsAction) throw new BotException();

                    break;
                case GamePhase.SelectSupport:
                    botAction = Bot.AI_SelectSupports(this);
                    if (botAction is not SelectCombatSupportAction) throw new BotException();

                    break;
                case GamePhase.SelectNextCombat:
                    botAction = Bot.AI_SelectNextCombat(this);
                    if (botAction is not SelectNextCombatAction) throw new BotException();

                    break;
                case GamePhase.Combat:
                    botAction = Bot.AI_CombatDecision(this);
                    if (botAction is not CombatDecisionAction) throw new BotException();

                    break;
                
                
                
                default: throw new BotException();
            }

            waitingOnBotActionReply = true;
            timeout = 0;
            botAction.Send(OnBotActionReply);
        }

        private int fallbackAttempts = 0;
        void OnBotActionReply(bool success)
        {
            if (success)
            {
                Debug.LogWarning("Bot action reply received");
                waitingOnBotActionReply = false;
                fallbackAttempts = 0;
            }
            else
            {
                Debug.LogError("Bot action failed. Attempting a fallback move");
                PassAction fallbackAction = new PassAction();
                fallbackAction.Send(OnBotActionReply);
                fallbackAttempts++;
                if (fallbackAttempts >= 3) Client.SendSyncCheck();
            }
        }

        public override void RebuildBot()
        {
            Bot.Rebuild(this);
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

            if (NetworkMember.GameStarted) UIController.UnresolvedStateChange = true;
        }

        public override void OnSendingSync(int targetPlayer)
        {
            PushGlobalFields(targetPlayer);
        }

        protected override void OnSyncStarted()
        {
            if (NetworkMember.GameStarted) RebuildMapRendererFactionsGraph();
        }

        // Game flow and logic
        public override void OnGameStart()
        {
            PrepareForInitialSetup();
            // TODO this method still needed?
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
                factions[i].RecalculateDerivedValues();
                factions[i].CommandsAvailable = 0;
                factions[i].ProductionAvailable = factions[i].Production;
                factions[i].PushFullState();
            }
        }
        
        public void AdvanceCommandingPhasingPlayer()
        {
            bool done = false;
            int failsafe = 10000;
            CommittedCombats.Clear();
            while (done == false)
            {
                failsafe--;
                if (failsafe < 0)
                {
                    throw new InvalidOperationException("Infinite loop attempting to advance turn marker");
                    break;
                }
                bool allPlayersHaveGone = AdvanceTurnMarkerAndReturnTrueIfAllPlayersWent();
                if (allPlayersHaveGone)
                {
                    AdvanceSeason();
                    PushGlobalFields();
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
            
            RecalculateFactionDerivedValues();
        }

        public void EvaluateTerritoryControl()
        {
            GameTile[] tiles = GetEntitiesOfType<GameTile>();
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].EvaluateControl();
            }
            
            RecalculateFactionDerivedValues();
        }

        public void RecalculateFactionDerivedValues()
        {
            foreach (var gameFaction in GetEntitiesOfType<GameFaction>())
            {
                gameFaction.RecalculateDerivedValues();
            }
        }

        public void AdvanceSeason()
        {
            switch (Season)
            {
                case Season.Spring:
                    Season = Season.Summer;
                    break;
                case Season.Summer:
                    foreach (var gameFaction in GetEntitiesOfType<GameFaction>())
                    {
                        gameFaction.RunSummerBlockade();
                    }
                    Season = Season.Fall;
                    break;
                case Season.Fall:
                    Season = Season.Winter;
                    break;
                case Season.Winter:
                    StartNewYear();
                    return;
                default: throw new NotImplementedException();
            }

            GameFaction[] factions = GetEntitiesOfType<GameFaction>();
            for (int i = 0; i < factions.Length; i++)
            {
                factions[i].CommandsAvailable = 0;
                factions[i].PushFullState();
            }

            // If NOT winter
            ResetPlayerStatuses(false);
            GamePhase = GamePhase.SelectCommandCards;
            PushGlobalFields();
        }

        public bool needsProjectionUpdate = true;
        public void CalculateProjectedMembershipStatus ()
        {
            if (IsServer) throw new InvalidOperationException();
            GameCountry[] gameCountries = GetEntitiesOfType<GameCountry>();
            if (GamePhase == GamePhase.Diplomacy)
            {
                foreach (var gameCountry in gameCountries)
                {
                    if (gameCountry is null) continue;
                    if (gameCountry.MembershipStatus == FactionMembershipStatus.InitialMember ||
                        gameCountry.MembershipStatus == FactionMembershipStatus.Ally)
                    {
                        gameCountry.FactionProjection.iFaction = gameCountry.iFaction;
                        gameCountry.FactionProjection.Influence = int.MaxValue;
                    }
                    gameCountry.FactionProjection.Influence = gameCountry.AppliedInfluence;
                    gameCountry.FactionProjection.iFaction = gameCountry.iFaction;
                    (int iFaction, int influence) highestInfluence = (-1, 0);
                    (int iFaction, int influence) secondHighestInfluence = (-1, 0);
                    for (int iFaction = 0; iFaction < gameCountry.influencePlayedByPlayer.Length; iFaction++)
                    {
                        if (gameCountry.influencePlayedByPlayer[iFaction] > highestInfluence.influence)
                        {
                            secondHighestInfluence = highestInfluence;
                            highestInfluence = (iFaction, gameCountry.influencePlayedByPlayer[iFaction]);
                        }
                        else if (gameCountry.influencePlayedByPlayer[iFaction] > secondHighestInfluence.influence)
                        {
                            secondHighestInfluence = (iFaction, gameCountry.influencePlayedByPlayer[iFaction]);
                        }
                    }

                    if (highestInfluence.influence > 0)
                    {
                        int cancelledInfluence = secondHighestInfluence.influence;
                        int uncancelledInfluence = highestInfluence.influence - cancelledInfluence;
                        int iFaction = highestInfluence.iFaction;
                        for (int i = 0; i < uncancelledInfluence; i++)
                        {
                            if (gameCountry.iFaction == iFaction)
                            {
                                gameCountry.FactionProjection.Influence++;
                            }
                            else if (gameCountry.iFaction == -1)
                            {
                                gameCountry.FactionProjection.iFaction = iFaction;
                                gameCountry.FactionProjection.Influence++;

                            }
                            else
                            {
                                gameCountry.FactionProjection.Influence--;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var gameCountry in gameCountries)
                {
                    gameCountry.FactionProjection.Influence = gameCountry.MembershipStatus == FactionMembershipStatus.InitialMember ? int.MaxValue : gameCountry.AppliedInfluence;
                    gameCountry.FactionProjection.iFaction = gameCountry.iFaction;
                } 
            }
            
            foreach (var mapTile in MapRenderer.MapTilesByID)
            {
                mapTile.RecalculateMaterialDuringRuntime();
            }

            needsProjectionUpdate = false;
        }
        
        public void EndCardplay()
        {
            // End cardplay
            foreach (var country in GetEntitiesOfType<GameCountry>())
            {
                if (country is null) continue;
                if (country.MembershipStatus == FactionMembershipStatus.InitialMember ||
                    country.MembershipStatus == FactionMembershipStatus.Ally) continue;
                (int iFaction, int influence) highestInfluence = (-1, 0);
                (int iFaction, int influence) secondHighestInfluence = (-1, 0);
                for (int iFaction = 0; iFaction < country.influencePlayedByPlayer.Length; iFaction++)
                {
                    if (country.influencePlayedByPlayer[iFaction] > highestInfluence.influence)
                    {
                        secondHighestInfluence = highestInfluence;
                        highestInfluence = (iFaction, country.influencePlayedByPlayer[iFaction]);
                    }
                    else if (country.influencePlayedByPlayer[iFaction] > secondHighestInfluence.influence)
                    {
                        secondHighestInfluence = (iFaction, country.influencePlayedByPlayer[iFaction]);
                    }
                    
                    country.influencePlayedByPlayer[iFaction] = 0;
                }

                if (highestInfluence.influence > 0)
                {
                    int cancelledInfluence = secondHighestInfluence.influence;
                    int uncancelledInfluence = highestInfluence.influence - cancelledInfluence;
                    int iFaction = highestInfluence.iFaction;
                    GameFaction faction = GetEntity<GameFaction>(iFaction);
                    HashSet<GameCountry> modifiedCountries = new HashSet<GameCountry>();
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
                            Debug.Log($"Removed a {country.AssociatedFaction.Name} influence from #{country.ID}");
                        }

                        modifiedCountries.Add(country);
                    }

                    foreach (var gameCountry in modifiedCountries)
                    {
                        gameCountry.PushFullState();
                    }
                }
            }
            ResetPlayerStatuses(false);
            Season = Season.Spring;
            GamePhase = GamePhase.SelectCommandCards;
            PositionInTurnOrder = -1; // Will be advanced to 0 during AdvanceCommandingPhasingPlayer()
            AdvanceCommandingPhasingPlayer();
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
            PositionInTurnOrder = -1; // Will be advanced to 0 during AdvanceCommandingPhasingPlayer()
            AdvanceCommandingPhasingPlayer();
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
        public bool AdvanceTurnMarkerAndReturnTrueIfAllPlayersWent()
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
        
        public void AddNeutralStartingUnits()
        {
            foreach (var gameTile in GetEntitiesOfType<GameTile>())
            {
                if (gameTile is null) continue;
                if (gameTile.Muster <= 0) continue;
                if (gameTile.Country is null) continue;
                if (gameTile.Country.Faction == null)
                {
                    GameCadre.CreateCadre(
                        gameState:this, 
                        iUnitType:Ruleset.GetIDOfNamedUnitType("Fortress"),
                        iCountry:gameTile.Country.ID, 
                        iTile:gameTile.ID, 
                        pips:gameTile.Muster);
                }
            }
        } 

        
        
        
        // Setup and core logic
        protected override void SetupRecalculationQueueDictionary()
        {
            RecalculationQueue.EnsureKey(typeof(GameCadre));
            RecalculationQueue.EnsureKey(typeof(GameTile));
            RecalculationQueue.EnsureKey(typeof(GameBorder));
            RecalculationQueue.EnsureKey(typeof(ActionCard));
            RecalculationQueue.EnsureKey(typeof(InvestmentCard));
            RecalculationQueue.EnsureKey(typeof(GameCountry));
            RecalculationQueue.EnsureKey(typeof(GameFaction));
            RecalculationQueue.EnsureKey(typeof(GameCombat));
        }
        private void RecalculateType(Type type)
        {
            try
            {
                foreach (var entity in RecalculationQueue.Get_CertainOfKey(type)) entity.RecalculateDerivedValues();
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError($"Entity type {type} not added to the RecalculationQueue dictionary");
            }
        }
        protected override void HandleRecalculations()
        {
            RecalculateType(typeof(GameCadre));
            RecalculateType(typeof(GameTile));
            RecalculateType(typeof(GameBorder));
            RecalculateType(typeof(ActionCard));
            RecalculateType(typeof(InvestmentCard));
            RecalculateType(typeof(GameCountry));
            RecalculateType(typeof(GameFaction));
            RecalculateType(typeof(GameCombat));
        }

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
        [ConsoleCommand("getcombats")]
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
                    bool neutralUnits = false;
                    foreach (var cadre in tile.GetCadresOnTile())
                    {
                        if (cadre.Faction is not null) factionHasUnits[cadre.Faction.ID] = true;
                        else neutralUnits = true;
                    }
                    if (factionHasUnits[ActivePlayer])
                    {
                        for (int iFaction = 0; iFaction < factionHasUnits.Length; iFaction++)
                        {
                            if (iFaction == ActivePlayer) continue;
                            if (!factionHasUnits[iFaction]) continue;
                            combatOptions.Add(new CombatOption(iTile:tile.ID, iAttacker:ActivePlayer, iDefender:iFaction, isOptional:!ForcedCombats.Contains(tile.ID)));
                        }

                        if (neutralUnits)
                        {
                            combatOptions.Add(new CombatOption(iTile:tile.ID, iAttacker:ActivePlayer, iDefender:-1, isOptional:!ForcedCombats.Contains(tile.ID)));
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

        public IGameCadre GetCadre(int iCadre)
        {
            return GetEntity<GameCadre>(iCadre) as IGameCadre;
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
            if (!IsServer) throw new ServerOnlyException();
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
        public override void FullyRefreshMapRenderer()
        {
            RebuildMapRendererFactionsGraph();
            foreach (var gameEntity in GetAllEntities())
            {
                gameEntity.RefreshMapState();
            }
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
                    if (mapCountry.associatedFaction == mapFaction)
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
                if (i < existingMapFactions.Length) MapRenderer.MapFactionsByID[i] = existingMapFactions[i];
                else
                {
                    mapFaction = MapFaction.Create(faction.Name, MapRenderer, i, faction.IdeologyName);
                }
            }
        }

        /// <summary>
        /// The serverside setup
        /// </summary>
        /// <param name="mapPrefab"></param>
        /// <param name="isForServer"></param>
        /// <returns></returns>
        public void BuildFromMapPrefab(string rulesetName, string scenarioName)
        {
            Scenario scenario = Scenario.LoadScenario(scenarioName);
            MapName = scenario.mapName;
            Map map = Map.LoadMap(MapName).GetComponent<Map>();
            Ruleset ruleset = Ruleset.LoadRuleset(rulesetName);
            Ruleset = ruleset;
            RulesetName = rulesetName;
            map.GameState = this;
            RegisterEntityType<GameTile>(map.MapTilesByID.Length);
            RegisterEntityType<GameBorder>(map.MapBordersByID.Length);
            RegisterEntityType<GameFaction>(scenario.factions.Count);
            RegisterEntityType<GameCountry>(map.MapCountriesByID.Length);
            RegisterEntityType<GameCadre>(map.MaxCadres);
            RegisterEntityType<ActionCard>(100); // TODO Actually figure out the deck size and set the cap to that
            RegisterEntityType<InvestmentCard>(100);
            NumBorders = map.MapBordersByID.Length;
            for (int i = 0; i < map.MapCountriesByID.Length; i++)
            {
                MapCountry mapCountry = map.MapCountriesByID[i];
                GameCountry country = GetOrCreateEntity<GameCountry>(mapCountry.ID);
                country.InternalName = mapCountry.InternalName;
                if (mapCountry.Capital is null) Debug.LogError($"{mapCountry.name} has no capital!");
                country.iCapital = mapCountry.Capital is null ? -1 : mapCountry.Capital.ID;
                country.Active = true;
                CountryIDs.Add(mapCountry.name, mapCountry.ID);
                if (!(mapCountry.colonialOverlord is null))
                    country.iColonialOverlord = mapCountry.colonialOverlord.ID;
            }
            
            for (int i = 0; i < map.MapTilesByID.Length; i++)
            {
                MapTile mapTile = map.MapTilesByID[i];
                GameTile tile = GetOrCreateEntity<GameTile>(mapTile.ID);
                tile.Active = true;
                tile.Resources = mapTile.resources;
                tile.ColonialResources = mapTile.colonialResources;
                tile.CitySize = mapTile.citySize;
                tile.TerrainType = mapTile.terrainType;
                TileIDs.Add(mapTile.name, mapTile.ID);
                MapCountry mapCountry = mapTile.mapCountry;
                
                if (mapCountry == null) tile.iCountry = -1;
                else tile.iCountry = mapTile.mapCountry.ID;
                
            }


            CalculateDerivedTileAndBorderValues(map);
            
            foreach (MapCountry mapCountry in map.MapCountriesByID)
            {
                if (mapCountry.associatedFaction != null)
                {
                    GameCountry gameCountry = GetEntity<GameCountry>(mapCountry.ID);
                    gameCountry.iFaction = mapCountry.associatedFaction.ID;
                    gameCountry.MembershipStatus = FactionMembershipStatus.InitialMember;
                }
            }
            
            for (int i = 0; i < scenario.factions.Count; i++)
            {
                Scenario.Faction scenarioFaction = scenario.factions[i];
                GameFaction faction = GetOrCreateEntity<GameFaction>(i);
                faction.Active = true;
                faction.Industry = scenarioFaction.startingIndustry;
                faction.iLeaderCountry = CountryIDs[scenarioFaction.leader];
                // The scenario faction name values are used farther below.
                // If the faction name is changed from the scenario faction name, update the below section/s as well
                faction.Name = scenarioFaction.name;
                faction.IdeologyName = scenarioFaction.Ideology;
                
                foreach (string sCountry in scenarioFaction.countries)
                {
                    int iCountry = CountryIDs[sCountry];
                    GameCountry gameCountry = GetEntity<GameCountry>(iCountry);
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

                foreach (var specialStartingUnitInfo in scenarioFaction.startingSpecialUnits)
                {
                    UnitType unitType = ruleset.GetNamedUnitType(specialStartingUnitInfo.UnitType);
                    if (unitType is null) Debug.LogError($"Invalid unit type {specialStartingUnitInfo.UnitType}");
                    MapTile mapTile = map.GetTileByName(specialStartingUnitInfo.Tile);
                    if (mapTile is null) Debug.LogError($"Invalid map tile {specialStartingUnitInfo.Tile}");
                    MapCountry mapCountry = map.GetCountryByName(specialStartingUnitInfo.Country);
                    if (mapCountry is null) Debug.LogError($"Invalid country {specialStartingUnitInfo.Country}");
                    try
                    {
                        GameCadre.CreateCadre(this, 
                            iUnitType: unitType.IdAndInitiative, 
                            iCountry: mapCountry.ID,
                            iTile: mapTile.ID, 
                            pips: specialStartingUnitInfo.pips);
                    }
                    catch (NullReferenceException e)
                    {
                        Debug.LogError($"There was an issue creating the special starting unit at {specialStartingUnitInfo.Tile}");
                        continue;
                    }
                }
            }

            foreach (var specialDiplomacyAction in Ruleset.specialDiplomacyActionDefinitions)
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

            foreach (var tile in GetEntitiesOfType<GameTile>())
            {
                tile.iStartingFaction = tile.Country?.iFaction ?? -1;
            }

            GenerateDeck(this);
            
            // TODO Initial draw

            Year = scenario.startYear;
            PlayerCount = GetEntitiesOfType<GameFaction>().Length;
            Server.SetNumberOfPlayerSlots(PlayerCount);
            AddNeutralStartingUnits();
            InitializeFields();
            RecalculateAllDerivedValues();
            RunRecalculations();
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
                        FlagForRecalculation(cadre);
                        cadre.PushFullState();
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
                    Season = Season.Spring;
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
                            cadre.Pips = 3;
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
    }
}