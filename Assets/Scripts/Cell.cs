using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    private GameObject[] _walls;
    private int[] _neighbours;
    private int _index;
    private Vector3 _worldPosition;
    public Cell(int x, int y, int sizeMaze, Vector3 worldPosition)
    {
        _index = x * sizeMaze + y;
        _walls = new GameObject[4];
        _worldPosition = worldPosition;
    }
    
    public int getIndex()
    {
        return _index;
    }

    public void addWall(int index,GameObject wall)
    {
        _walls[index] = wall;
    }
    
    public GameObject getWall(int index)
    {
        return _walls[index];
    }

    public void destroyWall(int index)
    {
        if(_walls[index]==null)Debug.Log("Critical Error: you are trying to destroy a wall that there isn't");
        else
        {
            GameObject.Destroy(_walls[index]);
        }
    }

    public Vector3 getWorldPosition()
    {
        return _worldPosition;
    }
}
