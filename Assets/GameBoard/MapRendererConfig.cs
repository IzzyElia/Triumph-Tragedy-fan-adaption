using UnityEngine;

namespace GameBoard
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/MapRendererConfig", order = 1)]
    public class MapRendererConfig : ScriptableObject
    {
        public Map mapPrefab;
    }
}