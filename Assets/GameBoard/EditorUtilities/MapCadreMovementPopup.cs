using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    public class MapCadreMovementPopup : PopupWindowContent
    {
        private MapCadre _mapCadre;
        private GameObject[] _validTargets;
        private Vector2 scrollPosition;
        private BorderType _selectedBorderType;
        public MapCadreMovementPopup(MapCadre cadre)
        {
            this._mapCadre = cadre;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 200);
        }

        public override void OnOpen()
        {
            List<GameObject> validTargets = new List<GameObject>();
            Map map = _mapCadre.Map;
            if (map == null) return;
            
            
            foreach (MapTile tile in map.countriesWrapper.GetComponentsInChildren<MapTile>())
            {
                if (tile != _mapCadre.Tile)
                    validTargets.Add(tile.gameObject);
            }
        
            validTargets.Sort((a, b) =>
            {
                Vector3 position = _mapCadre.transform.position;
                return Vector3.Distance(
                        a.transform.position, position
                    )
                    .CompareTo(
                        Vector3.Distance(b.transform.position, position)
                    );
            });
        
            this._validTargets = validTargets.ToArray();
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Select a tile to test a move to", EditorStyles.boldLabel);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
            foreach (GameObject obj in _validTargets)
            {
                if (GUILayout.Button(obj.name))
                {
                    MapTile target = obj.GetComponent<MapTile>();
                    _mapCadre.Tile = target;
                    _mapCadre.SetPositionUnanimated(_mapCadre.ChoosePosition(target));
                    editorWindow.Close();
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
