using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DisjointSetMaze
{
    public sealed class MazeGenerator : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Maze";

        private static readonly WallDirection[] Directions =
        {
            WallDirection.North,
            WallDirection.East,
            WallDirection.South,
            WallDirection.West
        };

        [Header("Maze")]
        [FormerlySerializedAs("size")]
        [SerializeField, Min(1)] private int _mazeSize = 10;

        [FormerlySerializedAs("waitingTimeBeforeStart")]
        [SerializeField, Min(0f)] private float _initialDelay = 1f;

        [FormerlySerializedAs("generate")]
        [SerializeField] private bool _generateOnStart = true;

        [Tooltip("Distance between adjacent cell centers in local space.")]
        [SerializeField, Min(0.01f)] private float _cellSize = 5f;

        [Header("Generation visualization")]
        [FormerlySerializedAs("timeLimited")]
        [SerializeField] private bool _animateGeneration = true;

        [FormerlySerializedAs("timeIteration")]
        [SerializeField, Min(0f)] private float _generationStepDelay = 0.1f;

        [FormerlySerializedAs("stepIteration")]
        [SerializeField, Min(1)] private int _passagesPerStep = 10;

        [Header("Reproducibility")]
        [SerializeField] private bool _randomizeSeedEachGeneration;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private int _lastUsedSeed;

        [Header("Frame budgets")]
        [SerializeField, Min(1)] private int _cellsSpawnedPerFrame = 256;
        [SerializeField, Min(1)] private int _searchNodesPerFrame = 2048;
        [SerializeField, Min(1)] private int _pathMarkersPerStep = 10;

        [Header("Prefabs")]
        [SerializeField] private GameObject _wallPrefab;

        [FormerlySerializedAs("_cubePrefab")]
        [SerializeField] private GameObject _pathMarkerPrefab;

        private Coroutine _generationCoroutine;
        private Transform _generatedRoot;

        public int LastUsedSeed => _lastUsedSeed;
        public bool IsGenerating => _generationCoroutine != null;

        private void Start()
        {
            if (_generateOnStart)
            {
                Generate();
            }
        }

        private void OnDisable()
        {
            if (StopActiveGeneration())
            {
                RemoveGeneratedHierarchy();
            }
        }

        private void OnValidate()
        {
            _mazeSize = Mathf.Max(1, _mazeSize);
            _cellSize = Mathf.Max(0.01f, _cellSize);
            _initialDelay = Mathf.Max(0f, _initialDelay);
            _generationStepDelay = Mathf.Max(0f, _generationStepDelay);
            _passagesPerStep = Mathf.Max(1, _passagesPerStep);
            _cellsSpawnedPerFrame = Mathf.Max(1, _cellsSpawnedPerFrame);
            _searchNodesPerFrame = Mathf.Max(1, _searchNodesPerFrame);
            _pathMarkersPerStep = Mathf.Max(1, _pathMarkersPerStep);
        }

        [ContextMenu("Generate")]
        public void Generate()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Maze generation is available in Play mode.", this);
                return;
            }

            if (!isActiveAndEnabled)
            {
                Debug.LogWarning("Enable the MazeGenerator before generating a maze.", this);
                return;
            }

            if (!TryCreateRunSettings(out var settings)) return;

            StopActiveGeneration();
            RemoveGeneratedHierarchy();

            _lastUsedSeed = _randomizeSeedEachGeneration ? Guid.NewGuid().GetHashCode() : _seed;
            var random = new System.Random(_lastUsedSeed);
            _generatedRoot = CreateChild(GeneratedRootName, transform);

            _generationCoroutine = StartCoroutine(GenerateMaze(settings, random, _generatedRoot));
            Debug.Log($"Generating {settings.Size}x{settings.Size} maze with seed {_lastUsedSeed}.", this);
        }

        private bool TryCreateRunSettings(out RunSettings settings)
        {
            settings = default;

            if (_mazeSize < 1)
            {
                Debug.LogError("Maze size must be at least one.", this);
                return false;
            }

            if (float.IsNaN(_cellSize) || float.IsInfinity(_cellSize) || _cellSize <= 0f)
            {
                Debug.LogError("Cell size must be finite and greater than zero.", this);
                return false;
            }

            if (_wallPrefab == null || _pathMarkerPrefab == null)
            {
                Debug.LogError("Assign both the wall and path marker prefabs before generating.", this);
                return false;
            }

            var cellCount = (long)_mazeSize * _mazeSize;
            var edgeCount = 2L * _mazeSize * (_mazeSize - 1L);
            if (cellCount > int.MaxValue || edgeCount > int.MaxValue)
            {
                Debug.LogError("The requested maze is too large for the in-memory grid representation.", this);
                return false;
            }

            settings = new RunSettings(
                _mazeSize,
                (int)cellCount,
                _cellSize,
                _wallPrefab,
                _pathMarkerPrefab,
                _initialDelay,
                _animateGeneration,
                _generationStepDelay,
                Mathf.Max(1, _passagesPerStep),
                Mathf.Max(1, _cellsSpawnedPerFrame),
                Mathf.Max(1, _searchNodesPerFrame),
                Mathf.Max(1, _pathMarkersPerStep));
            return true;
        }

        private IEnumerator GenerateMaze(RunSettings settings, System.Random random, Transform generationRoot)
        {
            var cells = new Cell[settings.CellCount];
            var wallsRoot = CreateChild("Walls", generationRoot);
            var pathRoot = CreateChild("Path", generationRoot);

            yield return SpawnGrid(cells, settings, wallsRoot);

            if (settings.InitialDelay > 0f)
            {
                yield return new WaitForSeconds(settings.InitialDelay);
            }

            var sets = new DisjointSet(settings.CellCount);
            var edges = BuildInternalEdges(settings.Size);
            Shuffle(edges, random);

            var carvedPassages = 0;
            var generationDelay = settings.GenerationStepDelay > 0f
                ? new WaitForSeconds(settings.GenerationStepDelay)
                : null;

            foreach (var edge in edges)
            {
                if (!sets.UnionSet(edge.CellIndex, edge.NeighbourIndex)) continue;

                OpenPassage(cells, edge);
                carvedPassages++;

                if (settings.AnimateGeneration && carvedPassages % settings.PassagesPerStep == 0)
                {
                    yield return generationDelay;
                }

                if (sets.ComponentCount == 1) break;
            }

            if (sets.ComponentCount != 1 || carvedPassages != settings.CellCount - 1)
            {
                Debug.LogError("Maze generation ended without producing a spanning tree.", this);
                _generationCoroutine = null;
                yield break;
            }

            yield return DrawSolutionPath(cells, settings, pathRoot);
            _generationCoroutine = null;
        }

        private IEnumerator SpawnGrid(Cell[] cells, RunSettings settings, Transform wallsRoot)
        {
            var wallSize = settings.CellSize;
            var spawnedCells = 0;

            for (var x = 0; x < settings.Size; x++)
            {
                for (var z = 0; z < settings.Size; z++)
                {
                    var position = new Vector3(x * wallSize, 0f, z * wallSize);
                    var cell = new Cell(x, z, settings.Size, position);
                    cells[cell.Index] = cell;

                    var northWall = InstantiateChild(
                        settings.WallPrefab,
                        wallsRoot,
                        position + new Vector3(0f, 0f, wallSize / 2f),
                        Quaternion.identity);
                    cell.SetWall(WallDirection.North, northWall);

                    var eastWall = InstantiateChild(
                        settings.WallPrefab,
                        wallsRoot,
                        position + new Vector3(wallSize / 2f, 0f, 0f),
                        Quaternion.Euler(0f, 90f, 0f));
                    cell.SetWall(WallDirection.East, eastWall);

                    if (z == 0)
                    {
                        var southWall = InstantiateChild(
                            settings.WallPrefab,
                            wallsRoot,
                            position + new Vector3(0f, 0f, -wallSize / 2f),
                            Quaternion.identity);
                        cell.SetWall(WallDirection.South, southWall);
                    }
                    else
                    {
                        cell.SetWall(
                            WallDirection.South,
                            cells[cell.Index - 1].GetWall(WallDirection.North));
                    }

                    if (x == 0)
                    {
                        var westWall = InstantiateChild(
                            settings.WallPrefab,
                            wallsRoot,
                            position + new Vector3(-wallSize / 2f, 0f, 0f),
                            Quaternion.Euler(0f, 90f, 0f));
                        cell.SetWall(WallDirection.West, westWall);
                    }
                    else
                    {
                        cell.SetWall(
                            WallDirection.West,
                            cells[cell.Index - settings.Size].GetWall(WallDirection.East));
                    }

                    spawnedCells++;
                    if (spawnedCells % settings.CellsSpawnedPerFrame == 0)
                    {
                        yield return null;
                    }
                }
            }
        }

        private static List<MazeEdge> BuildInternalEdges(int mazeSize)
        {
            var edgeCount = checked((int)(2L * mazeSize * (mazeSize - 1L)));
            var edges = new List<MazeEdge>(edgeCount);

            for (var x = 0; x < mazeSize; x++)
            {
                for (var z = 0; z < mazeSize; z++)
                {
                    var cellIndex = x * mazeSize + z;
                    if (z + 1 < mazeSize)
                    {
                        edges.Add(new MazeEdge(cellIndex, cellIndex + 1, WallDirection.North));
                    }

                    if (x + 1 < mazeSize)
                    {
                        edges.Add(new MazeEdge(cellIndex, cellIndex + mazeSize, WallDirection.East));
                    }
                }
            }

            return edges;
        }

        private static void Shuffle(IList<MazeEdge> edges, System.Random random)
        {
            for (var index = edges.Count - 1; index > 0; index--)
            {
                var otherIndex = random.Next(index + 1);
                var temporaryEdge = edges[index];
                edges[index] = edges[otherIndex];
                edges[otherIndex] = temporaryEdge;
            }
        }

        private static void OpenPassage(Cell[] cells, MazeEdge edge)
        {
            cells[edge.CellIndex].OpenPassage(edge.Direction);
            cells[edge.NeighbourIndex].OpenPassage(edge.Direction.Opposite());
        }

        private IEnumerator DrawSolutionPath(Cell[] cells, RunSettings settings, Transform pathRoot)
        {
            const int source = 0;
            var target = settings.CellCount - 1;
            var predecessors = new int[settings.CellCount];
            var visited = new bool[settings.CellCount];
            for (var index = 0; index < predecessors.Length; index++)
            {
                predecessors[index] = -1;
            }

            var stack = new Stack<int>();
            visited[source] = true;
            predecessors[source] = source;
            stack.Push(source);

            var searchedNodes = 0;
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == target) break;

                foreach (var direction in Directions)
                {
                    if (!cells[current].IsPassageOpen(direction)) continue;
                    if (!TryGetNeighbour(current, direction, settings.Size, out var neighbour)) continue;
                    if (visited[neighbour]) continue;

                    visited[neighbour] = true;
                    predecessors[neighbour] = current;
                    stack.Push(neighbour);
                }

                searchedNodes++;
                if (searchedNodes % settings.SearchNodesPerFrame == 0)
                {
                    yield return null;
                }
            }

            if (!visited[target])
            {
                Debug.LogError("The generated maze has no path between its entrance and exit.", this);
                yield break;
            }

            var path = new List<int>();
            var currentPathCell = target;
            for (var steps = 0; steps < settings.CellCount; steps++)
            {
                path.Add(currentPathCell);
                if (currentPathCell == source) break;
                currentPathCell = predecessors[currentPathCell];
            }

            if (path[path.Count - 1] != source)
            {
                Debug.LogError("Path reconstruction encountered an invalid predecessor chain.", this);
                yield break;
            }

            path.Reverse();
            var pathDelay = settings.GenerationStepDelay > 0f
                ? new WaitForSeconds(settings.GenerationStepDelay)
                : null;

            for (var pathIndex = 0; pathIndex < path.Count; pathIndex++)
            {
                InstantiateChild(
                    settings.PathMarkerPrefab,
                    pathRoot,
                    cells[path[pathIndex]].LocalPosition,
                    Quaternion.identity);

                if ((pathIndex + 1) % settings.PathMarkersPerStep == 0)
                {
                    yield return settings.AnimateGeneration ? pathDelay : null;
                }
            }
        }

        private static bool TryGetNeighbour(
            int cellIndex,
            WallDirection direction,
            int mazeSize,
            out int neighbourIndex)
        {
            var x = cellIndex / mazeSize;
            var z = cellIndex % mazeSize;

            switch (direction)
            {
                case WallDirection.North when z + 1 < mazeSize:
                    neighbourIndex = cellIndex + 1;
                    return true;
                case WallDirection.East when x + 1 < mazeSize:
                    neighbourIndex = cellIndex + mazeSize;
                    return true;
                case WallDirection.South when z > 0:
                    neighbourIndex = cellIndex - 1;
                    return true;
                case WallDirection.West when x > 0:
                    neighbourIndex = cellIndex - mazeSize;
                    return true;
                default:
                    neighbourIndex = -1;
                    return false;
            }
        }

        private static Transform CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static GameObject InstantiateChild(
            GameObject prefab,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }

        private bool StopActiveGeneration()
        {
            if (_generationCoroutine == null) return false;

            StopCoroutine(_generationCoroutine);
            _generationCoroutine = null;
            return true;
        }

        private void RemoveGeneratedHierarchy()
        {
            if (_generatedRoot == null) return;

            _generatedRoot.gameObject.SetActive(false);
            Destroy(_generatedRoot.gameObject);
            _generatedRoot = null;
        }

        private readonly struct MazeEdge
        {
            public readonly int CellIndex;
            public readonly int NeighbourIndex;
            public readonly WallDirection Direction;

            public MazeEdge(int cellIndex, int neighbourIndex, WallDirection direction)
            {
                CellIndex = cellIndex;
                NeighbourIndex = neighbourIndex;
                Direction = direction;
            }
        }

        private readonly struct RunSettings
        {
            public readonly int Size;
            public readonly int CellCount;
            public readonly float CellSize;
            public readonly GameObject WallPrefab;
            public readonly GameObject PathMarkerPrefab;
            public readonly float InitialDelay;
            public readonly bool AnimateGeneration;
            public readonly float GenerationStepDelay;
            public readonly int PassagesPerStep;
            public readonly int CellsSpawnedPerFrame;
            public readonly int SearchNodesPerFrame;
            public readonly int PathMarkersPerStep;

            public RunSettings(
                int size,
                int cellCount,
                float cellSize,
                GameObject wallPrefab,
                GameObject pathMarkerPrefab,
                float initialDelay,
                bool animateGeneration,
                float generationStepDelay,
                int passagesPerStep,
                int cellsSpawnedPerFrame,
                int searchNodesPerFrame,
                int pathMarkersPerStep)
            {
                Size = size;
                CellCount = cellCount;
                CellSize = cellSize;
                WallPrefab = wallPrefab;
                PathMarkerPrefab = pathMarkerPrefab;
                InitialDelay = initialDelay;
                AnimateGeneration = animateGeneration;
                GenerationStepDelay = generationStepDelay;
                PassagesPerStep = passagesPerStep;
                CellsSpawnedPerFrame = cellsSpawnedPerFrame;
                SearchNodesPerFrame = searchNodesPerFrame;
                PathMarkersPerStep = pathMarkersPerStep;
            }
        }
    }
}
