using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Izzy;
using Unity.Collections;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{

    public enum CountryAttributes
    {
        /// <summary>
        /// No more than 3 pips per cadre
        /// </summary>
        LimitedCadres,
    }
    public enum FactionAttribute
    {
        /// <summary>
        /// May make winter moves in home territory
        /// </summary>
        WinterMovesInHomeTerritory,
    }
    
    public class GameFaction : GameEntity, IGameFaction
    {
        private const byte _totalPipsUpdateHeader = 0;
        public string Name;
        public string IdeologyName;
        public MapFaction MapFaction
        {
            get
            {
                if (GameState.IsServer) throw new InvalidOperationException("Map rendering objects can only be accessed on the client side");
                return GameState.MapRenderer.MapFactionsByID[ID];
            }
        } 
        public int iLeaderCountry = -1;
        public GameCountry LeaderCountry
        {
            get
            {
                if (iLeaderCountry >= 0)
                    return ((TTGameState)GameState).GetEntity<GameCountry>(iLeaderCountry);
                else
                    throw new InvalidOperationException("Faction leader not set");
            }
            set
            {
                iLeaderCountry = value.ID;
            }
        }

        public List<int> FactionsAttacked = new List<int>();
        public List<int> NeutralCountriesAttacked = new List<int>();
        public bool HasTech(int iTech) => iTechs.Contains(iTech);

        public HashSet<int> iTechs = new ();
        private HashSet<int> tilesBlockaded = new HashSet<int>();
        private HashSet<int> tilesPartiallyBlockaded = new HashSet<int>();
        public int ProductionAvailable { get; set; } = 0;
        public int CommandsAvailable { get; set; } = 0;
        public int CommandInitiative { get; set; } = 0;
        public int Resources { get; private set; } // Derived value
        public int Population { get; private set; } // Derived value
        public SupplyStatus[] TileSupplyStatus { get; private set; } // Derived value
        public TradeStatus[] TileTradeStatus { get; private set; } // Derived value
        public TradeStatus[] PredictedTileTradeStatus { get; private set; } // Derived value
        public int[] iCapitals; // Derived value - main capital plus sub capitals
        public int Industry { get; set; } = 0;
        public int FactoriesNeededForIndustryUpgrade { get; set; } = 5;
        public int AttackedFactoryCostBonus { get; set; } = 1;
        public int Production => NumFactionsAtWarWith() > 0 ? Mathf.Min(Industry, Population, Resources) : Mathf.Min(Industry, Population);
        public int TotalPips { get; private set; } // Derived value calculated on the serverside
        public float AveragePipsPerCadre { get; private set; } // Derived value

        public int CalculateTotalCadres()
        {
            return GetCadres().Count;
        }

        public void RecalculateControlledCapitals()
        {
            List<int> capitals = new List<int>();
            foreach (var gameTile in GameState.GetEntitiesOfType<GameTile>())
            {
                if (gameTile.CitySize >= 4 &&
                    gameTile.Country is not null &&
                    gameTile.Country.Faction == this)
                {
                    capitals.Add(gameTile.ID);
                }
            }
            this.iCapitals = capitals.ToArray();
        }

        public void RunSummerBlockade()
        {
            tilesBlockaded.Clear();
            tilesPartiallyBlockaded.Clear();
            
            HashSet<int> iTilesWithFullTradeAccess = new HashSet<int>(((TTGameState)GameState).CalculateSupplyAccessibleTiles(
                iStartingTile: LeaderCountry.iCapital,
                supplyType: SupplyType.Trade,
                iFaction: ID,
                allowHornOfAfrica: false));
            HashSet<int> iTilesWithColonialTradeAccess = new HashSet<int> (((TTGameState)GameState).CalculateSupplyAccessibleTiles(
                iStartingTile: LeaderCountry.iCapital,
                supplyType: SupplyType.Trade,
                iFaction: ID,
                allowHornOfAfrica: true));

            GameTile[] tiles = GameState.GetEntitiesOfType<GameTile>();
            for (int iTile = 0; iTile < tiles.Length; iTile++)
            {
                if (!iTilesWithColonialTradeAccess.Contains(iTile)) tilesBlockaded.Add(iTile);
                else if (!iTilesWithFullTradeAccess.Contains(iTile)) tilesPartiallyBlockaded.Add(iTile);
            }
            
            RecalculateDerivedValues();
            PushFullState();
        }

        public void RecalculateSupply()
        {
            GameTile[] tiles = GameState.GetEntitiesOfType<GameTile>();
            TileSupplyStatus = new SupplyStatus[tiles.Length];
            TileTradeStatus = new TradeStatus[tiles.Length];
            PredictedTileTradeStatus = new TradeStatus[tiles.Length];
            
            HashSet<int> tilesInSupply = new HashSet<int>();
            foreach (var iCapital in iCapitals)
            {
                int[] iTilesInSupply = ((TTGameState)GameState).CalculateSupplyAccessibleTiles(
                    iStartingTile: iCapital,
                    supplyType: SupplyType.Supply,
                    iFaction: ID,
                    allowHornOfAfrica: true);
                foreach (var iSuppliedTile in iTilesInSupply)
                {
                    tilesInSupply.Add(iSuppliedTile);
                }
            }
            for (int iTile = 0; iTile < tiles.Length; iTile++)
            {
                TileSupplyStatus[iTile] = tilesInSupply.Contains(iTile) ? SupplyStatus.InSupply : SupplyStatus.NotInSupply;
            }
            
            HashSet<int> iTilesWithFullTradeAccess = new HashSet<int>(((TTGameState)GameState).CalculateSupplyAccessibleTiles(
                iStartingTile: LeaderCountry.iCapital,
                supplyType: SupplyType.Trade,
                iFaction: ID,
                allowHornOfAfrica: false));
            HashSet<int> iTilesWithColonialTradeAccess = new HashSet<int> (((TTGameState)GameState).CalculateSupplyAccessibleTiles(
                iStartingTile: LeaderCountry.iCapital,
                supplyType: SupplyType.Trade,
                iFaction: ID,
                allowHornOfAfrica: true));


            for (int iTile = 0; iTile < tiles.Length; iTile++)
            {
                GameTile tile = tiles[iTile];
                if (tilesBlockaded.Contains(iTile)) 
                    TileTradeStatus[iTile] = TradeStatus.FullyBlockaded;
                else if (tilesPartiallyBlockaded.Contains(iTile)) 
                    TileTradeStatus[iTile] = TradeStatus.Blockaded_ColonialAccessible;
                else 
                    TileTradeStatus[iTile] = TradeStatus.FullyAccessible;
                
                if (iTilesWithFullTradeAccess.Contains(iTile))
                    PredictedTileTradeStatus[iTile] = TradeStatus.FullyAccessible;
                else if (iTilesWithColonialTradeAccess.Contains(iTile))
                    PredictedTileTradeStatus[iTile] = TradeStatus.Blockaded_ColonialAccessible;
                else PredictedTileTradeStatus[iTile] = TradeStatus.FullyBlockaded;
            }
        }

        public override void RecalculateDerivedValues()
        {
            Resources = 0;
            Population = 0;
            RecalculateControlledCapitals(); // Capitals should be rechecked before calculating supply
            RecalculateSupply();
            GameTile[] tiles = GameState.GetEntitiesOfType<GameTile>();
            for (int iTile = 0; iTile < tiles.Length; iTile++)
            {
                GameTile tile = tiles[iTile];
                if (tile.Active && tile.Occupier?.AssociatedFaction == this)
                {
                    if (TileTradeStatus[iTile] == TradeStatus.FullyAccessible)
                    {
                        Resources += tile.Resources + tile.ColonialResources;
                    }
                    else if (TileTradeStatus[iTile] == TradeStatus.Blockaded_ColonialAccessible)
                    {
                        Resources += tile.ColonialResources;
                    }
                    Population += tile.Population;
                }
            }
        }

        public void RecalculateTotalPipCount(bool push = false)
        {
            TotalPips = 0;
            AveragePipsPerCadre = 0;
            int totalCadres = 0;
            foreach (var cadre in GameState.GetEntitiesOfType<GameCadre>())
            {
                if (cadre != null && cadre.Active && cadre.Faction == this)
                {
                    TotalPips += cadre.Pips;
                    totalCadres++;
                }
            }

            AveragePipsPerCadre = (float)TotalPips / (float)totalCadres;

            if (push)
            {
                foreach (int iPlayer in GameState.Players)
                {
                    DataStreamWriter message = StartCustomUpdate(_totalPipsUpdateHeader, iPlayer);
                    message.WriteInt(TotalPips);
                    message.WriteFloat(AveragePipsPerCadre);
                    PushCustomUpdate(iPlayer, ref message);
                }
            }
        }
        
        public ICollection<GameCadre> GetCadres ()
        {
            GameCadre[] allCadres = GameState.GetEntitiesOfType<GameCadre>();
            return allCadres.Where(cadre => cadre.Country.iFaction == ID).ToList();
        }

        public bool IsAtWarWithCountry(int iCountry)
        {
            GameCountry country = GameState.GetEntity<GameCountry>(iCountry);
            if (country.IsNeutral)
            {
                return NeutralCountriesAttacked.Contains(iCountry);
            }
            else
            {
                GameFaction faction = country.Faction;
                if (faction == this) return false;
                if (FactionsAttacked.Contains(faction.ID)) return true;
                if (faction.FactionsAttacked.Contains(this.ID)) return true;
                return false;
            }
        }

        public bool IsAtWarWithFaction(int iFaction) => FactionsAttacked.Contains(iFaction) || GameState.GetEntity<GameFaction>(iFaction).FactionsAttacked.Contains(this.ID);
        public bool IsAtWarWithFaction(GameFaction faction) => FactionsAttacked.Contains(faction.ID) || faction.FactionsAttacked.Contains(this.ID);

        public int NumFactionsAtWarWith()
        {
            int numFactionsAtWarWith = 0;
            foreach (var gameFaction in GameState.GetEntitiesOfType<GameFaction>())
            {
                if (gameFaction.ID == ID) continue;
                if (IsAtWarWithFaction(gameFaction)) numFactionsAtWarWith++;
            }

            return numFactionsAtWarWith;
        }
        
        public HashSet<FactionAttribute> attributes = new ();
        public (int iTile, int iCountry, int startingCadres)[] startingUnits;

        public override void RefreshMapState()
        {
            GameState.NetworkMember.NetworkingLog($"Applying Map State for {Name}");
            if (MapFaction is null) MapFaction.Create(name: Name, map: GameState.MapRenderer, id: ID, IdeologyName);
            else
            {
                MapFaction.name = Name;
            }
            foreach (GameCountry country in GameState.GetEntitiesOfType<GameCountry>())
            {
                if (country.iFaction == ID)
                {
                    GameState.MapRenderer.MapCountriesByID[country.ID].SetFaction(MapFaction.ID, country.MembershipStatus);
                }
            }

            MapFaction.gameObject.name = Name;
            MapFaction.leader = GameState.MapRenderer.MapCountriesByID[iLeaderCountry];
            CopyStartingUnitsTo(MapFaction);
        }
        
        public void CopyStartingUnitsTo(MapFaction mapFaction)
        {
            mapFaction.startingUnits.Clear();
            foreach ((int iTile, int iCountry, int startingCadres) in startingUnits) // We pass along starting units onto the map renderer side so the UI can access them
            {
                string mapTile = GameState.MapRenderer.MapTilesByID[iTile].name;
                string mapCountry = GameState.MapRenderer.MapCountriesByID[iCountry].name;
                mapFaction.startingUnits.Add(new StartingUnitInfo(mapTile, mapCountry, startingCadres));
            }
        }

        protected override void OnDeactivatedClientside()
        {
            throw new NotSupportedException("Did you mean to deactivate a game faction?");
        }

        protected override void ReceiveCustomUpdate(ref DataStreamReader incomingMessage, byte header)
        {
            switch (header)
            {
                case _totalPipsUpdateHeader:
                    TotalPips = incomingMessage.ReadInt();
                    AveragePipsPerCadre = incomingMessage.ReadFloat();
                    break;
            }
        }

        protected override void ReceiveFullState(ref DataStreamReader incomingMessage)
        {
            Name = incomingMessage.ReadFixedString64().ToString();
            IdeologyName = incomingMessage.ReadFixedString64().ToString();
            CommandsAvailable = incomingMessage.ReadShort();
            CommandInitiative = incomingMessage.ReadShort();
            iLeaderCountry = incomingMessage.ReadShort();
            Industry = incomingMessage.ReadShort();
            ProductionAvailable = incomingMessage.ReadShort();
            FactoriesNeededForIndustryUpgrade = incomingMessage.ReadByte();
            AttackedFactoryCostBonus = incomingMessage.ReadByte();
            TotalPips = incomingMessage.ReadInt();
            
            ushort techsLength = incomingMessage.ReadUShort();
            iTechs.Clear();
            for (int i = 0; i < techsLength; i++)
            {
                iTechs.Add(incomingMessage.ReadUShort());
            }
            
            ushort blockadedTilesLength = incomingMessage.ReadUShort();
            tilesBlockaded.Clear();
            for (int i = 0; i < blockadedTilesLength; i++)
            {
                tilesBlockaded.Add(incomingMessage.ReadUShort());
            }
            
            ushort colonialBlockadedTilesLength = incomingMessage.ReadUShort();
            tilesPartiallyBlockaded.Clear();
            for (int i = 0; i < colonialBlockadedTilesLength; i++)
            {
                tilesPartiallyBlockaded.Add(incomingMessage.ReadUShort());
            }

            ushort startingUnitsLength = incomingMessage.ReadUShort();
            startingUnits = new (int iTile, int iCountry, int startingCadres)[startingUnitsLength];
            for (int i = 0; i < startingUnitsLength; i++)
            {
                int iTile = (int)incomingMessage.ReadShort();
                int iCountry = (int)incomingMessage.ReadShort();
                int startingCadres = (int)incomingMessage.ReadUShort();
                startingUnits[i] = (iTile, iCountry, startingCadres);
            }

            ushort factionsAttackedLength = incomingMessage.ReadUShort();
            FactionsAttacked.Clear();
            for (int i = 0; i < factionsAttackedLength; i++)
            {
                FactionsAttacked.Add(incomingMessage.ReadShort());
            }
            
            ushort neutralCountriesAttackedLength = incomingMessage.ReadUShort();
            NeutralCountriesAttacked.Clear();
            for (int i = 0; i < neutralCountriesAttackedLength; i++)
            {
                NeutralCountriesAttacked.Add(incomingMessage.ReadShort());
            }

            // Updates to the map
            if (GameState.NetworkMember.GameStarted) RefreshMapState();
        }

        protected override void WriteFullState(int targetPlayer, ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteFixedString64(Name);
            outgoingMessage.WriteFixedString64(IdeologyName);
            outgoingMessage.WriteShort((short)CommandsAvailable);
            outgoingMessage.WriteShort((short)CommandInitiative);
            outgoingMessage.WriteShort((short)iLeaderCountry);
            outgoingMessage.WriteShort((short)Industry);
            outgoingMessage.WriteShort((short)ProductionAvailable);
            outgoingMessage.WriteByte((byte)FactoriesNeededForIndustryUpgrade);
            outgoingMessage.WriteByte((byte)AttackedFactoryCostBonus);
            outgoingMessage.WriteInt(TotalPips);
            
            outgoingMessage.WriteUShort((ushort)iTechs.Count);
            foreach (var tech in iTechs)
            {
                outgoingMessage.WriteUShort((ushort)tech);
            }

            outgoingMessage.WriteUShort((ushort)tilesBlockaded.Count);
            foreach (var iBlockadedTile in tilesBlockaded)
            {
                outgoingMessage.WriteUShort((ushort)iBlockadedTile);
            }
            
            outgoingMessage.WriteUShort((ushort)tilesPartiallyBlockaded.Count);
            foreach (var iBlockadedTile in tilesPartiallyBlockaded)
            {
                outgoingMessage.WriteUShort((ushort)iBlockadedTile);
            }

            outgoingMessage.WriteUShort((ushort)startingUnits.Length);
            for (int i = 0; i < startingUnits.Length; i++)
            {
                outgoingMessage.WriteShort((short)startingUnits[i].iTile);
                outgoingMessage.WriteShort((short)startingUnits[i].iCountry);
                outgoingMessage.WriteUShort((ushort)startingUnits[i].startingCadres);
            }

            outgoingMessage.WriteUShort((ushort)FactionsAttacked.Count);
            for (int i = 0; i < FactionsAttacked.Count; i++)
            {
                outgoingMessage.WriteShort((short)FactionsAttacked[i]);
            }
            
            outgoingMessage.WriteUShort((ushort)NeutralCountriesAttacked.Count);
            for (int i = 0; i < NeutralCountriesAttacked.Count; i++)
            {
                outgoingMessage.WriteShort((short)NeutralCountriesAttacked[i]);
            }
        }

        public override int HashFullState(int asPlayer)
        {
            int hash = Hashing.MurmurHash3_Combine(CommandsAvailable, iLeaderCountry, ProductionAvailable, Industry, FactoriesNeededForIndustryUpgrade);
            unchecked
            {
                foreach (var tech in iTechs)
                {
                    hash += Hashing.MurmurHash3(tech);
                }
                foreach (var iTile in tilesBlockaded)
                {
                    hash += Hashing.MurmurHash3(iTile);
                }
                foreach (var iTile in tilesPartiallyBlockaded)
                {
                    hash += Hashing.MurmurHash3(iTile);
                }
                for (int i = 0; i < startingUnits.Length; i++)
                {
                    hash = Hashing.CombineHashes(
                        hash,
                        Hashing.MurmurHash3_Combine(
                            startingUnits[i].iTile,
                            startingUnits[i].iCountry,
                            startingUnits[i].startingCadres
                        ));
                }
            }

            return hash;
        }

        public void DeclareWarOnCountry(int iCountry)
        {
            GameCountry targetCountry = GameState.GetEntity<GameCountry>(iCountry);
            NeutralCountriesAttacked.Add(iCountry);
            foreach (var gameFaction in GameState.GetEntitiesOfType<GameFaction>())
            {
                if (gameFaction is not null && gameFaction != this)
                {
                    int targetLargestCitySize = targetCountry.CalculateLargestCitySize();
                    for (int i = 0; i < targetLargestCitySize; i++)
                    {
                        gameFaction.DrawCard(CardType.Action);
                    }
                }
            }
        }
        
        public void DeclareWarOnFaction(int iFaction)
        {
            GameFaction targetFaction = GameState.GetEntity<GameFaction>(iFaction);
            FactionsAttacked.Add(iFaction);
            targetFaction.FactoriesNeededForIndustryUpgrade -= targetFaction.AttackedFactoryCostBonus;
        }

        public void DrawCard(CardType cardType)
        {
            int cardToDraw;
            switch (cardType)
            {
                case CardType.Action:
                    List<ActionCard> actionDeck = GameCard.GetCardsInDeck<ActionCard>(GameState);
                    cardToDraw = UnityEngine.Random.Range(minInclusive:0, maxExclusive:actionDeck.Count);
                    actionDeck[cardToDraw].HoldingPlayer = ID;
                    actionDeck[cardToDraw].PushFullState();
                    actionDeck.RemoveAt(cardToDraw);
                    break;
                case CardType.Investment:
                    List<InvestmentCard> investmentDeck = GameCard.GetCardsInDeck<InvestmentCard>(GameState);
                    cardToDraw = UnityEngine.Random.Range(minInclusive:0, maxExclusive:investmentDeck.Count);
                    investmentDeck[cardToDraw].HoldingPlayer = ID;
                    investmentDeck[cardToDraw].PushFullState();
                    investmentDeck.RemoveAt(cardToDraw);
                    break;
                default: throw new NotImplementedException();
            }

        }
    }
}