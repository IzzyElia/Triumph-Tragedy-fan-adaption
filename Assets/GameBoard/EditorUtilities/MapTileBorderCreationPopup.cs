using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    public class MapTileBorderCreationPopup : PopupWindowContent
    {
        private MapTile _mapTile;
        private GameObject[] _validTargets;
        private Vector2 scrollPosition;
        private BorderType _selectedBorderType;
        public MapTileBorderCreationPopup(MapTile mapTile)
        {
            this._mapTile = mapTile;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 200);
        }

        public override void OnOpen()
        {
            List<GameObject> validTargets = new List<GameObject>();
            Map map = _mapTile.Map;
            foreach (MapTile tile in map.countriesWrapper.GetComponentsInChildren<MapTile>())
            {
                if (tile != _mapTile && !_mapTile.connectedSpaces.Contains(tile))
                    validTargets.Add(tile.gameObject);
            }
        
            validTargets.Sort((a, b) => 
                Vector3.Distance(
                        a.transform.position, _mapTile.transform.position
                    )
                    .CompareTo(
                        Vector3.Distance(b.transform.position, _mapTile.transform.position)
                    )
            );
        
            this._validTargets = validTargets.ToArray();
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Select a tile to connect", EditorStyles.boldLabel);
            _selectedBorderType = (BorderType)EditorGUILayout.EnumPopup("Border Type", _selectedBorderType);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
            foreach (GameObject obj in _validTargets)
            {
                if (GUILayout.Button(obj.name))
                {
                    MapTile target = obj.GetComponent<MapTile>();
                    MapBorder newBorder = new GameObject(
                        $"{_mapTile.gameObject.name}-{obj.name}", 
                        typeof(MapBorder)
                    ).GetComponent<MapBorder>();
                    newBorder.borderType = _selectedBorderType;
                    newBorder.transform.position = (_mapTile.transform.position + target.transform.position) / 2;
                    newBorder.transform.SetParent(_mapTile.Map.mapBorderWrapper.transform);
                    _mapTile.Map.RecalculateMapConnections();
                    editorWindow.Close();
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
