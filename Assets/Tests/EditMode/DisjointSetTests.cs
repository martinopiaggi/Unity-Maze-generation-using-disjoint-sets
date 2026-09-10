using System;
using System.Reflection;
using NUnit.Framework;

namespace DisjointSetMaze.Tests
{
    public sealed class DisjointSetTests
    {
        [Test]
        public void ConstructorCreatesOneComponentPerElement()
        {
            var sets = new DisjointSet(8);

            Assert.That(sets.ComponentCount, Is.EqualTo(8));
            for (var element = 0; element < 8; element++)
            {
                Assert.That(sets.FindSet(element), Is.EqualTo(element));
            }
        }

        [Test]
        public void UnionMergesComponentsExactlyOnce()
        {
            var sets = new DisjointSet(4);

            Assert.That(sets.UnionSet(0, 1), Is.True);
            Assert.That(sets.UnionSet(1, 2), Is.True);
            Assert.That(sets.UnionSet(0, 2), Is.False);

            Assert.That(sets.ComponentCount, Is.EqualTo(2));
            Assert.That(sets.FindSet(0), Is.EqualTo(sets.FindSet(2)));
            Assert.That(sets.FindSet(0), Is.Not.EqualTo(sets.FindSet(3)));
        }

        [Test]
        public void UnionCanConnectEveryElement()
        {
            const int size = 10_000;
            var sets = new DisjointSet(size);

            for (var element = 1; element < size; element++)
            {
                Assert.That(sets.UnionSet(element - 1, element), Is.True);
            }

            Assert.That(sets.ComponentCount, Is.EqualTo(1));
            var root = sets.FindSet(0);
            for (var element = 1; element < size; element++)
            {
                Assert.That(sets.FindSet(element), Is.EqualTo(root));
            }
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void FindSetRejectsOutOfRangeElements(int element)
        {
            var sets = new DisjointSet(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => sets.FindSet(element));
        }

        [Test]
        public void ConstructorRejectsNegativeSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DisjointSet(-1));
        }

        [Test]
        public void UnionUsesRootRanksWhenGivenNonRoots()
        {
            var sets = new DisjointSet(6);
            sets.UnionSet(0, 1);
            sets.UnionSet(2, 3);
            sets.UnionSet(0, 2);
            sets.UnionSet(4, 5);
            var tallerRoot = sets.FindSet(0);
            sets.UnionSet(3, 4);
            Assert.That(sets.FindSet(4), Is.EqualTo(tallerRoot));
            Assert.That(sets.ComponentCount, Is.EqualTo(1));
        }

        [Test]
        public void FindCompressesEveryParentInADeepChainWithoutRecursion()
        {
            const int size = 250_000;
            var sets = new DisjointSet(size);
            var parents = (int[])typeof(DisjointSet)
                .GetField("_parents", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sets);
            for (var index = 0; index < size - 1; index++) parents[index] = index + 1;

            Assert.That(sets.FindSet(0), Is.EqualTo(size - 1));
            for (var index = 0; index < size; index++)
                Assert.That(parents[index], Is.EqualTo(size - 1));
        }

        [Test]
        public void InvalidUnionDoesNotChangeComponentCount()
        {
            var sets = new DisjointSet(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => sets.UnionSet(0, 2));
            Assert.That(sets.ComponentCount, Is.EqualTo(2));
            Assert.That(sets.UnionSet(0, 0), Is.False);
            Assert.That(sets.ComponentCount, Is.EqualTo(2));
        }

        [Test]
        public void EmptySetHasNoComponents()
        {
            var sets = new DisjointSet(0);

            Assert.That(sets.ComponentCount, Is.Zero);
        }
    }
}
