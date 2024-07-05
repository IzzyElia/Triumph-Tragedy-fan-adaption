using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;

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
        /// <summary>
        /// Returns all the tiles accessible to <param name="iCadre"></param> in a move
        /// </summary>
        public int[] CalculateAccessibleTiles(int iCadre, MoveType moveType)
        {
            GameCadre cadre = GetEntity<GameCadre>(iCadre);
            if (cadre.UnitType == null) return new int[0];
            lock (c_openTiles)
            {
                lock (c_closedTiles)
                {
                    c_closedTiles.Clear();
                    c_openTiles.Clear();
                    int moves = CountMoves(cadre.UnitType, moveType);

                    c_openTiles.Add(cadre.Tile, moves);
                    lock (c_newTiles)
                    {
                        c_newTiles.Clear();
                        int failsafe = 0;
                        while (c_openTiles.Count > 0)
                        {
                            failsafe++;
                            if (failsafe > 1000) throw new InvalidOperationException("Failsafe triggered");
                            foreach ((GameTile tile, int remainingMoves) in c_openTiles)
                            {
                                if (remainingMoves <= 0) continue;
                                foreach (var connectedTile in tile.ConnectedTiles)
                                {
                                    int remainingMovesIfMovedTo = remainingMoves - 1;
                                    if (connectedTile.TerrainType == TerrainType.NotInPlay) continue;
                                    if (connectedTile.TerrainType == TerrainType.Ocean)
                                    {
                                        remainingMovesIfMovedTo = remainingMoves - 2;
                                    }
                                    if (remainingMovesIfMovedTo < 0) continue;
                                    foreach (GameCadre cadreOnTile in connectedTile.GetCadresOnTile())
                                    {
                                        if (cadreOnTile.Faction != cadre.Faction)
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

        public int[] CalculateAccessibleTilesAdjecentTo(int iCadre, int iTile, MoveType moveType)
        {
            ICollection<GameTile> accessibleTiles = GetEntities<GameTile>(CalculateAccessibleTiles(iCadre, moveType));
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