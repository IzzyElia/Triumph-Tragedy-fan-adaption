using System;
using System.Collections.Generic;
using System.Linq;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine.Serialization;

namespace GameBoard.MapMarkers
{
    public enum CombatMarkerStatus
    {
        Inactive,
        Active,
        SelectedForSupport,
    }
    public class CombatMarker : MapMarker
    {
        [NonSerialized] public CombatMarkerStatus Status = CombatMarkerStatus.Inactive;
        [NonSerialized] public CombatOption CombatOption;
        [NonSerialized] public HashSet<int> SupportOptions = new HashSet<int>();


        public void RecalculateSupportOptions()
        {
            SupportOptions.Clear();
            foreach (var mapCadre in Map.MapCadresByID)
            {
                if (mapCadre is null) continue;
                if (mapCadre.MapCountry.faction.ID != CombatOption.iDefender) continue;
                if (Map.GameState.CalculateAccessibleTiles(mapCadre.ID, MoveType.Support).Contains(CombatOption.iTile))
                {
                    SupportOptions.Add(mapCadre.ID);
                }
            }
        }
        public void SetStatus(CombatMarkerStatus status)
        {
            Status = status;
            RecalculateHighlightState();
        }
        
        protected override void RecalculateHighlightState()
        {
            switch (Status)
            {
                case CombatMarkerStatus.Inactive:
                    Darken = false;
                    Highlight = Highlightable && Map.HoveredMapObject == this;
                    break;
                case CombatMarkerStatus.Active:
                    Darken = true;
                    Highlight = Highlightable && Map.HoveredMapObject == this;
                    break;
                case CombatMarkerStatus.SelectedForSupport:
                    Darken = true;
                    Highlight = true;
                    break;
            }

        }
    }
}