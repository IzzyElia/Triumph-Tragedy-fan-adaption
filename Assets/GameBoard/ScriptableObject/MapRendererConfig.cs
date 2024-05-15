using UnityEngine;

namespace GameBoard
{
    [CreateAssetMenu(fileName = "MapRendererConfig", menuName = "Custom/MapRendererConfig", order = 1)]
    public class MapRendererConfig : ScriptableObject
    {
        public GameObject mapPrefab;
    }
}