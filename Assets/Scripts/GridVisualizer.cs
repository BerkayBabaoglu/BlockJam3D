using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Visualization")]
    public bool showGrid = true;
    public bool showCellCenters = true;
    public bool showCellNumbers = true;
    public Color gridLineColor = Color.white;
    public Color cellCenterColor = Color.yellow;
    public Color cellNumberColor = Color.white;
    
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("Debug Info")]
    public bool showDebugInfo = true;
    public Vector3 testPosition = Vector3.zero;
    
    void OnDrawGizmos()
    {
        if (!showGrid) return;
        
        // Draw grid lines
        Gizmos.color = gridLineColor;
        
        // Vertical lines (X direction)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * cellSize, 0, 0);
            Vector3 end = start + new Vector3(0, 0, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }
        
        // Horizontal lines (Z direction)
        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = gridOrigin + new Vector3(0, 0, z * cellSize);
            Vector3 end = start + new Vector3(gridWidth * cellSize, 0, 0);
            Gizmos.DrawLine(start, end);
        }
        
        // Draw cell centers
        if (showCellCenters)
        {
            Gizmos.color = cellCenterColor;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 cellCenter = GetCellCenter(x, z);
                    Gizmos.DrawSphere(cellCenter, 0.1f);
                }
            }
        }
        
        // Draw test position
        if (showDebugInfo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(testPosition, 0.2f);
            
            // Show which grid cell the test position is in
            Vector2Int gridPos = GetGridPosition(testPosition);
            if (gridPos.x >= 0 && gridPos.x < gridWidth && gridPos.y >= 0 && gridPos.y < gridHeight)
            {
                Vector3 cellCenter = GetCellCenter(gridPos.x, gridPos.y);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(testPosition, cellCenter);
                Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
    
    // Get the center position of a grid cell
    Vector3 GetCellCenter(int x, int z)
    {
        return gridOrigin + new Vector3(x * cellSize + cellSize * 0.5f, 0, z * cellSize + cellSize * 0.5f);
    }
    
    // Convert world position to grid coordinates
    Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - gridOrigin;
        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int z = Mathf.FloorToInt(localPos.z / cellSize);
        
        return new Vector2Int(x, z);
    }
    
    // Convert grid coordinates to world position
    Vector3 GetWorldPosition(int x, int z)
    {
        return GetCellCenter(x, z);
    }
    
    // Get grid info for a world position
    public string GetGridInfo(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        Vector3 cellCenter = GetCellCenter(gridPos.x, gridPos.y);
        
        return $"World Pos: {worldPosition}\nGrid Pos: ({gridPos.x}, {gridPos.y})\nCell Center: {cellCenter}";
    }
    
    // Draw grid info in scene view
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Draw grid boundaries
        Gizmos.color = Color.blue;
        Vector3 gridSize = new Vector3(gridWidth * cellSize, 0.1f, gridHeight * cellSize);
        Gizmos.DrawWireCube(gridOrigin + gridSize * 0.5f, gridSize);
        
        // Draw grid origin
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(gridOrigin, 0.3f);
    }
}
