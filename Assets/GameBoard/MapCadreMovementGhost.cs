using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard
{
    public class MapCadreMovementGhost : MapCadrePlacementGhost
    {
        private static GameObject cachedPrefab;
        public int BaseCadre = -1;
        public MovementActionData MovementAction;
        public static MapCadreMovementGhost CreateMovementGhost(string name, Map map, MapTile tile, MapCountry country, UnitType unitType, UnitGhostPurpose purpose, int baseCadre)
        {
            if (cachedPrefab is null)
            {
                cachedPrefab = Resources.Load<GameObject>("Prefabs/MovementCadreGhost");
                if (cachedPrefab is null) Debug.LogError("Failed to load cadre ghost prefab");
            }

            MapCadreMovementGhost ghost = MapCadrePlacementGhost.Create<MapCadreMovementGhost>(name, map, tile, country, unitType, purpose,
                cachedPrefab);
            ghost.BaseCadre = baseCadre;
            return ghost;
        }
    }
}