using NUnit.Framework;

namespace DisjointSetMaze.Tests
{
    public sealed class WallDirectionTests
    {
        [TestCase(WallDirection.North, WallDirection.South)]
        [TestCase(WallDirection.East, WallDirection.West)]
        [TestCase(WallDirection.South, WallDirection.North)]
        [TestCase(WallDirection.West, WallDirection.East)]
        public void OppositeIsBidirectional(WallDirection direction, WallDirection expected)
        {
            Assert.That(direction.Opposite(), Is.EqualTo(expected));
            Assert.That(direction.Opposite().Opposite(), Is.EqualTo(direction));
        }
    }
}
