using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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