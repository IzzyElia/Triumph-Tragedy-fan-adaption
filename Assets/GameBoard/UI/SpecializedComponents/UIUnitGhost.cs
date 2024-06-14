using System;
using UnityEngine;

namespace GameBoard.UI.SpecializeComponents
{
    public class UIUnitMover : UIComponent
    {
        [NonSerialized] public MapCadre MoveTarget;
        public int iMoveTarget => MoveTarget.ID;
        public int[] iMovementPath;
        public int[] path;
        public CadrePlacementGhost ghost;

        public override void UIUpdate()
        {
            ghost.transform.position = new Vector3(
                UIController.PointerPositionInWorld.x, 
                UIController.PointerPositionInWorld.y,
                z:-0.2f);
        
            int iCurrentHoveredTile = UIController.HoveredOverTile.ID;
            int iPreviousValidHoveredTile = path[^1];
            
            if (UIController.PointerInputStatus == PointerInputStatus.Releasing)
            {
                UIController.MovementAction.AddParameter((iMoveTarget, path));
            }
            else if (iCurrentHoveredTile != iPreviousValidHoveredTile)
            {
                int[] pathWithNewTile = new int[path.Length + 1];
                Array.Copy(path, pathWithNewTile, path.Length);
                pathWithNewTile[path.Length] = iCurrentHoveredTile;
                (bool newTileValid, string reason) =
                    UIController.MovementAction.TestParameter((iMoveTarget, pathWithNewTile));
                if (newTileValid) path = pathWithNewTile;
                else Debug.Log(reason);
            }
        }

        public override void OnGamestateChanged()
        {
        }

        public override void OnResyncEnded()
        {
        }
    }
}