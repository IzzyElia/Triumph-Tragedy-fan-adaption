using System;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard
{
    public enum UnitGhostPurpose
    {
        Build,
        InitialPlacement,
        Held
    }
    public class MapCadrePlacementGhost : MapCadreCore
    {
        private static GameObject cachedPrefab;
        [NonSerialized] public UnitGhostPurpose Purpose;

        public static MapCadrePlacementGhost Create(string name, Map map, MapTile tile, MapCountry country,
            UnitType unitType, UnitGhostPurpose purpose) =>
            Create<MapCadrePlacementGhost>(name, map, tile, country, unitType, purpose);
        public static T Create<T>(string name, Map map, MapTile tile, MapCountry country, UnitType unitType, UnitGhostPurpose purpose, GameObject prefab = null) where T : MapCadrePlacementGhost
        {
            if (prefab is null)
            {
                if (cachedPrefab is null)
                {
                    cachedPrefab = Resources.Load<GameObject>("Prefabs/CadreGhost");
                    if (cachedPrefab is null) Debug.LogError("Failed to load cadre ghost prefab");
                }

                prefab = cachedPrefab;
            }

            T cadre = Instantiate(prefab).GetComponent<T>();
            if (cadre is null) Debug.LogError($"No cadre ghost component attached to the cadre ghost prefab");
            cadre.UseGhostMaterial = true;
            cadre.Purpose = purpose;
            cadre.RegisterTo(map);
            cadre.transform.SetParent(map.transform);
            cadre._mapCountry = country;
            //cadre.ID = id;
            cadre._tile = tile;
            cadre._unitType = unitType;
            if (tile is not null)
            {
                cadre.SetPositionUnanimated(cadre.ChoosePosition(tile));
                cadre.transform.SetParent(tile.transform);
            }

            cadre.MaxPips = 0; // Hides the pip display
            cadre.RecalculateAppearance();
            return cadre;
        }
    }
}