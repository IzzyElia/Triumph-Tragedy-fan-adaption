using System.Collections.Generic;
using GameBoard.MapMarkers;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard
{
    public interface IUIController
    {
        public GameObject UIObjectAtPointer { get; }
        public GameObject UICardUnderPointer { get; }
        public GameObject UICardRegionUnderPointer { get; }
        public GameObject UICardPlayPanelUnderPointer { get; }
        public GameObject UICardEffectUnderPointer { get; }
        public CombatMarker CombatMarkerSelectedForSupport { get; }
        public Dictionary<int, CombatOption> SupportUnitSelections { get; }
        public MapTile HoveredOverTile { get; }
        public bool PointerIsOverUI { get; }
        public Vector3 PointerPositionInWorld { get; }
        public Vector3 PointerPositionOnScreen { get; }
        public InputStatus PointerInputStatus { get; }
        public InputStatus ModifierInputStatus { get; }
        public float TimeSincePointerPressed { get; }
        public Vector3 PointerPressedAtScreenPosition { get; }
        public float DeltaDragSincePointerPressed { get; }
        public HashSet<int> MovementHighlights { get; }
        public List<MapObject> ScrubQueue { get; }
    }
}