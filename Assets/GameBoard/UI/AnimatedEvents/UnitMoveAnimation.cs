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
        private bool _loggedMissingReference = false;
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
                try
                {
                    if (cadre is null) continue;
                    cadre.transform.position = Vector3.Lerp(cadre.transform.position, cadre.Destination, _momentum);
                    if (Vector3.Distance(cadre.transform.position, cadre.Destination) > 0.01f)
                        allCadresCloseEnough = false;
                }
                catch (MissingReferenceException e)
                {
                    if (!_loggedMissingReference) Debug.LogWarning("Missing reference exception in unit animation");
                    _loggedMissingReference = true;
                    _cadres[i] = null;
                }
            }

            if (allCadresCloseEnough)
            {
                for (int i = 0; i < _cadres.Length; i++)
                {
                    try
                    {
                        if (_cadres[i] is null) continue;
                        _cadres[i].transform.position = _cadres[i].Destination;
                        _cadres[i].AnimatingMovement = false;
                    }
                    catch (MissingReferenceException e)
                    {
                        continue; // Missing reference warning logged above
                    }
                }

                return AnimationState.Exit;
            }
            
            return AnimationState.Continue;
        }
    }
}