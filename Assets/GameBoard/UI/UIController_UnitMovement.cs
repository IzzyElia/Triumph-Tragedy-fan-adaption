using System.Collections.Generic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;

namespace GameBoard.UI
{
    public partial class UIController
    {
        public HashSet<int> MovementHighlights { get; private set; } = new HashSet<int>();
        public List<MapCadreMovementGhost> heldMovementGhosts = new List<MapCadreMovementGhost>();
        public List<MapCadreMovementGhost> pendingMovementGhosts = new List<MapCadreMovementGhost>();

        public void CleanupAfterMovement()
        {
            List<int> previousHighlightedTiles = new List<int>(MovementHighlights);
            MovementHighlights.Clear();
            foreach (int iTile in previousHighlightedTiles)
            {
                MapRenderer.MapTilesByID[iTile].RecalculateHighlighting();
            }

            foreach (var movementGhost in heldMovementGhosts)
            {
                MapRenderer.MapCadresByID[movementGhost.BaseCadre].Darken = false;
                MapRenderer.MapCadresByID[movementGhost.BaseCadre].RecalculateAppearance();
                movementGhost.DestroyMapObject();
            }
            heldMovementGhosts.Clear();
            
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
            int commandsUsed = (int)MovementAction.GetData()[0];
            int commandsAvailable = faction.CommandsAvailable;
            int commandsRemaining = commandsAvailable - commandsUsed;
            return (commandsAvailable, commandsRemaining);
        }
        private void MovementUpdate()
        {
            (int commandsAvailable, int commandsRemaining) = GetCommands();
            
            if (PointerInputStatus == InputStatus.Pressed)
            {
                if (heldMovementGhosts.Count > 0 && !(HoveredMapObject is MapCadre cadre && cadre.MapCountry.faction == PlayerMapFaction)
                    && MovementHighlights.Contains(HoveredOverTile.ID))
                {
                    foreach (var ghost in heldMovementGhosts)
                    {
                        pendingMovementGhosts.Add(ghost);
                        MovementActionData move = new MovementActionData(ghost.BaseCadre, HoveredOverTile.ID);
                        ghost.MovementAction = move;
                        MovementAction.AddParameter(move);
                        CommandsInfoWindow.Refresh();
                    }
                    
                    heldMovementGhosts.Clear();

                    List<MapObject> prevSelectedMapObjects = new List<MapObject>(SelectedMapObjects);
                    SelectedMapObjects.Clear();
                    foreach (var mapObject in prevSelectedMapObjects)
                    {
                        if (!mapObject.IsDestroyed)
                        {
                            mapObject.OnSelectionStatusChanged(SelectionStatus.Unselected);
                            SelectionChanged = true;
                        }
                    }
                }
                else if (HoveredMapObject is MapCadreMovementGhost movementGhost)
                {
                    if (pendingMovementGhosts.Remove(movementGhost))
                    {
                        MovementAction.RemoveParameter(movementGhost.MovementAction);
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

            if (HoveredOverTileChanged && heldMovementGhosts.Count > 0 && HoveredOverTile is not null)
            {
                if (MovementHighlights.Contains(HoveredOverTile.ID))
                {
                    foreach (var ghost in heldMovementGhosts)
                    {
                        ghost.gameObject.SetActive(true);
                        ghost.Tile = HoveredOverTile;
                    }
                }
                else
                {
                    foreach (var ghost in heldMovementGhosts)
                    {
                        ghost.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnSelectionChanged()
        {
            List<int> previousHighlightedTiles = new List<int>(MovementHighlights);
            List<int[]> movesets = new List<int[]>();
            foreach (var selectedMapObject in SelectedMapObjects)
            {
                if (selectedMapObject is MapCadre cadre)
                {
                    if (!cadre.Darken)
                    {
                        cadre.Darken = true;
                        cadre.RecalculateAppearance();
                    }
                    movesets.Add(GameState.CalculateAccessibleTiles(cadre.ID, false));
                }
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


            List<MapCadre> selectedCadres = new List<MapCadre>();
            foreach (var mapObject in SelectedMapObjects)
            {
                if (mapObject is MapCadre cadre) selectedCadres.Add(cadre);
            }
            for (int i = 0; i < selectedCadres.Count; i++)
            {
                MapCadre cadre = selectedCadres[i];
                if (i >= heldMovementGhosts.Count)
                {
                    MapCadreMovementGhost ghost = MapCadreMovementGhost.CreateMovementGhost("MovementGhost", MapRenderer, HoveredOverTile, cadre.MapCountry, cadre.UnitType, UnitGhostPurpose.Held, cadre.ID);
                    heldMovementGhosts.Add(ghost);
                }
                else if (heldMovementGhosts[i] is null)
                {
                    MapCadreMovementGhost ghost = MapCadreMovementGhost.CreateMovementGhost("MovementGhost", MapRenderer, HoveredOverTile, cadre.MapCountry, cadre.UnitType, UnitGhostPurpose.Held, cadre.ID);
                    heldMovementGhosts[i] = ghost;
                    MapRenderer.MapCadresByID[ghost.BaseCadre].Darken = true;
                    MapRenderer.MapCadresByID[ghost.BaseCadre].RecalculateAppearance();
                }
                else
                {
                    heldMovementGhosts[i].UnitType = cadre.UnitType;
                    heldMovementGhosts[i].MapCountry = cadre.MapCountry;
                }
            }

            if (selectedCadres.Count < heldMovementGhosts.Count)
            {
                for (int i = selectedCadres.Count; i < heldMovementGhosts.Count; i++)
                {
                    heldMovementGhosts[i].DestroyMapObject();
                }
            }
        }
    }
}