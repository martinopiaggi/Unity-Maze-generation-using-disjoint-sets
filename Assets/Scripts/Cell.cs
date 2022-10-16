using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    private GameObject[] _walls;
    private int[] _neighbours;
    private int _index;

    public Cell(int x, int y, int sizeMaze)
    {
        _index = x * sizeMaze + y;
        _walls = new GameObject[4];
    }
        
    public int getIndex()
    {
        return _index;
    }

    public void addWall(int index,GameObject wall)
    {
        _walls[index] = wall;
    }
}
