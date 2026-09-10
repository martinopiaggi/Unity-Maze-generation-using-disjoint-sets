using System;
using UnityEngine;

namespace DisjointSetMaze
{
    public enum WallDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class WallDirectionExtensions
    {
        public static WallDirection Opposite(this WallDirection direction)
        {
            switch (direction)
            {
                case WallDirection.North:
                    return WallDirection.South;
                case WallDirection.East:
                    return WallDirection.West;
                case WallDirection.South:
                    return WallDirection.North;
                case WallDirection.West:
                    return WallDirection.East;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }

    public sealed class Cell
    {
        private readonly GameObject[] _walls = new GameObject[4];
        private byte _openPassages;

        public int Index { get; }
        public Vector3 LocalPosition { get; }

        public Cell(int x, int z, int mazeSize, Vector3 localPosition)
        {
            Index = x * mazeSize + z;
            LocalPosition = localPosition;
        }

        public void SetWall(WallDirection direction, GameObject wall)
        {
            _walls[(int)direction] = wall;
        }

        public GameObject GetWall(WallDirection direction)
        {
            return _walls[(int)direction];
        }

        public void OpenPassage(WallDirection direction)
        {
            var directionIndex = (int)direction;
            _openPassages |= (byte)(1 << directionIndex);

            var wall = _walls[directionIndex];
            if (wall != null && wall.activeSelf)
            {
                wall.SetActive(false);
            }
        }

        public bool IsPassageOpen(WallDirection direction)
        {
            return (_openPassages & (1 << (int)direction)) != 0;
        }
    }
}
