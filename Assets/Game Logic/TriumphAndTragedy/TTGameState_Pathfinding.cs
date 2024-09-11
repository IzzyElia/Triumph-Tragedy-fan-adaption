using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;

namespace Game_Logic.TriumphAndTragedy
{
    public partial class TTGameState
    {
        private Dictionary<GameTile, int> c_openTiles = new ();
        private HashSet<GameTile> c_closedTiles = new ();
        private Dictionary<GameTile, int> c_newTiles = new ();


        int CountMoves(UnitType unitType, MoveType moveType)
        {
            switch (moveType)
            {
                case MoveType.Normal: return unitType.Movement;
                case MoveType.Redeployment: return unitType.Movement * 2;
                case MoveType.Rebasing: return unitType.Movement;
                case MoveType.Support: return unitType.SupportRange;
                default: throw new NotImplementedException();
            }
        }
        
        public int[] CalculateSupplyAccessibleTiles(int iStartingTile, SupplyType supplyType, int iFaction, bool allowHornOfAfrica)
        {
            Dictionary<GameTile, int> clearedTiles = new Dictionary<GameTile, int>();
            GameTile startingTile = GetEntity<GameTile>(iStartingTile);
            GameFaction forFaction = GetEntity<GameFaction>(iFaction);
            const int terrainSwitchesLimit = 2;
            lock (c_openTiles)
            {
                lock (c_closedTiles)
                {
                    c_closedTiles.Clear();
                    c_openTiles.Clear();
                    
                    c_openTiles.Add(startingTile, 0); // the number represents the number of land/sea switches
                    lock (c_newTiles)
                    {
                        c_newTiles.Clear();
                        int failsafe = 0;
                        while (c_openTiles.Count > 0)
                        {
                            failsafe++;
                            if (failsafe > 1000) throw new InvalidOperationException();
                            foreach ((GameTile tile, int terrainSwitches) in c_openTiles)
                            {
                                for (int iBorder = 0; iBorder < tile.ConnectedTiles.Length; iBorder++)
                                {
                                    int terrainSwitchesIfMovedTo = terrainSwitches;
                                    GameBorder border = tile.ConnectedBorders[iBorder];
                                    GameTile connectedTile = tile.ConnectedTiles[iBorder];
                                    if (border.BorderType == BorderType.HornOfAfrica && !allowHornOfAfrica) continue;
                                    if (connectedTile.TerrainType == TerrainType.NotInPlay) continue;
                                    if (c_closedTiles.Contains(connectedTile)) continue;
                                    
                                    if (
                                        (tile.TerrainType == TerrainType.Land && (connectedTile.TerrainType == TerrainType.Ocean || connectedTile.TerrainType == TerrainType.Sea))
                                        ||
                                        (tile.TerrainType == TerrainType.Sea || tile.TerrainType == TerrainType.Ocean) && connectedTile.TerrainType == TerrainType.Land)
                                    {
                                        terrainSwitchesIfMovedTo = terrainSwitches + 1;
                                    }
                                    if (terrainSwitchesIfMovedTo > terrainSwitchesLimit) 
                                        continue;
                                    
                                    
                                    if (connectedTile.TerrainType == TerrainType.Land)
                                    {
                                        GameCountry occupier = connectedTile.Occupier;
                                        if (occupier != null)
                                        {
                                            switch (supplyType)
                                            {
                                                case SupplyType.Supply:
                                                    if (occupier.Faction == null || occupier.Faction.ID != forFaction.ID)
                                                    {
                                                        c_closedTiles.Add(connectedTile);
                                                        continue;
                                                    }
                                                    break;
                                                case SupplyType.Trade:
                                                    if (occupier.Faction != null &&
                                                        occupier.Faction.IsAtWarWithFaction(forFaction))
                                                    {
                                                        continue;
                                                    }
                                                    break;
                                            }
                                        }
                                    }


                                    if (connectedTile.TerrainType == TerrainType.Sea)
                                    {
                                        bool interdiction = false;
                                        foreach (GameCadre cadreOnTile in connectedTile.GetCadresOnTile())
                                        {
                                            if (cadreOnTile.Faction is not null && cadreOnTile.Faction.IsAtWarWithFaction(forFaction))
                                            {
                                                interdiction = true;
                                                break;
                                            }
                                        }
                                        if (interdiction) continue;
                                    }
                                    
                                    // If another move reaches this tile but our move would let us get there in fewer moves, use the path that uses the least moves
                                    if (clearedTiles.TryGetValue(connectedTile, out int clearedTileTerrainSwitches))
                                    {
                                        if (terrainSwitchesIfMovedTo >= clearedTileTerrainSwitches)
                                        {
                                            continue;
                                        }
                                    }
                                    if (c_newTiles.TryGetValue(connectedTile, out int newTileTerrainSwitches))
                                    {
                                        c_newTiles[connectedTile] = Math.Min(terrainSwitchesIfMovedTo, newTileTerrainSwitches);
                                    }
                                    else
                                    {
                                        c_newTiles.Add(connectedTile, newTileTerrainSwitches);
                                    }
                                }
                            }

                            foreach ((GameTile tile, int terrainSwitches) in c_openTiles)
                            {
                                if (clearedTiles.Keys.Contains(tile)) 
                                    clearedTiles[tile] = Math.Min(clearedTiles[tile], terrainSwitches);
                                else 
                                    clearedTiles.Add(tile, terrainSwitches);
                            }
                            c_openTiles.Clear();
                            foreach ((GameTile tile, int terrainSwitches) in c_newTiles)
                            {
                                c_openTiles.Add(tile, terrainSwitches);
                            }
                            c_newTiles.Clear();
                        }
                    }

                    int[] results = new int[c_closedTiles.Count + clearedTiles.Count];
                    int j = 0;
                    foreach (var tile in c_closedTiles)
                    {
                        results[j] = tile.ID;
                        j++;
                    }
                    foreach (var tile in clearedTiles.Keys)
                    {
                        // TODO Remove this debugging check once you're sure this all work
                        if (c_closedTiles.Contains(tile))
                            Debug.LogError($"{tile.Name} marked as both cleared and closed");
                        
                        results[j] = tile.ID;
                        j++;
                    }

                    return results;
                }
            }
        }

