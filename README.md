# Disjoint Set Maze

![mazeCover](https://user-images.githubusercontent.com/72280379/196811359-9cd38f5f-e00a-485b-b4bd-922bd84081d0.jpg)

This Unity 3D project generates a **perfect maze**: every cell is reachable, there are no cycles, and there is exactly one path between any two cells. An iterative depth-first search displays the solution from the lower-left cell to the upper-right cell.

## What makes this a perfect maze?

Model the square grid as a graph: each cell is a vertex, and each possible passage between orthogonally adjacent cells is an edge. For an `n × n` grid:

- `V = n²` cells
- `E = 2n(n − 1)` internal candidate edges
- `V − 1` accepted passages in the finished maze

A perfect maze is a **spanning tree** of that grid: it includes every cell, is connected, and has no cycles. Connecting only the entrance and exit is not enough. The earlier stopping rule could leave an acyclic forest with other cells unreachable, even though a unique corner-to-corner solution existed.

The corrected generator accepts a passage only when it joins two different components. This cannot create a cycle. Since the complete grid is connected, processing its edges can reduce the initial `V` components to one, requiring exactly `V − 1` successful unions. Generation checks both invariants before drawing the solution:

```text
ComponentCount == 1
carvedPassages == CellCount - 1
```

## Maze generation visualization

Each run follows five stages:

1. **Snapshot:** validate the configuration, copy settings and prefab references into `RunSettings`, and create a local seeded `System.Random`.
2. **Build:** create the cells and walls. Each shared wall is instantiated once and referenced by both neighboring cells.
3. **Shuffle:** enumerate every internal north/east neighbor pair once, then shuffle the edge list with Fisher–Yates.
4. **Carve:** scan the shuffled edges using randomized Kruskal. Open a passage only when `UnionSet` returns `true`, stopping when one component remains.
5. **Solve:** run iterative depth-first search over the passage data, reconstruct the solution, and instantiate its path markers.

Fisher–Yates shuffles in `O(E)` time, and the subsequent scan examines at most `E` edges. Unlike retry-based random sampling, it cannot repeatedly choose perimeter walls or the same candidate edge. An unbiased edge permutation does **not** imply uniform sampling over all possible spanning trees.

Generation can run without animation delays or pause after a configurable number of carved passages. Grid creation, search, and path-marker creation also yield in batches. The images below show the original visualization; Inspector fields have since changed.

![parameters](https://user-images.githubusercontent.com/72280379/197043839-c21b8e2f-d337-411e-b5f6-59e9c8c78c2c.jpg)

![gifMaze](https://user-images.githubusercontent.com/72280379/196811398-d209820e-3bfb-4014-a763-bdb0f0c3d346.gif)

## Controls

The free-fly camera uses the Input System directly:

- `W`, `A`, `S`, `D`: move forward, left, backward, and right relative to the camera
- `E` / `Q`: move up / down relative to the camera
- Left Shift: sprint
- Mouse: look while the cursor is captured
- Left click: capture the cursor; Escape: release it

### Generation settings

- Open `Assets/Scenes/SampleScene.unity` and enter Play mode. **Generate On Start** starts generation automatically; otherwise invoke **Generate** from the component context menu or wire `MazeGenerator.Generate()` to a UI button. The component must be enabled.
- **Maze Size** is the number of cells along each side, not the total cell count. Assign both **Wall Prefab** and **Path Marker Prefab**.
- **Cell Size** controls the distance between cell centers independently of prefab scale. Wall artwork must still fit that spacing. The grid is built in the generator's local XZ plane, with the first cell at local origin; moving, rotating, or scaling the generator transforms its maze.
- **Initial Delay** pauses after the grid is spawned. **Animate Generation**, **Generation Step Delay**, and **Passages Per Step** control carving cadence.
- Disable **Randomize Seed Each Generation** and set **Seed** to replay a maze. The actual seed is logged, displayed as **Last Used Seed**, and exposed by `LastUsedSeed`. Replay assumes the same grid size, algorithm, and runtime; it is not a cross-version `System.Random` guarantee.

The RNG belongs to the run and does not change `UnityEngine.Random` state. Settings and prefab references are captured before generation, so changing Inspector values mid-run applies to the next generation rather than mixing configurations.

| Work | Inspector budget | Behavior |
| --- | --- | --- |
| Cell and wall creation | **Cells Spawned Per Frame** | Yield after this many cells. |
| DFS expansion | **Search Nodes Per Frame** | Yield during solution search. |
| Solution markers | **Path Markers Per Step** | Batch marker creation; use the generation delay when animated. |
| Accepted passages | **Passages Per Step** | Pause after this many carved passages when animated. |

These are work-count budgets, not maximum frame-time guarantees. Edge-list construction, shuffling, and unanimated carving still run synchronously. Yielding improves responsiveness but does not reduce total CPU work or object count.

## Disjoint set

[`DisjointSet.cs`](Assets/Scripts/DisjointSet.cs) stores a parent and rank for each element. A root points to itself; two elements are connected exactly when `FindSet` returns the same root.

The two optimizations are now implemented explicitly:

- **Iterative full path compression:** first find the root, then rewrite every parent on the traversed path to point directly to it. The old recursive lookup found roots without compressing paths; iteration also removes the call-stack risk of an adversarial deep chain.
- **Union by rank on roots:** attach the lower-rank root beneath the higher-rank root, increasing rank only when equal-rank roots merge. Comparing the original elements' ranks, as the old code did, could preserve connectivity while losing the balancing guarantee. Rank is a balancing estimate, not the current height after compression.

Together these give amortized `O(α(V))` per operation, where the inverse Ackermann function grows so slowly that the cost is effectively constant at practical maze sizes.

Core methods (bounds validation is defined in the source file):

```csharp
public int FindSet(int element)
{
    ValidateElement(element);

    var root = element;
    while (_parents[root] != root)
        root = _parents[root];

    while (_parents[element] != element)
    {
        var parent = _parents[element];
        _parents[element] = root;
        element = parent;
    }

    return root;
}

public bool UnionSet(int first, int second)
{
    var firstRoot = FindSet(first);
    var secondRoot = FindSet(second);
    if (firstRoot == secondRoot) return false;

    if (_ranks[firstRoot] < _ranks[secondRoot])
    {
        var temporaryRoot = firstRoot;
        firstRoot = secondRoot;
        secondRoot = temporaryRoot;
    }

    _parents[secondRoot] = firstRoot;
    if (_ranks[firstRoot] == _ranks[secondRoot])
        _ranks[firstRoot]++;

    ComponentCount--;
    return true;
}
```

The constructor initializes all singleton roots, eliminating a separate `MakeSet` phase. Invalid sizes or indices throw `ArgumentOutOfRangeException`. A duplicate union returns `false` without changing rank or `ComponentCount`; a successful union decrements the count exactly once. The class is sealed and its backing arrays are readonly fields.

## Passage data and solution search

[`Cell.cs`](Assets/Scripts/Cell.cs) stores open directions in a byte bitmask: north, east, south, and west. Opening a passage records the direction on one cell and the opposite direction on its neighbor. Perimeter directions never open.

The bitmask is authoritative; wall GameObjects are its visual representation. Carved walls are deactivated rather than individually destroyed, and search reads `IsPassageOpen`, not a wall's null or active state. Unity's delayed destruction and overloaded null comparison could make the old approach appear to work, but object lifetime should not determine connectivity.

The iterative DFS in [`MazeGenerator.cs`](Assets/Scripts/MazeGenerator.cs):

- Marks cells visited when discovered, preventing duplicate stack pushes.
- Records predecessors and checks that the stack is nonempty before popping.
- Logs an error if the target is unreachable.
- Caps reconstruction at `CellCount` steps to prevent an endless predecessor cycle.
- Handles `1 × 1`: source equals target, no passage is carved, and exactly one solution marker is created.

DFS is sufficient because a perfect maze has only one simple path between the selected corners. BFS would return the same path; this is not a claim that DFS finds shortest paths in arbitrary graphs.

## Safe regeneration and ownership

One tracked coroutine owns a generation run and its nested work. A valid new `Generate()` request stops the previous run before replacing its output. Invalid settings are rejected before replacing the existing maze. `IsGenerating` exposes whether a run is active.

All output belongs to a tracked hierarchy under the generator:

```text
MazeGenerator
└── Generated Maze
    ├── Walls
    └── Path
```

Cleanup deactivates the old generated root immediately, then schedules its destruction. It uses the owned reference, not a scene-wide tag search or a child-name fallback, so unrelated authored objects are preserved—even if they are also named `Generated Maze`. Both prefabs use `Untagged`; no custom project tag is required.

Disabling the component during generation cancels the run and removes its partial output. Disabling it after completion does not clear the finished maze. Renamed serialized fields use `FormerlySerializedAs` to preserve existing scene values where applicable.

## Complexity and scale

| Phase | Time | Extra memory |
| --- | --- | --- |
| Build internal edge list | `O(E)` | `O(E)` |
| Fisher–Yates shuffle | `O(E)` | `O(1)` beyond the list |
| Kruskal with disjoint set | `O(E α(V))` | `O(V)` for the disjoint set |
| Iterative DFS | `O(V + E)` upper bound | `O(V)` |
| Path reconstruction | `O(V)` worst case | `O(V)` |

Efficient graph algorithms do not make Unity object creation free. A `224 × 224` maze illustrates the distinction:

| Quantity | Count |
| --- | ---: |
| Cells, `V` | 50,176 |
| Internal candidate edges, `E` | 99,904 |
| Carved passages, `V − 1` | 50,175 |
| Initially created wall GameObjects, `2n(n + 1)` | 100,800 |
| Walls left active after carving | 50,625 |

All 100,800 wall objects remain allocated until cleanup, including disabled walls; solution markers add further objects. Allocation, Transform management, culling, hierarchy overhead, and draw submission can dominate total time. The earlier claim of “50k cells in less than 100 ms” has no supporting benchmark and should not be treated as a performance guarantee.

URP instancing and frame budgets do not eliminate GameObject lifecycle cost. The next performance step is to profile a standalone target build—creation time, allocations, CPU/GPU frame time, and rendering batches—then compare pooling, combined meshes, or procedural wall geometry based on the bottleneck. An Entities-based renderer is another possible direction, not a requirement for maze correctness.

## Manual checks and build

For an interactive check, generate fixed-seed mazes at sizes `1`, `2`, `10`, and a representative larger size; regenerate during an active run, disable mid-run, and check placement with a translated or rotated generator.

To build from a terminal with Unity CLI available, save your work and close the Editor for this project copy before running:

```powershell
unity build . --target StandaloneWindows64 --output-path Builds/Windows/DisjointSetMaze.exe --allow-dirty-build
```

## References

- [Disjoint-set data structure](https://en.wikipedia.org/wiki/Disjoint-set_data_structure#Merging_two_sets)
- [Randomized Kruskal maze generation](https://en.wikipedia.org/wiki/Maze_generation_algorithm#Randomized_Kruskal's_algorithm)
