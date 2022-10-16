using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisjointSet : MonoBehaviour
{
    private int[] _set;
    private int[] _rank;

    public DisjointSet(int maximumSize)
    {
        _set = new int[maximumSize];
        _rank = new int[maximumSize];
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
        var _parentX = FindSet(x);
        var _parentY = FindSet(y);

        if (_rank[x] > _rank[y])
        {
            _set[_parentY] = _parentX;
        }
        else
        {
            _set[_parentX] = _parentY;
            if (_rank[x] == _rank[y]) _rank[y]++;
        }
    }
}