        public int[] CalculateAccessibleTiles(int iCadre, MoveType moveType, int from = -1,
            UnitType calculatingAsType = null)
        {
            GameCadre cadre = GetEntity<GameCadre>(iCadre);
            if (calculatingAsType == null) calculatingAsType = cadre.UnitType;
            if (calculatingAsType == null) return new int[0];
            GameFaction faction = cadre.Faction;
            UnitType unitType = cadre.UnitType;
            int iStartingTile = from == -1 ? cadre.Tile.ID : from;
            return CalculateAccessibleTiles(unitType: calculatingAsType, unitFaction: faction, moveType: moveType,
                from: iStartingTile);
        }
        /// <summary>
        /// Returns all the tiles accessible to <param name="iCadre"></param> in a move
        /// </summary>
        public int[] CalculateAccessibleTiles(UnitType unitType, GameFaction unitFaction, MoveType moveType, int from)
        {
            GameTile startingTile = GetEntity<GameTile>(from);
            if (unitType.Category == UnitCategory.Ground && startingTile.TerrainType == TerrainType.Sea ||
                startingTile.TerrainType == TerrainType.Ocean)
                unitType = Ruleset.SeaTransportUnitType;
            lock (c_openTiles)
            {
                lock (c_closedTiles)
                {
                    c_closedTiles.Clear();
                    c_openTiles.Clear();
                    int moves = CountMoves(unitType, moveType);
                    
                    c_openTiles.Add(startingTile, moves);
                    lock (c_newTiles)
                    {
                        c_newTiles.Clear();
                        int failsafe = 0;
                        while (c_openTiles.Count > 0)
                        {
                            failsafe++;
                            if (failsafe > 1000) throw new InvalidOperationException();
                            foreach ((GameTile tile, int remainingMoves) in c_openTiles)
                            {
                                if (remainingMoves <= 0) continue;
                                foreach (var connectedTile in tile.ConnectedTiles)
                                {
                                    // TODO Why don't we start with the closedtiles.contains check?
                                    int remainingMovesIfMovedTo = remainingMoves - 1;
                                    if (connectedTile.TerrainType == TerrainType.NotInPlay) continue;
                                    
                                    // Check that the unit can move onto the tile type (sea can only move to sea or coastal)
                                    switch (unitType.Category)
                                    {
                                        case UnitCategory.Ground:
                                            //
                                            if (connectedTile.TerrainType == TerrainType.Sea || connectedTile.TerrainType == TerrainType.Ocean) continue;
                                            //
                                            if (startingTile.TerrainType == TerrainType.Land &&
                                                (connectedTile.TerrainType == TerrainType.Sea ||
                                                 connectedTile.TerrainType == TerrainType.Ocean))
                                                remainingMovesIfMovedTo = 0;
                                            break;
                                        case UnitCategory.Sea:
                                            if (tile.TerrainType == TerrainType.Land && !tile.IsCoastal) continue;
                                            if (tile.IsCoastal) remainingMovesIfMovedTo = 0;
                                            break;
                                        case UnitCategory.Sub:
                                            if (tile.TerrainType == TerrainType.Land && !tile.IsCoastal) continue;
                                            if (tile.IsCoastal) remainingMovesIfMovedTo = 0;
                                            break;
                                    }
                                    
                                    // If the unit must rebase, it can only move to friendly tiles
                                    if (moveType == MoveType.Rebasing && unitType.RebaseRule == RebaseRule.MustRebase && (connectedTile.Occupier == null || connectedTile.Occupier.Faction != unitFaction))
                                        continue;
                                    
                                    if (connectedTile.TerrainType == TerrainType.Ocean)
                                    {
                                        remainingMovesIfMovedTo = remainingMoves - 2;
                                    }

                                    if (connectedTile.Occupier != null 
                                            && 
                                        connectedTile.Occupier.Faction != unitFaction
                                            &&
                                        !(unitFaction == null || 
                                          unitFaction.IsAtWarWithCountry(connectedTile.Occupier.ID) ||
                                          (!IsServer && UIController.DiploPanel.ConsideringWarWith(connectedTile.Occupier.ID)))) continue;
                                    
                                    if (remainingMovesIfMovedTo < 0) continue;
                                    foreach (GameCadre cadreOnTile in connectedTile.GetCadresOnTile())
                                    {
                                        if (cadreOnTile.Faction != unitFaction)
                                        {
                                            remainingMovesIfMovedTo = 0; // Will start combat
                                        }
                                    }

                                    
                                    if (!c_closedTiles.Contains(connectedTile))
                                    {
                                        // If another move reaches this tile but our move would let us get there in fewer moves, use the path that uses the least moves
                                        if (c_newTiles.TryGetValue(connectedTile, out int connectedTileRemainingMoves))
                                        {
                                            c_newTiles[connectedTile] = Math.Max(remainingMovesIfMovedTo,
                                                connectedTileRemainingMoves);
                                        }
                                        else
                                        {
                                            c_newTiles.Add(connectedTile, remainingMovesIfMovedTo);
                                        }
                                    }
                                }
                            }

                            foreach (GameTile tile in c_openTiles.Keys)
                            {
                                c_closedTiles.Add(tile);
                            }
                            c_openTiles.Clear();
                            foreach ((GameTile tile, int remainingMoves) in c_newTiles)
                            {
                                c_openTiles.Add(tile, remainingMoves);
                            }
                            c_newTiles.Clear();
                        }
                    }

                    int[] results = new int[c_closedTiles.Count];
                    int j = 0;
                    foreach (var tile in c_closedTiles)
                    {
                        results[j] = tile.ID;
                        j++;
                    }

                    return results;
                }
            }
        }

