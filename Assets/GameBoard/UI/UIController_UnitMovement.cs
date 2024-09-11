using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;

namespace GameBoard.UI
{
    // Unit movement manager
    public partial class UIController
    {
        public HashSet<int> MovementHighlights { get; private set; } = new HashSet<int>();
        public MapCadreMovementGhost heldMovementGhost = null;
        public List<MapCadreMovementGhost> pendingMovementGhosts = new List<MapCadreMovementGhost>();

        public void CleanupAfterMovement()
        {
            List<int> previousHighlightedTiles = new List<int>(MovementHighlights);
            MovementHighlights.Clear();
            foreach (int iTile in previousHighlightedTiles)
            {
                MapRenderer.MapTilesByID[iTile].RecalculateHighlighting();
            }

            if (heldMovementGhost is not null)
            {
                MapRenderer.MapCadresByID[heldMovementGhost.BaseCadre].Darken = false;
                MapRenderer.MapCadresByID[heldMovementGhost.BaseCadre].RecalculateAppearance();
                heldMovementGhost.DestroyMapObject();
                heldMovementGhost = null;
            }

            
            foreach (var movementGhost in pendingMovementGhosts)
            {
                MapRenderer.MapCadresByID[movementGhost.BaseCadre].Darken = false;
                MapRenderer.MapCadresByID[movementGhost.BaseCadre].RecalculateAppearance();
                movementGhost.DestroyMapObject();
            }
            pendingMovementGhosts.Clear();
            
            MovementAction.Reset();
        }

        public (int available, int remaining) GetCommands()
        {
            IGameFaction faction = GameState.GetFaction(iPlayer);
            int commandsUsed = pendingMovementGhosts.Count;
            int commandsAvailable = faction.CommandsAvailable;
            int commandsRemaining = commandsAvailable - commandsUsed;
            return (commandsAvailable, commandsRemaining);
        }
        private void MovementUpdate()
        {
            (int commandsAvailable, int commandsRemaining) = GetCommands();
            
            if (PointerInputStatus == InputStatus.Pressed)
            {
                if (heldMovementGhost is not null && HoveredOverTile is not null && MovementHighlights.Contains(HoveredOverTile.ID))
                {
                    if (heldMovementGhost.Tile == MapRenderer.MapCadresByID[heldMovementGhost.BaseCadre].Tile)
                    {
                        heldMovementGhost.DestroyMapObject();
                        heldMovementGhost = null;
                    }
                    else
                    {
                        pendingMovementGhosts.Add(heldMovementGhost);
                        MovementActionData move = new MovementActionData(isDiploAction:false, heldMovementGhost.BaseCadre, HoveredOverTile.ID);
                        heldMovementGhost.MovementAction = move;

                        CommandsInfoWindow.Refresh();

                        heldMovementGhost = null;
                    }
                    
                    /*
                    MapObject prevSelectedMapObject = SelectedMapObject;
                    SelectedMapObject = null;
                    if (prevSelectedMapObject is not null && !prevSelectedMapObject.IsDestroyed)
                    {
                        prevSelectedMapObject.OnSelectionStatusChanged(SelectionStatus.Unselected);
                        SelectionChanged = true;
                    }
                    */
                }
                else if (HoveredMapObject is MapCadreMovementGhost movementGhost)
                {
                    if (pendingMovementGhosts.Remove(movementGhost))
                    {
                        MapRenderer.MapCadresByID[movementGhost.BaseCadre].Darken = false;
                        MapRenderer.MapCadresByID[movementGhost.BaseCadre].RecalculateAppearance();
                        movementGhost.DestroyMapObject();
                        CommandsInfoWindow.Refresh();
                    }
                }
            }
            
            if (SelectionChanged && GameState.GamePhase == GamePhase.GiveCommands)
            {
                OnSelectionChanged();
            }

            if (HoveredOverTileChanged && heldMovementGhost is not null && HoveredOverTile is not null)
            {
                if (MovementHighlights.Contains(HoveredOverTile.ID))
                {
                    if (heldMovementGhost is not null)
                    {
                        heldMovementGhost.gameObject.SetActive(true);
                        heldMovementGhost.Tile = HoveredOverTile;
                    }
                }
                else
                {
                    if (heldMovementGhost is not null)
                    {
                        heldMovementGhost.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnSelectionChanged()
        {
            List<int> previousHighlightedTiles = new List<int>(MovementHighlights);
            List<int[]> movesets = new List<int[]>();
            if (SelectedMapObject is MapCadre selectedCadre)
            {
                if (!selectedCadre.Darken)
                {
                    selectedCadre.Darken = true;
                    selectedCadre.RecalculateAppearance();
                }
                movesets.Add(GameState.CalculateAccessibleTiles(selectedCadre.ID, MoveType.Normal));
            }

            if (movesets.Count > 0)
            {
                MovementHighlights = new HashSet<int>(movesets[0]);
                for (int i = 1; i < movesets.Count; i++)
                {
                    MovementHighlights.IntersectWith(movesets[i]);
                }
            }
            else
            {
                MovementHighlights.Clear();
            }
            
            HashSet<int> allTilesPotentiallyAffected = new HashSet<int>(previousHighlightedTiles);
            allTilesPotentiallyAffected.UnionWith(MovementHighlights);
            foreach (int iTile in allTilesPotentiallyAffected)
            {
                MapRenderer.MapTilesByID[iTile].RecalculateHighlighting();
            }

            
            if (SelectedMapObject is MapCadre cadre)
            {
                if (heldMovementGhost is null)
                {
                    MapCadreMovementGhost ghost = MapCadreMovementGhost.CreateMovementGhost("MovementGhost", MapRenderer, HoveredOverTile, cadre.MapCountry, cadre.UnitType, UnitGhostPurpose.Held, cadre.ID);
                    heldMovementGhost = ghost;
                    MapRenderer.MapCadresByID[ghost.BaseCadre].Darken = true;
                    MapRenderer.MapCadresByID[ghost.BaseCadre].RecalculateAppearance();
                }
                else
                {
                    heldMovementGhost.UnitType = cadre.UnitType;
                    heldMovementGhost.MapCountry = cadre.MapCountry;
                }
            }
            else if (heldMovementGhost is not null)
            {
                heldMovementGhost.DestroyMapObject();
                heldMovementGhost = null;
            }
        }
    }
}