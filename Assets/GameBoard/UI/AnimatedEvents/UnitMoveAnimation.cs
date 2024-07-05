using System;
using UnityEngine;

namespace GameBoard.UI.AnimatedEvents
{
    public class UnitMoveAnimation : AnimatedEvent
    {
        private MapCadre[] _cadres;
        
        public UnitMoveAnimation(UIController uiController, int iTile, params int[] iCadres) : base(uiController, callback:null, simultaneous:false)
        {
            MapTile tile = MapRenderer.MapTilesByID[iTile];
            _cadres = new MapCadre[iCadres.Length];
            for (int i = 0; i < iCadres.Length; i++)
            {
                MapCadre cadre = MapRenderer.GetCadreByID(iCadres[i]);
                if (cadre.AnimatingMovement) Debug.LogError("Cadre already has ongoing movement animation");
                cadre.AnimatingMovement = true;
                cadre.Tile = tile;
                cadre.Destination = cadre.ChoosePosition(tile);
                this._cadres[i] = cadre;
            }

        }

        private float _momentum = 0f;
        private const float _maxMomentum = 0.05f;
        protected override AnimationState OnStep(float deltaTime)
        {
            if (_momentum < _maxMomentum)
            {
                _momentum = Mathf.Min(_momentum + 0.005f, _maxMomentum);
            }

            bool allCadresCloseEnough = true;
            for (int i = 0; i < _cadres.Length; i++)
            {
                MapCadre cadre = _cadres[i];
                cadre.transform.position = Vector3.Lerp(cadre.transform.position, cadre.Destination, _momentum);
                if (Vector3.Distance(cadre.transform.position, cadre.Destination) > 0.01f)
                    allCadresCloseEnough = false;
            }

            if (allCadresCloseEnough)
            {
                for (int i = 0; i < _cadres.Length; i++)
                {
                    _cadres[i].transform.position = _cadres[i].Destination;
                    _cadres[i].AnimatingMovement = false;
                }

                return AnimationState.Exit;
            }
            
            return AnimationState.Continue;
        }
    }
}