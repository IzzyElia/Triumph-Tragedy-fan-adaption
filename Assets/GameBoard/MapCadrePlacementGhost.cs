using System;
using GameBoard.UI.SpecializeComponents;
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
    public class CadrePlacementGhost : MapCadreCore
    {
        private static GameObject prefab;
        [NonSerialized] public UnitGhostPurpose Purpose;
        public static CadrePlacementGhost Create(string name, Map map, MapTile tile, MapCountry country, UnitType unitType, UnitGhostPurpose purpose)
        {
            if (prefab is null)
            {
                prefab = Resources.Load<GameObject>("Prefabs/CadreGhost");
                if (prefab is null) Debug.LogError("Failed to load cadre ghost prefab");
            }

            CadrePlacementGhost cadre = Instantiate(prefab).GetComponent<CadrePlacementGhost>();
            if (cadre is null) Debug.LogError($"No cadre ghost component attached to the cadre ghost prefab");
            cadre.Purpose = purpose;
            cadre.RegisterTo(map);
            cadre.transform.SetParent(map.transform);
            cadre._mapCountry = country;
            //cadre.ID = id;
            cadre._tile = tile;
            cadre._unitType = unitType;
            if (tile is not null)
            {
                cadre.transform.position = cadre.ChoosePosition(tile);
                cadre.transform.SetParent(tile.transform);
            }
            cadre.RecalculateAppearance();
            return cadre;
        }
    }
}