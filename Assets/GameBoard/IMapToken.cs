using UnityEngine;

namespace GameBoard
{
    public interface IMapToken
    {
        public MapTile Tile { get; }
        public Vector3 TokenPosition { get; }
    }
}