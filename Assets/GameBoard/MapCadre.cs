using System;
using System.Linq;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard
{
    [ExecuteAlways]
    public class MapCadre : MapCadreCore
    {
        private static GameObject prefab;

        public static MapCadre Create(string name, Map map, MapTile tile, MapCountry country, UnitType unitType, int id)
        {
            if (prefab is null)
            {
                prefab = Resources.Load<GameObject>("Prefabs/Cadre");
                if (prefab is null) Debug.LogError("Failed to load cadre prefab");
            }
            MapCadre cadre = Instantiate(prefab).GetComponent<MapCadre>();
            if (cadre is null) Debug.LogError($"No cadre component attached to the cadre prefab");
            cadre.UseGhostMaterial = false;
            cadre._mapCountry = country;
            cadre.ID = id;
            cadre.RegisterTo(map);
            cadre.transform.SetParent(map.transform);
            cadre.Tile = tile;
            cadre._unitType = unitType;
            cadre.transform.position = cadre.ChoosePosition(tile);
            cadre.RecalculateAppearance();
            return cadre;
        }
    }
}