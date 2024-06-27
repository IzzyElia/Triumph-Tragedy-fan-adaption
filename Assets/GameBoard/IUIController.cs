using System.Collections.Generic;
using GameSharedInterfaces;
using UnityEngine;

namespace GameBoard
{
    public interface IUIController
    {
        GameObject UIObjectAtPointer { get; }
        GameObject UICardUnderPointer { get; }
        GameObject UICardRegionUnderPointer { get; }
        GameObject UICardPlayPanelUnderPointer { get; }
        GameObject UICardEffectUnderPointer { get; }
        MapTile HoveredOverTile { get; }
        bool PointerIsOverUI { get; }
        Vector3 PointerPositionInWorld { get; }
        Vector3 PointerPositionOnScreen { get; }
        InputStatus PointerInputStatus { get; }
        InputStatus ModifierInputStatus { get; }
        float TimeSincePointerPressed { get; }
        Vector3 PointerPressedAtScreenPosition { get; }
        float DeltaDragSincePointerPressed { get; }
        public HashSet<int> MovementHighlights { get; }
        public List<MapObject> ScrubQueue { get; }
    }
}