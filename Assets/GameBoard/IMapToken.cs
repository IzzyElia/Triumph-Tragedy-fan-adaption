using UnityEngine;

namespace GameBoard
{
    public interface IMapToken
    {
        public MapTile Tile { get; }
        public Transform transform { get; }
    }
}