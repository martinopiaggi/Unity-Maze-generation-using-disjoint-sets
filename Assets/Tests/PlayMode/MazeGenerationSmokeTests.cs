using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DisjointSetMaze.Tests
{
    public sealed class MazeGenerationSmokeTests
    {
        [UnityTest]
        public IEnumerator SampleSceneGeneratesAndRegeneratesAMaze()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

            var generator = Object.FindFirstObjectByType<MazeGenerator>();
            Assert.That(generator, Is.Not.Null, "SampleScene must contain a MazeGenerator.");

            yield return WaitForGeneration(generator, 20f);
            AssertGeneratedHierarchy(generator);

            generator.Generate();
            yield return WaitForGeneration(generator, 20f);
            AssertGeneratedHierarchy(generator);

            var activeGeneratedRoots = 0;
            foreach (Transform child in generator.transform)
            {
                if (child.gameObject.activeSelf && child.name == "Generated Maze")
                {
                    activeGeneratedRoots++;
                }
            }

            Assert.That(activeGeneratedRoots, Is.EqualTo(1), "Regeneration must replace the active maze hierarchy.");
        }

        private static IEnumerator WaitForGeneration(MazeGenerator generator, float timeoutSeconds)
        {
            yield return null;
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (generator.IsGenerating && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(generator.IsGenerating, Is.False, "Maze generation exceeded the smoke-test timeout.");
        }

        private static void AssertGeneratedHierarchy(MazeGenerator generator)
        {
            var generatedRoot = generator.transform.Find("Generated Maze");
            Assert.That(generatedRoot, Is.Not.Null);

            var walls = generatedRoot.Find("Walls");
            var path = generatedRoot.Find("Path");
            Assert.That(walls, Is.Not.Null);
            Assert.That(path, Is.Not.Null);
            Assert.That(walls.childCount, Is.GreaterThan(0));
            Assert.That(path.childCount, Is.GreaterThan(0));
        }
    }

    public sealed class MazeLifecycleTests
    {
        private GameObject _owner;
        private GameObject _wall;
        private GameObject _marker;
        private MazeGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Test Generator");
            _wall = new GameObject("Test Wall");
            _marker = new GameObject("Test Marker");
            _generator = _owner.AddComponent<MazeGenerator>();
            Set("_generateOnStart", false);
            Set("_initialDelay", 0f);
            Set("_animateGeneration", false);
            Set("_wallPrefab", _wall);
            Set("_pathMarkerPrefab", _marker);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_owner);
            Object.Destroy(_wall);
            Object.Destroy(_marker);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SingleCellCompletesWithOneSolutionMarker() => GenerateAndCheck(1);

        [UnityTest]
        public IEnumerator TwoByTwoCompletesWithASimpleSolution() => GenerateAndCheck(2);

        [UnityTest]
        public IEnumerator TenByTenCompletesWithASimpleSolution() => GenerateAndCheck(10);

        [UnityTest]
        public IEnumerator LargerMazeCompletesWithASimpleSolution() => GenerateAndCheck(32);

        [UnityTest]
        public IEnumerator RepeatedGenerateCancelsPartialRunsAndPreservesAuthoredChildren()
        {
            var authored = new GameObject("Generated Maze");
            authored.transform.SetParent(_owner.transform, false);
            Set("_cellsSpawnedPerFrame", 1);
            _generator.Generate();
            yield return null;
            _generator.Generate();
            _generator.Generate();
            yield return WaitForCompletion();
            yield return null;

            Assert.That(authored != null && authored.activeSelf, Is.True);
            Assert.That(_owner.transform.childCount, Is.EqualTo(2));
            Assert.That(GeneratedRoot().Find("Path").childCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator DisablingMidRunRemovesOnlyPartialGeneratedObjects()
        {
            Set("_cellsSpawnedPerFrame", 1);
            _generator.Generate();
            yield return null;
            Assert.That(_generator.IsGenerating, Is.True);
            _generator.enabled = false;
            yield return null;
            Assert.That(_generator.IsGenerating, Is.False);
            Assert.That(_owner.transform.childCount, Is.Zero);
            _generator.enabled = true;
            _generator.Generate();
            yield return WaitForCompletion();
            Assert.That(GeneratedRoot().Find("Path").childCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator SettingsAreSnapshottedAndRandomStateIsNotChanged()
        {
            Set("_mazeSize", 2);
            Set("_cellsSpawnedPerFrame", 1);
            var randomState = Random.state;
            _generator.Generate();
            Set("_mazeSize", 10);
            Set("_cellSize", 7f);
            yield return WaitForCompletion();
            Assert.That(GeneratedRoot().Find("Walls").childCount, Is.EqualTo(12));
            var path = GeneratedRoot().Find("Path");
            Assert.That(path.GetChild(path.childCount - 1).localPosition, Is.EqualTo(new Vector3(5f, 0f, 5f)));
            Assert.That(Random.state, Is.EqualTo(randomState));
        }

        [UnityTest]
        public IEnumerator FixedSeedRegenerationKeepsIdenticalWalls()
        {
            _generator.Generate();
            yield return WaitForCompletion();
            var first = WallStates();
            _generator.Generate();
            yield return WaitForCompletion();
            Assert.That(WallStates(), Is.EqualTo(first));
        }

        private IEnumerator GenerateAndCheck(int size)
        {
            Set("_mazeSize", size);
            _owner.transform.SetPositionAndRotation(new Vector3(13f, 2f, -7f), Quaternion.Euler(0f, 37f, 0f));
            _generator.Generate();
            yield return WaitForCompletion();
            var root = GeneratedRoot();
            Assert.That(root.localPosition, Is.EqualTo(Vector3.zero));
            var walls = root.Find("Walls");
            Assert.That(walls.childCount, Is.EqualTo(2 * size * (size + 1)));
            var inactiveWalls = new HashSet<Vector3>();
            foreach (Transform wall in walls)
                if (!wall.gameObject.activeSelf) inactiveWalls.Add(wall.localPosition);
            Assert.That(inactiveWalls.Count, Is.EqualTo(size * size - 1));

            var path = root.Find("Path");
            Assert.That(path.childCount, Is.InRange(1, size * size));
            Assert.That(path.GetChild(0).localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Vector3.Distance(path.GetChild(0).position, _owner.transform.position), Is.LessThan(0.001f));
            Assert.That(path.GetChild(path.childCount - 1).localPosition,
                Is.EqualTo(new Vector3((size - 1) * 5f, 0f, (size - 1) * 5f)));
            var visited = new HashSet<Vector3>();
            for (var index = 0; index < path.childCount; index++)
            {
                var position = path.GetChild(index).localPosition;
                Assert.That(visited.Add(position), Is.True, "The solution must be simple.");
                if (index == 0) continue;
                var previous = path.GetChild(index - 1).localPosition;
                var delta = position - previous;
                Assert.That(Mathf.Abs(delta.x) + Mathf.Abs(delta.z), Is.EqualTo(5f));
                Assert.That(inactiveWalls.Contains((position + previous) / 2f), Is.True,
                    "The solution crossed a closed wall.");
            }
        }

        private bool[] WallStates()
        {
            var walls = GeneratedRoot().Find("Walls");
            var states = new bool[walls.childCount];
            for (var index = 0; index < states.Length; index++) states[index] = walls.GetChild(index).gameObject.activeSelf;
            return states;
        }

        private Transform GeneratedRoot()
        {
            return (Transform)typeof(MazeGenerator).GetField("_generatedRoot", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_generator);
        }

        private IEnumerator WaitForCompletion()
        {
            var deadline = Time.realtimeSinceStartup + 20f;
            while (_generator.IsGenerating && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(_generator.IsGenerating, Is.False, "Maze generation timed out.");
        }

        private void Set(string name, object value)
        {
            typeof(MazeGenerator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_generator, value);
        }
    }
}
