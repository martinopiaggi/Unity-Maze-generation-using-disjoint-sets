using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    private GameObject[] _walls;
    private int _index;
    private Vector3 _worldPosition;
    public Cell(int x, int y, int sizeMaze, Vector3 worldPosition)
    {
        _index = x * sizeMaze + y;
        _walls = new GameObject[4];
        _worldPosition = worldPosition;
    }
    
    public int GetIndex()
    {
        return _index;
    }

    public void AddWall(int index,GameObject wall)
    {
        _walls[index] = wall;
    }
    
    public GameObject GetWall(int index)
    {
        return _walls[index];
    }

    public void DestroyWall(int index)
    {
        if(_walls[index]!=null) GameObject.Destroy(_walls[index]);
    }

    public Vector3 GetWorldPosition()
    {
        return _worldPosition;
    }
}
