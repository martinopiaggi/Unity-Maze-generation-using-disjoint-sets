using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public int size = 10;
    [SerializeField] private GameObject _wallPrefab;
    private Cell[,] _cells;
    
    private void Start()
    {
        _cells = new Cell[10, 10];
        spawnEntireGrid(10);
    }

    private void spawnEntireGrid(int size)
    {
        var wallSize = _wallPrefab.transform.localScale.y;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                var positionCell = new Vector3(x * wallSize, 0, z * wallSize);
                Cell newCell = new Cell(x, z, size);
                _cells[x,z] = newCell;
                
                
                
                Quaternion wallRotation = Quaternion.Euler(0f,90f,0f);
                var positionWall = positionCell + new Vector3(wallSize / 2f, 0f, 0f);
                var newWall = Instantiate(_wallPrefab, positionWall, wallRotation);
                newCell.addWall(1,newWall);
                
                positionWall = positionCell + new Vector3(0f, 0f, wallSize / 2f);
                newWall = Instantiate(_wallPrefab, positionWall, Quaternion.identity);
                newCell.addWall(2,newWall);
                
                if (x == 0)
                {
                    Quaternion wallRotated = Quaternion.Euler(0f,90f,0f);
                   positionWall = positionCell + new Vector3(-wallSize / 2f, 0f, 0f);
                    newWall = Instantiate(_wallPrefab, positionWall, wallRotated);
                    newCell.addWall(3,newWall);
                }
                
                if (z == 0)
                {
                    positionWall = positionCell + new Vector3(0f, 0f, -wallSize / 2f);
                    newWall = Instantiate(_wallPrefab, positionWall, Quaternion.identity);
                    newCell.addWall(0,newWall);
                }
                
                
                
                
            } 
        }
    }
}
