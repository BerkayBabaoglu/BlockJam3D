using System;
using UnityEngine;

public class GridData
{
    public int cellsX;
    public int cellsZ;
    public bool[] grassCells; 

    public GridData(int x,int z)
    {
        cellsX = x;
        cellsZ = z;
        grassCells = new bool[x*z];
    }

    public bool GetCell(int x,int z)
    {
        if(x<0 || x >= cellsX || z<0 || z>= cellsZ) return false;
        return grassCells[z * cellsX + x];
    }

    public void SetCell(int x, int z, bool value)
    {
        if (x < 0 || x >= cellsX || z < 0 || z >= cellsZ) return;
        grassCells[z * cellsX + x] = value;
    }
}
