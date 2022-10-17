using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public int size = 10;
    public float waitingTimeBeforeStart = 1f;
    public bool timeLimited = true;
    public float timeIteration = 0.1f;
    [SerializeField] private GameObject _wallPrefab;
    private Cell[,] _cells;
    private float _wallSize;


    private void Start()
    {
        _cells = new Cell[size, size];
        SpawnEntireGrid(size);

        StartCoroutine(RanMaze());
        DFS();
    }

    private void SpawnEntireGrid(int size)
    {
        _wallSize = _wallPrefab.transform.localScale.y;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var positionCell = new Vector3(x * _wallSize, 0, z * _wallSize);
                Cell newCell = new Cell(x, z, size, positionCell);
                _cells[x, z] = newCell;

                Quaternion wallRotation = Quaternion.Euler(0f, 90f, 0f);
                var positionWall = positionCell + new Vector3(_wallSize / 2f, 0f, 0f);
                var newWall = Instantiate(_wallPrefab, positionWall, wallRotation);
                newCell.AddWall(1, newWall);

                positionWall = positionCell + new Vector3(0f, 0f, _wallSize / 2f);
                newWall = Instantiate(_wallPrefab, positionWall, Quaternion.identity);
                newCell.AddWall(0, newWall);

                if (x == 0)
                {
                    Quaternion wallRotated = Quaternion.Euler(0f, 90f, 0f);
                    positionWall = positionCell + new Vector3(-_wallSize / 2f, 0f, 0f);
                    newWall = Instantiate(_wallPrefab, positionWall, wallRotated);
                    newCell.AddWall(3, newWall);
                }
                else
                {
                    newCell.AddWall(3, _cells[x - 1, z].GetWall(1));
                }

                if (z == 0)
                {
                    positionWall = positionCell + new Vector3(0f, 0f, -_wallSize / 2f);
                    newWall = Instantiate(_wallPrefab, positionWall, Quaternion.identity);
                    newCell.AddWall(2, newWall);
                }
                else
                {
                    newCell.AddWall(2, _cells[x, z - 1].GetWall(0));
                }
            }
        }
    }


    private IEnumerator RanMaze()
    {
        DisjointSet sets = new DisjointSet(size * size);
        for (int i = 0; i < size * size; i++) sets.MakeSet(i);
        yield return new WaitForSeconds(waitingTimeBeforeStart);
        var source = 0;
        var dest = size * size - 1;
        int iterations = 0;
        while (sets.FindSet(source) != sets.FindSet(dest))
        {
            var randomIndexCell = Random.Range(0, size * size);
            var randomCell = GetCellwithIndex(randomIndexCell);
            var randomWall = Random.Range(0, 4);
            iterations++;
            if (randomCell.GetWall(randomWall) == null) continue;
            int indexNeighbour = -1;
            if (!IsPerimetralWall(randomIndexCell,randomWall))
            {
                if (randomWall == 0) indexNeighbour = randomCell.GetIndex() + 1;
                if (randomWall == 1) indexNeighbour = randomCell.GetIndex() + size;
                if (randomWall == 2) indexNeighbour = randomCell.GetIndex() - 1;
                if (randomWall == 3) indexNeighbour = randomCell.GetIndex() - size;
            }

            if (indexNeighbour >= 0 && indexNeighbour < size * size)
            {
                if (sets.FindSet(indexNeighbour) != sets.FindSet(randomIndexCell)) //not reachable
                {
                    randomCell.DestroyWall(randomWall);
                    sets.UnionSet(indexNeighbour, randomIndexCell);
                }
            }
            if(timeLimited)yield return new WaitForSeconds(timeIteration);
        }
    }



        private Cell GetCellwithIndex(int index)
        {
            var z = index % size;
            var x = (index - z) / size;
            return _cells[x, z];
        }


        private bool IsPerimetralWall(int cellIndex, int wallIndex)
        {
            var z = cellIndex % size;
            var x = (cellIndex - z) / size;
            if (z == 0 && wallIndex == 2) return true;
            if (z == size - 1 && wallIndex == 0) return true;
            if (x == size - 1 && wallIndex == 1) return true;
            if (x == 0 && wallIndex == 3) return true;
            return false;
        }


        private void DFS()
        {
            Debug.DrawLine(_cells[size-1,size-1].GetWorldPosition(), _cells[0,0].GetWorldPosition(), Color.green, 50.0f, false);

            Stack stack = new Stack();
            int[] _predecessors = new[size*size -1];
            var source = _cells[0,0];
            var target = _cells[size - 1, size - 1];
            stack.Add(source);
            while (node.GetWorldPosition() != target.GetWorldPosition() || stack.Count>0)
            {
                Cell node = stack.Pop();
                var indexNode = node.GetIndex();
                if(indexNode-size>=0)_cells
            }

        }
        
        
        
    }