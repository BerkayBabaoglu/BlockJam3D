using System;
using UnityEngine;

[System.Serializable]
public class GridData
{
    public int cellsX;
    public int cellsZ;
    public int[,] cells; 

    [System.Serializable]
    public class SerializableGrid
    {
        public int cellsX;
        public int cellsZ;
        public int[] cells1D;
        
        public SerializableGrid(int x, int z)
        {
            cellsX = x;
            cellsZ = z;
            cells1D = new int[x * z];
        }
        
        public void SetCell(int x, int z, int value)
        {
            if (x >= 0 && x < cellsX && z >= 0 && z < cellsZ)
            {
                cells1D[z * cellsX + x] = value;
            }
        }
        
        public int GetCell(int x, int z)
        {
            if (x >= 0 && x < cellsX && z >= 0 && z < cellsZ)
            {
                return cells1D[z * cellsX + x];
            }
            return 0;
        }
        
        public GridData ToGridData()
        {
            GridData result = new GridData(cellsX, cellsZ);
            for (int x = 0; x < cellsX; x++)
            {
                for (int z = 0; z < cellsZ; z++)
                {
                    result.SetCell(x, z, GetCell(x, z));
                }
            }
            return result;
        }
    }

    public GridData(int x,int z)
    {
        cellsX = x;
        cellsZ = z;
        cells = new int[x,z];

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < z; j++)
            {
                cells[i,j] = 0;
            }
        }
    }

    public int GetCell(int x,int z)
    {
        if (cells == null || x < 0 || x >= cellsX || z < 0 || z >= cellsZ)
        {
            Debug.LogWarning($"GetCell: Geçersiz koordinatlar ({x},{z}) veya cells array null. cellsX={cellsX}, cellsZ={cellsZ}");
            return 0;
        }
        
        return cells[x,z];
    }

    public void SetCell(int x, int z, int value)
    {
        if (cells == null || x < 0 || x >= cellsX || z < 0 || z >= cellsZ)
        {
            Debug.LogWarning($"SetCell: Geçersiz koordinatlar ({x},{z}) veya cells array null. cellsX={cellsX}, cellsZ={cellsZ}");
            return; 
        }
       
        cells[x,z] = value;
    }
    
    // JSON serialization için
    public SerializableGrid ToSerializable()
    {
        SerializableGrid result = new SerializableGrid(cellsX, cellsZ);
        for (int x = 0; x < cellsX; x++)
        {
            for (int z = 0; z < cellsZ; z++)
            {
                result.SetCell(x, z, GetCell(x, z));
            }
        }
        return result;
    }
}