        public int[] CalculateAccessibleTilesAdjecentTo(int iCadre, int iTile, MoveType moveType, int from = -1, UnitType calculatingAsType = null)
        {
            ICollection<GameTile> accessibleTiles = GetEntities<GameTile>(CalculateAccessibleTiles(iCadre, moveType, from));
            return accessibleTiles.Where(t => t.ConnectedTileIDs.Contains(iTile)).Select(t => t.ID).ToArray();
        }
        
        
        
        /*
        /// <summary>
        /// Must have set moves.UnitMoves. This method then calculates moves.ValidMoves, and the method returns false if no valid path combination could be found
        /// </summary>
        /// <param name="moves"></param>
        /// <returns></returns>
        public bool TryFindPaths(UnitPathCollection moves)
        {
            Dictionary<(int, int), int> borderCrossings = new Dictionary<(int, int), int>();
            bool allPathsFound = true;

            foreach (var unitMove in moves.UnitMoves)
            {
                List<List<GameTile>> paths = new List<List<GameTile>>();
                GameTile start = GetEntity<GameTile>(unitMove.iStart);
                GameTile end = GetEntity<GameTile>(unitMove.iEnd);
                GameCadre cadre = GetEntity<GameCadre>(unitMove.iCadre);

                // Clear previous pathfinding data
                c_currentPath.Clear();
                c_visited.Clear();
                FindPathsDFS(start, end, cadre, c_currentPath, paths, c_visited);
                
                if (paths.Count == 0)
                {
                    allPathsFound = false;
                    break;
                }
            }

            if (allPathsFound)
            {
                moves.ValidPaths = new UnitPath[moves.UnitMoves.Count];
                for (int i = 0; i < moves.ValidPaths.Length; i++)
                {
                    
                }
            }

            return allPathsFound;
        }


        Stack<GameTile> c_currentPath = new Stack<GameTile>();
        HashSet<int> c_visited = new HashSet<int>();
        private List<List<GameTile>> FindAllPaths(GameTile start, GameTile end, GameCadre cadre)
        {
            List<List<GameTile>> paths = new List<List<GameTile>>();
            lock (c_currentPath)
            {
                lock (c_visited)
                {
                    c_currentPath.Clear();
                    c_visited.Clear();
                    FindPathsDFS(start, end, cadre, c_currentPath, paths, c_visited);
                }
            }
            return paths;
        }
        private static void FindPathsDFS(GameTile current, GameTile end, GameCadre cadre, Stack<GameTile> currentPath, List<List<GameTile>> paths, HashSet<int> visited)
        {
            // Add the current tile to the path
            currentPath.Push(current);
            visited.Add(current.ID);

            if (current.ID == end.ID)
            {
                // If current tile is the end, save the current path
                paths.Add(new List<GameTile>(currentPath));
            }
            else if (currentPath.Count - 1 < cadre.UnitType.Movement)
            {
                // Continue to search only if we have not exceeded the move limit
                foreach (GameTile next in current.ConnectedTiles)
                {
                    if (!visited.Contains(next.ID))
                    {
                        FindPathsDFS(next, end, cadre, currentPath, paths, visited);
                    }
                }
            }

            // Backtrack
            currentPath.Pop();
            visited.Remove(current.ID);
        }
        */
    }
}