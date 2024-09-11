using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine.Tilemaps;
using Izzy;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Game_Logic.TriumphAndTragedy.AI
{
    public class ChamberlainBot : Bot
    {
        class General
        {
            public General(ChamberlainBot master)
            {
                this._master = master;
            }
            private ChamberlainBot _master;
            public int[] _objectives;
            public int[] _secondaryObjectives; // objectives to help with completing the main objective
            public int[] _assignedUnits;
            public GameTile[] Objectives => _master.GameState.GetEntities<GameTile>(_objectives);
            public GameCadre[] AssignedUnits => _master.GameState.GetEntities<GameCadre>(_assignedUnits);
            public bool OperationActive = false;
            
            public UnitAIScore RequestUnits()
            {
                for (int i = 0; i < _objectives.Length; i++)
                {
                    int iObjective = _objectives[i];
                    GameTile objective = _master.GameState.GetEntity<GameTile>(iObjective);
                    PowerScore enemyStrength = _master.CalculateThreat(iObjective);
                    float totalEnemyPower = enemyStrength.TotalAIScore.groundCombatScore +
                                            enemyStrength.TotalAIScore.seaCombatScore +
                                            enemyStrength.TotalAIScore.airCombatScore;
                    float enemyGroundPower = totalEnemyPower * enemyStrength.TotalAIScore.landCaptureFactor;
                    float groundPowerNeeded = enemyGroundPower * 1.3f;
                    if (objective.IsCoastal)
                    {
                        
                    }
                }
                throw new NotImplementedException();
            }
        }
        
        
        
        public override PlayerAction AI_InitialUnits(TTGameState gameState)
        {
            float[] tileValues = ScoreValueOfTiles();
            List<(int iTile, int iCountry, int startingCadres)> startingUnits =
                new List<(int iTile, int iCountry, int startingCadres)>(playedFaction.startingUnits);
            List<(int iTile, byte unitType, int iCountry, byte pips)> builds =
                new List<(int iTile, byte unitType, int iCountry, byte pips)>();
            HashSet<int> fortressesBuiltOn = new HashSet<int>();
            foreach ((int iTile, int iCountry, int startingCadres) startingUnit in IzzyUtility.Shuffle(startingUnits, new Random(DateTime.UtcNow.Ticks.GetHashCode())))
            {
                float tileValue = tileValues[startingUnit.iTile];
                GameTile tile = GameState.GetEntity<GameTile>(startingUnit.iTile);
                for (int i = 0; i < startingUnit.startingCadres; i++)
                {
                    float navelImportance = 0;
                    float groundImportance = 0;
                    foreach (var connectedTile in tile.ConnectedTiles)
                    {
                        float connectedTileValue = tileValues[connectedTile.ID];
                        if (connectedTile.TerrainType == TerrainType.Land)
                        {
                            groundImportance += connectedTileValue;
                        }
                        else if (connectedTile.TerrainType == TerrainType.Sea ||
                                 connectedTile.TerrainType == TerrainType.Ocean)
                        {
                            navelImportance += connectedTileValue;
                        }
                        else if (connectedTile.TerrainType == TerrainType.Strait)
                        {
                            groundImportance += connectedTileValue;
                            navelImportance += connectedTileValue;
                        }
                    }
                    List<(UnitType unitType, int pips)> startingUnitsOnTile = new List<(UnitType unitType, int pips)>();
                    for (int j = 0; j < builds.Count; j++)
                    {
                        if (builds[j].iTile == startingUnit.iTile)
                        {
                            startingUnitsOnTile.Add((gameState.Ruleset.unitTypes[builds[j].unitType], builds[j].pips));
                        }
                    }
                    (UnitType unitType, float score, PowerScore powerScore) bestOption = (null, 0, new PowerScore());
                    foreach (var unitType in GameState.Ruleset.unitTypes)
                    {
                        if (!unitType.IsBuildableThroughNormalPlacementRules) continue;
                        if ((unitType.Category == UnitCategory.Sea || unitType.Category == UnitCategory.Sub) &&
                            !(tile.IsCoastal || tile.TerrainType == TerrainType.Sea || tile.TerrainType == TerrainType.Ocean))
                            continue;
                        if (unitType.IsFortress && fortressesBuiltOn.Contains(startingUnit.iTile)) continue;
                        (UnitType unitType, int pips)[] concat =
                            new (UnitType unitType, int pips)[startingUnitsOnTile.Count + 1];
                        startingUnitsOnTile.CopyTo(concat, 0);
                        concat[^1] = (unitType, 1);
                        PowerScore defense = CalculateDefence(iTile: startingUnit.iTile, potentiallyIntroducedUnits: concat);

                        float score = ((defense.TotalAIScore.groundCombatScore * groundImportance) +
                                       (defense.TotalAIScore.seaCombatScore * navelImportance) +
                                       (defense.TotalAIScore.landCaptureFactor * 3f))
                                      *
                                      (defense.TotalAIScore.mobilityFactor + 1);
                        
                        if (score > bestOption.score) bestOption = (unitType, score, defense);
                        Debug.Log($"BOT: Evaluated {unitType.Name} to give {gameState.GetEntity<GameTile>(startingUnit.iTile).Name} a total score of {score}");
                    }

                    if (bestOption.unitType == null) throw new BotException("Could not pick a unit type");
                    builds.Add((startingUnit.iTile, (byte)bestOption.unitType.IdAndInitiative, startingUnit.iCountry, 1));
                    if (bestOption.unitType.IsFortress) fortressesBuiltOn.Add(startingUnit.iTile);
                    groundImportance -= bestOption.powerScore.TotalAIScore.groundCombatScore;
                    navelImportance -= bestOption.powerScore.TotalAIScore.seaCombatScore;
                }
            }

            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "InitialUnitsAction", iPlayerFaction);;
            action.SetAllParameters(builds.ToArray());
            return action;
        }

        public override PlayerAction AI_Production(TTGameState gameState)
        {
            //throw new System.NotImplementedException();
            PlayerAction action = PlayerAction.GenerateClientsidePlayerActionByName(gameState, "ProductionAction", iPlayerFaction);;
            return action;
        }

        public override PlayerAction AI_Cardplay(TTGameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public override PlayerAction AI_SelectCommandsCards(TTGameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public override PlayerAction AI_Commands(TTGameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public override PlayerAction AI_SelectCombats(TTGameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public override PlayerAction AI_SelectSupports(TTGameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public override PlayerAction AI_SelectNextCombat(TTGameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public override PlayerAction AI_CombatDecision(TTGameState gameState)
        {
            throw new System.NotImplementedException();
        }

        public override void Rebuild(TTGameState gameState)
        {
            RecalculateThreatPotentials();
        }

        // Utility functions
        private float aggression = 1;
        private float conservatism = 1;
        float[] ScoreValueOfTiles()
        {
            GameTile[] tiles = GameState.GetEntitiesOfType<GameTile>();
            float[] currentPass = new float[tiles.Length];
            float[] importanceByTileID = new float[tiles.Length];
            float popResourceRatio = playedFaction.Resources > 0 ? (float)playedFaction.Population / (float)playedFaction.Resources : float.PositiveInfinity;
            float resourcePopRatio = playedFaction.Population > 0 ? (float)playedFaction.Resources / (float)playedFaction.Population : float.PositiveInfinity;

            bool lostCapital = false;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].CitySize >= 4 && 
                    tiles[i].iStartingFaction == iPlayerFaction &&
                    tiles[i].Occupier.iFaction != iPlayerFaction)
                    lostCapital = true;
            }
            
            // Start with capitals
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].CitySize != 5) continue;
                
                // Our own main capitals are mission critical. Rival main capitals are highly valuable but not crucial
                if (tiles[i].iStartingFaction == iPlayerFaction) importanceByTileID[i] = 5 * (conservatism + 1);
                else currentPass[i] = 1 * (aggression + 1);
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].CitySize != 4) continue;
                
                // Our own subcapitals are mission critical only if one has been lost already
                if (tiles[i].iStartingFaction == iPlayerFaction)
                {
                    if (lostCapital) importanceByTileID[i] = 25 * (conservatism + 1);
                    else importanceByTileID[i] = 3 * (conservatism + 1);
                }
                else
                {
                    importanceByTileID[i] = 1 * (aggression + 1);
                }
            }

            for (int i = 0; i < importanceByTileID.Length; i++) importanceByTileID[i] += currentPass[i];
            currentPass = new float[importanceByTileID.Length];
            
            // Base tile value pass (except capitals
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].CitySize >= 4) continue;

                float importance = 0;
                importance += tiles[i].Population * resourcePopRatio;
                importance += tiles[i].Resources * popResourceRatio;
                if (tiles[i].Occupier?.Faction == playedFaction) importance *= conservatism + 1;
                else importance *= aggression + 1;

                importanceByTileID[i] = importance;
            }
            for (int i = 0; i < importanceByTileID.Length; i++) importanceByTileID[i] += currentPass[i];
            currentPass = new float[importanceByTileID.Length];
            
            // Adjacency pass
            const int numAdjecencyPasses = 2;
            for (int pass = 0; pass < numAdjecencyPasses; pass++)
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    foreach (var adjacentTile in tiles[i].ConnectedTiles)
                    {
                        currentPass[i] = Mathf.Max(currentPass[i], importanceByTileID[adjacentTile.ID] / 2f);
                    }
                }
                for (int i = 0; i < importanceByTileID.Length; i++) importanceByTileID[i] += currentPass[i];
                currentPass = new float[importanceByTileID.Length];
            }
            
            return importanceByTileID;
        }
        
        /// <summary>
        /// Calculates the raw defensive power on the tile
        /// </summary>
        /// <returns></returns>
        PowerScore CalculateDefence(int iTile, int iPotentiallyMovedCadre = -1, int iPotentiallyMovedToTile = -1, (UnitType unitType, int pips)[] potentiallyIntroducedUnits = null)
        {
            List<PowerScore> cadrePowerScores = new List<PowerScore>();
            foreach (var gameCadre in GameState.CadresByTileID.Get(iTile))
            {
                if (gameCadre.Faction.ID != playedFaction.ID) continue;
                if (gameCadre.ID == iPotentiallyMovedCadre && iPotentiallyMovedToTile != gameCadre.Tile.ID) continue;
                PowerScore cadrePower = new PowerScore(gameCadre);
                cadrePowerScores.Add(cadrePower);
            }

            if (potentiallyIntroducedUnits is not null)
            {
                for (int i = 0; i < potentiallyIntroducedUnits.Length; i++)
                {
                    cadrePowerScores.Add(new PowerScore(potentiallyIntroducedUnits[i].unitType, potentiallyIntroducedUnits[i].pips, GameState));

                }
            }

            PowerScore combinedPowerScore = new PowerScore();
            foreach (var cadrePowerScore in cadrePowerScores)
            {
                combinedPowerScore += cadrePowerScore;
            }

            return combinedPowerScore;
        }

        PowerScore CalculateEnemyDefence(int iTile)
        {
            PowerScore sumDefenderScore = new PowerScore();
            foreach (var gameCadre in GameState.GetEntity<GameTile>(iTile).GetCadresOnTile())
            {
                if (gameCadre.Faction == playedFaction) continue;
                if (gameCadre.UnitType == UnitType.Unknown)
                {
                    sumDefenderScore += new PowerScore(threatAverages[0], pips:gameCadre.Faction.AveragePipsPerCadre, GameState);
                }
            }

            return sumDefenderScore;
        }
        
        /// <summary>
        /// Calculates the strength while multiplying each units power by the number of tiles that unit could potentially access
        /// </summary>
        /// <returns></returns>
        PowerScore CalculatePowerProjection(int iTile, int iPotentiallyMovedCadre = -1, int iPotentiallyMovedToTile = -1, (UnitType unitType, int pips)[] potentiallyIntroducedUnits = null, params UnitCategory[] categories)
        {
            List<PowerScore> cadrePowerScores = new List<PowerScore>();
            foreach (var gameCadre in GameState.CadresByTileID.Get(iTile))
            {
                if (categories.Length > 0)
                {
                    if (!categories.Contains(gameCadre.UnitType.Category)) continue;
                }
                if (gameCadre.Faction.ID != playedFaction.ID) continue;
                if (gameCadre.ID == iPotentiallyMovedCadre && iPotentiallyMovedToTile != gameCadre.Tile.ID) continue;
                int numAccessibleTiles = GameState.CalculateAccessibleTiles(gameCadre.ID, MoveType.Normal).Length + 1;
                PowerScore cadrePower = new PowerScore(gameCadre);
                cadrePower *= Mathf.Log(numAccessibleTiles, 2);
                cadrePowerScores.Add(cadrePower);
            }

            if (potentiallyIntroducedUnits is not null)
            {
                for (int i = 0; i < potentiallyIntroducedUnits.Length; i++)
                {
                    
                    int numAccessibleTiles = GameState.CalculateAccessibleTiles(unitType:potentiallyIntroducedUnits[i].unitType, unitFaction:playedFaction, moveType:MoveType.Normal, from:iTile).Length + 1;
                    cadrePowerScores.Add(new PowerScore(potentiallyIntroducedUnits[i].unitType, potentiallyIntroducedUnits[i].pips, GameState) * numAccessibleTiles);
                }
            }

            PowerScore combinedPowerScore = new PowerScore();
            foreach (var cadrePowerScore in cadrePowerScores)
            {
                combinedPowerScore += cadrePowerScore;
            }

            return combinedPowerScore;
        }

        /// <summary>
        /// Returns the potential strength that we could move to the tile, if all available units were moves
        /// </summary>
        PowerScore CalculatePotentialStrength(int iTile, int iPotentiallyMovedCadre = -1, int potentiallyMovedToTile = -1)
        {
            List<PowerScore> potentialDefenderPowerScores = new List<PowerScore>();
            foreach (var gameCadre in GameState.GetEntitiesOfType<GameCadre>())
            {
                if (gameCadre is null || !gameCadre.Active) continue;
                if (gameCadre.Faction != playedFaction) continue;
                int[] accessibleTiles = 
                    (gameCadre.ID == iPotentiallyMovedCadre && potentiallyMovedToTile != gameCadre.Tile.ID) ?
                        GameState.CalculateAccessibleTiles(gameCadre.ID, MoveType.Redeployment, from: potentiallyMovedToTile) : GameState.CalculateAccessibleTiles(gameCadre.ID, MoveType.Redeployment);
                if (accessibleTiles.Contains(iTile))
                {
                    potentialDefenderPowerScores.Add(new PowerScore(gameCadre));
                }
            }
            
            PowerScore combinedPowerScore = new PowerScore();
            foreach (var cadrePowerScore in potentialDefenderPowerScores)
            {
                combinedPowerScore += cadrePowerScore;
            }

            return combinedPowerScore;
        }
        
        /// <summary>
        /// A cached list of the worst-case-scenario at each movement distance
        /// </summary>
        private UnitType[] threatPotentials = null;
        private UnitType[] threatAverages = null;
        void RecalculateThreatPotentials()
        {
            // find the unit type with the highest movement. Each number of moves is associated with its own stat set for threat calculations
            // ie the potential threat posed by a powerful unit with a movement potential of 1 tile shouldn't be considered when dealing with an unknown unit 3 tiles away
            int highestMovement = 0;
            foreach (var unitType in GameState.Ruleset.unitTypes)
            {
                highestMovement = Math.Max(highestMovement, unitType.Movement);
            }
            
            // Each location in the array represents the potential power of all unit types up to the associated movement score
            threatPotentials = new UnitType[highestMovement];
            threatAverages = new UnitType[highestMovement];
            for (int i = 0; i < highestMovement; i++) 
            {
                threatPotentials[i] = new UnitType($"Threat potential at movement {i}");
                threatPotentials[i].IdAndInitiative = GameState.Ruleset.unitTypes.Length;
                threatPotentials[i].Category = UnitCategory.Unspecified;
                threatPotentials[i].RebaseRule = RebaseRule.Unspecified;
                threatPotentials[i].FireAndRetreatRule = FireAndRetreatRule.Unspecified;
                threatPotentials[i].HasFirstFire = true;
                threatPotentials[i].TakesDoubleHits = false;
                threatPotentials[i].IsFortress = false;
                threatPotentials[i].IsBuildableThroughNormalPlacementRules = false;
                threatPotentials[i].IsTransportType = false;
                threatPotentials[i].GraphicsSets = null;
                threatPotentials[i].TechModifiers = null;
                
                threatAverages[i] = new UnitType($"Threat average at movement {i}");
                threatAverages[i].IdAndInitiative = GameState.Ruleset.unitTypes.Length;
                threatAverages[i].Category = UnitCategory.Unspecified;
                threatAverages[i].RebaseRule = RebaseRule.Unspecified;
                threatAverages[i].FireAndRetreatRule = FireAndRetreatRule.Unspecified;
                threatAverages[i].HasFirstFire = true;
                threatAverages[i].TakesDoubleHits = false;
                threatAverages[i].IsFortress = false;
                threatAverages[i].IsBuildableThroughNormalPlacementRules = false;
                threatAverages[i].IsTransportType = false;
                threatAverages[i].GraphicsSets = null;
                threatAverages[i].TechModifiers = null;
            }
            
            foreach (var unitType in GameState.Ruleset.unitTypes)
            {
                for (int i = 0; i < unitType.Movement; i++)
                {
                    threatPotentials[i].IdAndInitiative = Math.Min(threatPotentials[i].IdAndInitiative, unitType.IdAndInitiative);
                    threatPotentials[i].Movement = i;
                    threatPotentials[i].AirAttack = Math.Max(threatPotentials[i].AirAttack, unitType.AirAttack);
                    threatPotentials[i].GroundAttack = Math.Max(threatPotentials[i].GroundAttack, unitType.GroundAttack);
                    threatPotentials[i].SeaAttack = Math.Max(threatPotentials[i].SeaAttack, unitType.SeaAttack);
                    threatPotentials[i].SubAttack = Math.Max(threatPotentials[i].SubAttack, unitType.SubAttack);
                    threatPotentials[i].SupportRange = Math.Max(threatPotentials[i].SupportRange, unitType.SupportRange);
                    threatPotentials[i].RedeploymentMovement = Math.Max(threatPotentials[i].RedeploymentMovement, unitType.RedeploymentMovement);
                    threatPotentials[i].AIValues = threatPotentials[i].AIValues.MaxOf(unitType.AIValues);

                    threatAverages[i].IdAndInitiative = Mathf.CeilToInt((threatAverages[i].IdAndInitiative + unitType.IdAndInitiative) / 2f);
                    threatAverages[i].Movement = i;
                    threatAverages[i].AirAttack = Mathf.CeilToInt((threatAverages[i].AirAttack + unitType.AirAttack) / 2f);
                    threatAverages[i].GroundAttack = Mathf.CeilToInt((threatAverages[i].GroundAttack + unitType.GroundAttack) / 2f);
                    threatAverages[i].SeaAttack = Mathf.CeilToInt((threatAverages[i].SeaAttack + unitType.SeaAttack) / 2f);
                    threatAverages[i].SubAttack = Mathf.CeilToInt((threatAverages[i].SubAttack + unitType.SubAttack) / 2f);
                    threatAverages[i].SupportRange = Mathf.CeilToInt((threatAverages[i].SupportRange + unitType.SupportRange) / 2f);
                    threatAverages[i].RedeploymentMovement = Mathf.CeilToInt((threatAverages[i].RedeploymentMovement + unitType.RedeploymentMovement) / 2f);
                    threatAverages[i].AIValues = threatAverages[i].AIValues.AverageOf(unitType.AIValues);
                }
            }
        }

        PowerScore CalculateThreat(int iTile)
        {
            int[] factionCadreTotals = new int[GameState.PlayerCount];
            for (int i = 0; i < factionCadreTotals.Length; i++) factionCadreTotals[i] = GameState.GetEntity<GameFaction>(i).CalculateTotalCadres();
            
            List<PowerScore> potentialAttackerPowerScores = new List<PowerScore>();
            foreach (var gameCadre in GameState.GetEntitiesOfType<GameCadre>())
            {
                if (gameCadre is null || !gameCadre.Active) continue;
                if (gameCadre.Faction == playedFaction) continue;
                if (gameCadre.Faction == null) continue; // is neutral
                UnitType threatPotential = null;
                for (int i = 0; i < threatPotentials.Length; i++)
                {
                    if (i == 0 && iTile != gameCadre.iTile) continue;
                    if (i == 1 && !gameCadre.Tile.ConnectedTileIDs.Contains(iTile)) continue;
                    int[] accessibleTiles = GameState.CalculateAccessibleTiles(gameCadre.ID, MoveType.Normal, calculatingAsType:threatPotentials[i]);
                    if (accessibleTiles.Contains(iTile))
                    {
                        threatPotential = threatPotentials[i];
                    }
                }

                if (threatPotential != null)
                {
                    int totalFactionPips = gameCadre.Faction.TotalPips;
                    if (totalFactionPips <= 0) totalFactionPips = 1;
                    potentialAttackerPowerScores.Add(new PowerScore(threatPotential, (float)totalFactionPips / (float)factionCadreTotals[gameCadre.Faction.ID], GameState));
                }
            }
            
            PowerScore combinedPowerScore = new PowerScore();
            foreach (var cadrePowerScore in potentialAttackerPowerScores)
            {
                combinedPowerScore += cadrePowerScore;
            }

            return combinedPowerScore;
        }
        
        struct PowerScore
        {
            public PowerScore(GameCadre gameCadre) : this(gameCadre.UnitType, gameCadre.Pips, gameCadre.GameState)
            {
            }
            public PowerScore(UnitType unitType, float pips, GameState gameState)
            {
                TotalAIScore = unitType.AIValues;
                CadresRepresented = 1;
                PipsRepresented = pips;
            }
            
            public UnitAIScore TotalAIScore;
            public int CadresRepresented;
            public float PipsRepresented;
            
            
            public static PowerScore operator +(PowerScore a, PowerScore b)
            {
                return new PowerScore
                {
                    TotalAIScore = a.TotalAIScore + b.TotalAIScore,
                    CadresRepresented = a.CadresRepresented + b.CadresRepresented,
                    PipsRepresented = a.PipsRepresented + b.PipsRepresented,
                };
            }
            
            public static PowerScore operator *(PowerScore a, float num)
            {
                return new PowerScore
                {
                    TotalAIScore = a.TotalAIScore * num,
                    CadresRepresented = a.CadresRepresented,
                    PipsRepresented = a.PipsRepresented
                };
            }
        }
        
        
        
        // Bot boilerplate ----------------------------------------------------
        public ChamberlainBot(TTGameState gameState) : base(gameState)
        {
            if (gameState is null) throw new ArgumentException();
        }
    }
}