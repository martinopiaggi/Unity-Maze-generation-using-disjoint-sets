using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace DisjointSetMaze.Tests
{
    public sealed class MazeTopologyTests
    {
        private static readonly int[] Seeds = { int.MinValue, -12345, -1, 0, 1, 12345, int.MaxValue };
        private static readonly WallDirection[] Directions =
        {
            WallDirection.North, WallDirection.East, WallDirection.South, WallDirection.West
        };

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(224)]
        public void ActualGeneratorHelpersProduceDeterministicSpanningTrees(int size)
        {
            foreach (var seed in Seeds)
            {
                var cells = Carve(size, seed);
                var repeated = Carve(size, seed);
                var passageCount = 0;
                var independentSets = new DisjointSet(cells.Length);
                for (var index = 0; index < cells.Length; index++)
                {
                    foreach (var direction in Directions)
                    {
                        var open = cells[index].IsPassageOpen(direction);
                        Assert.That(repeated[index].IsPassageOpen(direction), Is.EqualTo(open));
                        var valid = TryNeighbour(index, direction, size, out var neighbour);
                        Assert.That(open && !valid, Is.False, "A perimeter wall must remain closed.");
                        if (!valid) continue;
                        Assert.That(open, Is.EqualTo(cells[neighbour].IsPassageOpen(direction.Opposite())));
                        if (!open || index > neighbour) continue;
                        Assert.That(independentSets.UnionSet(index, neighbour), Is.True, "A passage introduced a cycle.");
                        passageCount++;
                    }
                }

                Assert.That(passageCount, Is.EqualTo(cells.Length - 1));
                Assert.That(independentSets.ComponentCount, Is.EqualTo(1));
                AssertAllCellsReachable(cells, size);
            }
        }

        private static Cell[] Carve(int size, int seed)
        {
            var cells = new Cell[size * size];
            for (var index = 0; index < cells.Length; index++)
                cells[index] = new Cell(index / size, index % size, size, Vector3.zero);

            var edges = (IList)Helper("BuildInternalEdges").Invoke(null, new object[] { size });
            Assert.That(edges.Count, Is.EqualTo(2 * size * (size - 1)));
            var unique = new HashSet<long>();
            foreach (var edge in edges)
            {
                var first = EdgeIndex(edge, "CellIndex");
                var second = EdgeIndex(edge, "NeighbourIndex");
                Assert.That(first, Is.InRange(0, cells.Length - 1));
                Assert.That(second, Is.InRange(0, cells.Length - 1));
                var distance = Math.Abs(first / size - second / size) + Math.Abs(first % size - second % size);
                Assert.That(distance, Is.EqualTo(1));
                Assert.That(unique.Add((long)Math.Min(first, second) * cells.Length + Math.Max(first, second)), Is.True);
            }

            Helper("Shuffle").Invoke(null, new object[] { edges, new System.Random(seed) });
            var sets = new DisjointSet(cells.Length);
            var openPassage = Helper("OpenPassage");
            foreach (var edge in edges)
            {
                if (!sets.UnionSet(EdgeIndex(edge, "CellIndex"), EdgeIndex(edge, "NeighbourIndex"))) continue;
                openPassage.Invoke(null, new[] { (object)cells, edge });
                if (sets.ComponentCount == 1) break;
            }
            Assert.That(sets.ComponentCount, Is.EqualTo(1));
            return cells;
        }

        private static void AssertAllCellsReachable(Cell[] cells, int size)
        {
            var visited = new HashSet<int> { 0 };
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                foreach (var direction in Directions)
                {
                    if (!cells[index].IsPassageOpen(direction)) continue;
                    Assert.That(TryNeighbour(index, direction, size, out var neighbour), Is.True);
                    if (visited.Add(neighbour)) queue.Enqueue(neighbour);
                }
            }
            Assert.That(visited.Count, Is.EqualTo(cells.Length));
        }

        private static bool TryNeighbour(int index, WallDirection direction, int size, out int neighbour)
        {
            var args = new object[] { index, direction, size, -1 };
            var found = (bool)Helper("TryGetNeighbour").Invoke(null, args);
            neighbour = (int)args[3];
            if (found)
            {
                Assert.That(neighbour, Is.InRange(0, size * size - 1));
                Assert.That(Math.Abs(index / size - neighbour / size) + Math.Abs(index % size - neighbour % size), Is.EqualTo(1));
            }
            return found;
        }

        private static int EdgeIndex(object edge, string name)
        {
            return (int)edge.GetType().GetField(name).GetValue(edge);
        }

        private static MethodInfo Helper(string name)
        {
            return typeof(MazeGenerator).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(MazeGenerator).FullName, name);
        }
    }
}
