# Unity-Maze-generation-using-disjoint-sets

![mazeCover](https://user-images.githubusercontent.com/72280379/196811359-9cd38f5f-e00a-485b-b4bd-922bd84081d0.jpg)

Maze generator using disjoin-sets in Unity 3D. 
There are no cycles and the random maze is solvable by a unique path. I have implemented a Depth First Search to show the path for visualization purposes.

# Maze generation visualization

The generation is very quick (50k cells in less than 100 ms) but It's also possible to view the generation at human speed: 

![parameters](https://user-images.githubusercontent.com/72280379/197043839-c21b8e2f-d337-411e-b5f6-59e9c8c78c2c.jpg)


I inserted a delay before the start and a delay at each iteration, so that it's possible to play and see the maze growing visually.

![gifMazwe](https://user-images.githubusercontent.com/72280379/196811398-d209820e-3bfb-4014-a763-bdb0f0c3d346.gif)


# So simple, but so elegant.

Disjoint-sets are super simple. In this project, they are implemented using two classical optimizations: 
- Optimization with Path Compression
- Optimization with Union by Rank 

````C#
public class DisjointSet 
{
    private int[] _set;
    private int[] _rank;

    public DisjointSet(int size)
    {
        _set = new int[size];
        _rank = new int[size];
    }
    
    public void MakeSet(int x)
    {
        _set[x] = x;
        _rank[x] = 0;
    }
    
    public int FindSet(int x)
    {
        if (x != _set[x]) return FindSet(_set[x]);
        return x;
    }

    public void UnionSet(int x, int y)
    {
        var parentX = FindSet(x);
        var parentY = FindSet(y);
        if (_rank[x] > _rank[y]) _set[parentY] = parentX;
        else
        {
            _set[parentX] = parentY;
            if (_rank[x] == _rank[y]) _rank[y]++;
        }
    }
}
````

# References 

[Disjoint Sets on Wikipedia](https://en.wikipedia.org/wiki/Disjoint-set_data_structure#Merging_two_sets)

[Algorithm to generate the maze](https://en.wikipedia.org/wiki/Maze_generation_algorithm#Randomized_Kruskal's_algorithm)
