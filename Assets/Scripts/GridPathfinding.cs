using System.Collections.Generic;
using UnityEngine;

public class GridPathfinding : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin = new Vector3(0f, -0.7f, 0f);
    
    [Header("Pathfinding Settings")]
    public LayerMask obstacleLayer = -1;
    public bool showDebugPath = true;
    public bool showCellCenters = true;
    public bool showGridInfo = true;
    public Color walkableCellColor = Color.green;
    public Color blockedCellColor = Color.red;
    public Color cellCenterColor = Color.yellow;
    
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 0.1f;
    
    private Node[,] grid;
    private List<Node> openList = new List<Node>();
    private List<Node> closedList = new List<Node>();
    
    // Node class for A* pathfinding
    [System.Serializable]
    public class Node
    {
        public int x, z;
        public Vector3 worldPosition;
        public bool walkable;
        public Node parent;
        public int gCost; // Cost from start to this node
        public int hCost; // Heuristic cost from this node to target
        public int fCost { get { return gCost + hCost; } }
        
        public Node(int x, int z, Vector3 worldPos, bool walkable)
        {
            this.x = x;
            this.z = z;
            this.worldPosition = worldPos;
            this.walkable = walkable;
        }
    }
    
    void Start()
    {
        InitializeGrid();
    }
    
    void InitializeGrid()
    {
        Debug.Log($"[GridPathfinding] Initializing grid: {gridWidth}x{gridHeight}, cellSize: {cellSize}, origin: {gridOrigin}");
        
        grid = new Node[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPoint = GetWorldPosition(x, z);
                
                // Make all cells walkable for now (for testing)
                bool walkable = true;
                
                grid[x, z] = new Node(x, z, worldPoint, walkable);
                
                Debug.Log($"[GridPathfinding] Grid cell ({x},{z}) at {worldPoint} - Walkable: {walkable}");
            }
        }
        
        Debug.Log($"[GridPathfinding] Grid initialization completed");
    }
    
    // Convert grid coordinates to world position
    public Vector3 GetWorldPosition(int x, int z)
    {
        return gridOrigin + new Vector3(cellSize * (x + 0.5f), gridOrigin.y, cellSize * (z + 0.5f));
    }
    
    // Convert world position to grid coordinates
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - gridOrigin;
        int x = Mathf.FloorToInt((localPos.x / cellSize) - 0.5f);
        int z = Mathf.FloorToInt((localPos.z / cellSize) - 0.5f);
        
        x = Mathf.Clamp(x, 0, gridWidth - 1);
        z = Mathf.Clamp(z, 0, gridHeight - 1);
        
        return new Vector2Int(x, z);
    }
    
    // Get walkable neighbors of a node (only horizontal and vertical, no diagonal)
    List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        
        // Check 4 directions: up, right, down, left (no diagonal)
        int[] dx = { 0, 1, 0, -1 };
        int[] dz = { -1, 0, 1, 0 };
        
        for (int i = 0; i < 4; i++)
        {
            int checkX = node.x + dx[i];
            int checkZ = node.z + dz[i];
            
            if (checkX >= 0 && checkX < gridWidth && checkZ >= 0 && checkZ < gridHeight)
            {
                if (grid[checkX, checkZ].walkable)
                {
                    neighbors.Add(grid[checkX, checkZ]);
                }
            }
        }
        
        return neighbors;
    }
    
    // Calculate heuristic distance between two nodes (Manhattan distance for grid-based movement)
    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.x - nodeB.x);
        int dstZ = Mathf.Abs(nodeA.z - nodeB.z);
        
        // Since we only move horizontally and vertically, use Manhattan distance
        return dstX + dstZ;
    }
    
    // Simple grid-based pathfinding algorithm
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Debug.Log($"[GridPathfinding] FindPath called: {startPos} -> {targetPos}");
        
        Vector2Int startGrid = GetGridPosition(startPos);
        Vector2Int targetGrid = GetGridPosition(targetPos);
        
        Debug.Log($"[GridPathfinding] Grid positions - Start: {startGrid}, Target: {targetGrid}");
        Debug.Log($"[GridPathfinding] Grid bounds: {gridWidth}x{gridHeight}");
        
        // Ensure we're within grid bounds
        if (startGrid.x < 0 || startGrid.x >= gridWidth || startGrid.y < 0 || startGrid.y >= gridHeight ||
            targetGrid.x < 0 || targetGrid.x >= gridWidth || targetGrid.y < 0 || targetGrid.y >= gridHeight)
        {
            Debug.LogWarning($"[GridPathfinding] Start or target position outside grid bounds! Start: {startGrid}, Target: {targetGrid}, Grid: {gridWidth}x{gridHeight}");
            return null;
        }
        
        // Create a simple path: start -> intermediate grid cells -> target
        List<Vector3> path = new List<Vector3>();
        
        // Add start position
        path.Add(startPos);
        
        // Add intermediate grid cells
        int currentX = startGrid.x;
        int currentZ = startGrid.y;
        
        // Move horizontally first
        while (currentX != targetGrid.x)
        {
            if (currentX < targetGrid.x) currentX++;
            else currentX--;
            
            Vector3 intermediatePos = GetWorldPosition(currentX, currentZ);
            path.Add(intermediatePos);
            Debug.Log($"[GridPathfinding] Added horizontal waypoint: ({currentX},{currentZ}) at {intermediatePos}");
        }
        
        // Then move vertically
        while (currentZ != targetGrid.y)
        {
            if (currentZ < targetGrid.y) currentZ++;
            else currentZ--;
            
            Vector3 intermediatePos = GetWorldPosition(currentX, currentZ);
            path.Add(intermediatePos);
            Debug.Log($"[GridPathfinding] Added vertical waypoint: ({currentX},{currentZ}) at {intermediatePos}");
        }
        
        // Add target position
        path.Add(targetPos);
        
        Debug.Log($"[GridPathfinding] Simple path created with {path.Count} waypoints");
        
        // Log all waypoints
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"[GridPathfinding] Waypoint {i}: {path[i]}");
        }
        
        return path;
    }
    
    // Retrace path from end to start
    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;
        
        // Build path from end to start
        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }
        
        // Add start position
        path.Add(startNode.worldPosition);
        
        // Reverse to get path from start to end
        path.Reverse();
        
        // Don't optimize - keep all grid waypoints for grid-based movement
        Debug.Log($"[GridPathfinding] Path created with {path.Count} waypoints (no optimization)");
        
        return path;
    }
    
    // Check if a position is walkable
    public bool IsPositionWalkable(Vector3 position)
    {
        Vector2Int gridPos = GetGridPosition(position);
        
        if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight)
            return false;
            
        return grid[gridPos.x, gridPos.y].walkable;
    }
    
    // Move character along path
    public IEnumerator<WaitForEndOfFrame> MoveAlongPath(Transform character, List<Vector3> path)
    {
        if (path == null || path.Count == 0)
            yield break;
            
        int currentWaypointIndex = 0;
        
        while (currentWaypointIndex < path.Count)
        {
            Vector3 targetWaypoint = path[currentWaypointIndex];
            
            // Move towards waypoint
            while (Vector3.Distance(character.position, targetWaypoint) > stoppingDistance)
            {
                Vector3 direction = (targetWaypoint - character.position).normalized;
                character.position += direction * moveSpeed * Time.deltaTime;
                
                // Rotate towards movement direction
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    character.rotation = Quaternion.Slerp(character.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                yield return new WaitForEndOfFrame();
            }
            
            currentWaypointIndex++;
        }
        
        Debug.Log("Path completed!");
    }
    
    // Public method to start pathfinding for a character
    public void StartPathfinding(Transform character, Vector3 targetPosition)
    {
        List<Vector3> path = FindPath(character.position, targetPosition);
        
        if (path != null)
        {
            StartCoroutine(MoveAlongPath(character, path));
        }
        else
        {
            Debug.LogWarning($"No path found from {character.position} to {targetPosition}");
        }
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugPath) return;
        
        // Draw grid boundaries
        Gizmos.color = Color.blue;
        Vector3 gridSize = new Vector3(gridWidth * cellSize, 0.1f, gridHeight * cellSize);
        Vector3 gridCenter = gridOrigin + gridSize * 0.5f;
        Gizmos.DrawWireCube(gridCenter, gridSize);
        
        // Draw grid info text (for debugging)
        #if UNITY_EDITOR
        if (showGridInfo)
        {
            UnityEditor.Handles.Label(gridCenter + Vector3.up * 2f, 
                $"Pathfinding Grid: {gridWidth}x{gridHeight}\nCell Size: {cellSize:F2}\nTotal Size: {gridSize.x:F1}x{gridSize.z:F1}\nOrigin: {gridOrigin}");
        }
        #endif
        
        // Draw grid cells and centers
        if (grid != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 cellCenter = GetWorldPosition(x, z);
                    
                    // Draw cell outline
                    if (grid[x, z] != null)
                    {
                        Gizmos.color = grid[x, z].walkable ? walkableCellColor : blockedCellColor;
                    }
                    else
                    {
                        Gizmos.color = Color.gray;
                    }
                    
                    Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
                    
                    // Draw cell center point
                    if (showCellCenters)
                    {
                        Gizmos.color = cellCenterColor;
                        Gizmos.DrawSphere(cellCenter, 0.1f);
                    }
                    
                    // Draw cell coordinates for debugging
                    #if UNITY_EDITOR
                    if (showGridInfo)
                    {
                        UnityEditor.Handles.Label(cellCenter + Vector3.up * 0.2f, $"({x},{z})");
                    }
                    #endif
                }
            }
        }
        else
        {
            // Draw grid cells even when grid is not initialized (for editor)
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 cellCenter = GetWorldPosition(x, z);
                    
                    // Draw cell outline
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
                    
                    // Draw cell center point
                    if (showCellCenters)
                    {
                        Gizmos.color = cellCenterColor;
                        Gizmos.DrawSphere(cellCenter, 0.1f);
                    }
                    
                    // Draw cell coordinates for debugging
                    #if UNITY_EDITOR
                    if (showGridInfo)
                    {
                        UnityEditor.Handles.Label(cellCenter + Vector3.up * 0.2f, $"({x},{z})");
                    }
                    #endif
                }
            }
        }
    }
    
    // Update grid walkability (call this when obstacles change)
    public void UpdateGridWalkability()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPoint = GetWorldPosition(x, z);
                
                // Check if this grid cell is walkable
                bool walkable = !Physics.CheckSphere(worldPoint, cellSize * 0.3f, obstacleLayer);
                
                grid[x, z].walkable = walkable;
            }
        }
    }
}
