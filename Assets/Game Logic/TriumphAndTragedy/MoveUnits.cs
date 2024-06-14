using System;
using System.Linq;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using Izzy;
using Izzy.ForcedInitialization;
using Unity.Collections;

namespace Game_Logic.TriumphAndTragedy
{
    [ForceInitialize]
    public class MoveUnits : PlayerAction
    {
        static MoveUnits()
        {
            PlayerAction.RegisterPlayerActionType<MoveUnits>();
        }

        private (int cadre, int[] path)[] _moves = Array.Empty<(int, int[])>();

        public MoveUnits() {}

        public override void Execute()
        {
            for (int i = 0; i < _moves.Length; i++)
            {
                (int iCadre, int[] path) = _moves[i];
                int iDestination = path[^1];
                GameCadre cadre = GameState.GetEntity<GameCadre>(iCadre);
                cadre.PushMove(path);
            }

            GameState.GamePhase = GamePhase.Combat;
            GameState.PushGlobalFields();
        }

        public (bool, string) AreMovesValid((int cadre, int[] path)[] movesToTest)
        {
            if (GameState.GamePhase != GamePhase.GiveCommands) return (false, "Not in command phase");
            if (GameState.ActivePlayer != iPlayerFaction) return (false, "Not the active player)");
            
            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayerFaction);
            if (playerFaction == null)
                return (false, "Nonexistent player faction attempting action");
            if (movesToTest.Length > playerFaction.CommandsAvailable) return (false, "Not enough commands available");
            
            int[] movesOverBorder = new int[GameState.NumBorders];
            for (int i = 0; i < movesToTest.Length; i++)
            {
                (int iCadre, int[] path) = movesToTest[i];
                GameCadre cadre = GameState.GetEntity<GameCadre>(iCadre);
                GameFaction cadreFaction = cadre.Faction;
                if (cadreFaction.ID != iPlayerFaction) return (false, $"Unit does not belong to {cadreFaction.Name}");
                for (int j = 0; j < path.Length; j++)
                {
                    int iTile = path[j];
                    int iPrevTile = j == 0 ? cadre.iTile : path[j-1];
                    GameTile tile = GameState.GetEntity<GameTile>(iTile);
                    GameTile prevTile = GameState.GetEntity<GameTile>(iPrevTile);
                    GameBorder border = GameState.BorderOfTiles[new UnorderedPair<int>(iTile, iPrevTile)];
                    int iBorder = border.ID;
                    if (cadre.UnitType.Category == UnitCategory.Ground)
                    {
                        // Check movement limits
                        int maxMoves = int.MaxValue;
                        bool movementLimted =
                            (border.BorderType == BorderType.Coast && tile.TerrainType == MapTile.TerrainType.Land &&
                             tile.Occupier != cadreFaction)
                            ||
                            (tile.GetCadresOnTile().Any(x => x.Faction != cadre.Faction));
                        if (movementLimted)
                        {
                            switch (border.BorderType)
                            {
                                case BorderType.Impassable:
                                    maxMoves = 0;
                                    break;
                                case BorderType.Plains:
                                    maxMoves = 3;
                                    break;
                                case BorderType.Forest:
                                    maxMoves = 2;
                                    break;
                                case BorderType.River:
                                    maxMoves = 2;
                                    break;
                                case BorderType.Mountain:
                                    maxMoves = 1;
                                    break;
                                case BorderType.Coast:
                                    if (playerFaction.HasTech(Tech.LSTs))
                                        maxMoves = 2;
                                    else
                                        maxMoves = 1;
                                    break;
                                case BorderType.Strait:
                                    if (playerFaction.HasTech(Tech.LSTs))
                                        maxMoves = 2;
                                    else
                                        maxMoves = 1;
                                    break;
                                case BorderType.Unspecified:
                                    maxMoves = 0;
                                    break;
                            }
                            movesOverBorder[iBorder]++;
                        }

                        if (movesOverBorder[iBorder] > maxMoves)
                        {
                            return (false, $"Too many moves over the {border.MapBorder.name} border");
                        }
                    }
                }
            }

            return (true, string.Empty);
        }

        /// <param name="parameters">
        /// Parameter format is a cadres movement and paths - (int iCadre, int[] path)
        /// </param>
        public override (bool, string) TestParameter(params object[] parameters)
        {
            (int cadre, int[] path) move = ((int)parameters[0], (int[])parameters[1]);
            (int cadre, int[] path)[] movesToTest = new (int cadre, int[] path)[this._moves.Length + 1];
            for (int i = 0; i < this._moves.Length; i++)
            {
                movesToTest[i] = this._moves[i];
            }
            movesToTest[^1] = move;
            return AreMovesValid(movesToTest);
        }

        /// <param name="parameters">
        /// Parameter format is a cadres and its movement path - (int iCadre, int[] path)[]
        /// </param>
        public override void AddParameter(params object[] parameters)
        {
            (int cadre, int[] path) move = ((int)parameters[0], (int[])parameters[1]);
            (int cadre, int[] path)[] updatedMoves = new (int cadre, int[] path)[this._moves.Length + 1];
            for (int i = 0; i < this._moves.Length; i++)
            {
                updatedMoves[i] = this._moves[i];
            }
            _moves = updatedMoves;
        }

        /// <param name="parameters">
        /// Parameters format is an array of cadres and movement paths - (int iCadre, int[] addedMove)[]
        /// </param>
        public override void SetAllParameters(params object[] parameters)
        {
            this._moves = parameters[0] as (int cadre, int[] path)[];
        }
        
        public override object[] GetParameters()
        {
            return new object[] { _moves };
        }

        public override (bool, string) Validate() => AreMovesValid(this._moves);
        
        public override void Reset()
        {
            _moves = Array.Empty<(int cadre, int[] path)>();
        }

        public override void Recreate(ref DataStreamReader incomingMessage)
        {
            int length = incomingMessage.ReadInt();
            this._moves = new (int, int[])[length];

            for (int i = 0; i < length; i++)
            {
                int cadre = incomingMessage.ReadInt();
                int pathLength = incomingMessage.ReadInt();
                int[] path = new int[pathLength];
                for (int j = 0; j < pathLength; j++)
                {
                    path[j] = incomingMessage.ReadInt();
                }
                _moves[i] = (cadre, path);
            } 
        }
        public override void Write(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(_moves.Length);
            for (int i = 0; i < _moves.Length; i++)
            {
                outgoingMessage.WriteInt(_moves[i].cadre);
                outgoingMessage.WriteInt(_moves[i].path.Length);
                for (int j = 0; j < _moves[i].path.Length; j++)
                {
                    outgoingMessage.WriteInt(_moves[i].path[j]);
                }
            }
        }
    }
}