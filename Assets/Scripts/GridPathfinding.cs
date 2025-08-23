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
    
    [Header("Obstacle Detection")]
    public float rayHeight = 5f;
    public LayerMask characterLayer = 1 << 8; // Character layer (8)
    public bool useRayDetection = true;
    
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
        // Set up character layer properly
        int characterLayerIndex = LayerMask.NameToLayer("Character");
        if (characterLayerIndex != -1)
        {
            characterLayer = 1 << characterLayerIndex;
            Debug.Log($"[GridPathfinding] Character layer set to: {characterLayerIndex} (mask: {characterLayer})");
        }
        else
        {
            Debug.LogWarning("[GridPathfinding] Character layer not found! Using default layer 8");
            characterLayer = 1 << 8;
        }
        
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
                
                // Check if cell is walkable using ray detection
                bool walkable = CheckCellWalkability(worldPoint);
                
                grid[x, z] = new Node(x, z, worldPoint, walkable);
                
                Debug.Log($"[GridPathfinding] Grid cell ({x},{z}) at {worldPoint} - Walkable: {walkable}");
            }
        }
        
        Debug.Log($"[GridPathfinding] Grid initialization completed");
    }
    
    // Update walkability of a specific grid cell
    public void UpdateCellWalkability(int x, int z)
    {
        if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight)
            return;
            
        Vector3 worldPoint = GetWorldPosition(x, z);
        bool walkable = CheckCellWalkability(worldPoint);
        
        if (grid[x, z].walkable != walkable)
        {
            grid[x, z].walkable = walkable;
            Debug.Log($"[GridPathfinding] Updated cell ({x},{z}) walkability: {walkable}");
        }
    }
    
    // Update walkability of a cell at world position
    public void UpdateCellWalkability(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridPosition(worldPosition);
        UpdateCellWalkability(gridPos.x, gridPos.y);
    }
    
    // Refresh entire grid walkability
    public void RefreshGridWalkability()
    {
        Debug.Log("[GridPathfinding] Refreshing grid walkability...");
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                UpdateCellWalkability(x, z);
            }
        }
        Debug.Log("[GridPathfinding] Grid walkability refresh completed");
    }
    
    // Context menu for testing
    [ContextMenu("Test Grid Walkability")]
    void TestGridWalkability()
    {
        Debug.Log("[GridPathfinding] Testing grid walkability...");
        Debug.Log($"Character layer mask: {characterLayer}");
        Debug.Log($"Ray height: {rayHeight}");
        Debug.Log($"Use ray detection: {useRayDetection}");
        
        if (grid != null)
        {
            int walkableCount = 0;
            int blockedCount = 0;
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (grid[x, z].walkable)
                        walkableCount++;
                    else
                        blockedCount++;
                }
            }
            
            Debug.Log($"Grid stats: {walkableCount} walkable, {blockedCount} blocked out of {gridWidth * gridHeight} total cells");
        }
        else
        {
            Debug.LogWarning("Grid is null!");
        }
    }
    
    [ContextMenu("Refresh Grid")]
    void RefreshGrid()
    {
        RefreshGridWalkability();
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
    
    // Check if a grid cell is walkable using ray detection
    bool CheckCellWalkability(Vector3 cellPosition)
    {
        if (!useRayDetection)
        {
            Debug.Log($"[GridPathfinding] Ray detection disabled for cell at {cellPosition}");
            return true;
        }
            
        // Cast ray upward from cell position
        Vector3 rayStart = cellPosition;
        Vector3 rayDirection = Vector3.up;
        
        Debug.Log($"[GridPathfinding] Checking cell at {cellPosition} with ray: {rayStart} -> {rayStart + rayDirection * rayHeight}");
        Debug.Log($"[GridPathfinding] Character layer mask: {characterLayer}");
        
        // Check for Character layer objects
        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, rayHeight, characterLayer))
        {
            Debug.Log($"[GridPathfinding] Cell at {cellPosition} BLOCKED by Character object: {hit.collider.gameObject.name} at layer {hit.collider.gameObject.layer}");
            return false; // Cell is blocked
        }
        
        Debug.Log($"[GridPathfinding] Cell at {cellPosition} is WALKABLE - no Character objects found");
        return true; // Cell is walkable
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
        
        // Check if start and target cells are walkable
        if (!grid[startGrid.x, startGrid.y].walkable)
        {
            Debug.LogWarning($"[GridPathfinding] Start cell ({startGrid.x},{startGrid.y}) is not walkable!");
            return null;
        }
        
        if (!grid[targetGrid.x, targetGrid.y].walkable)
        {
            Debug.LogWarning($"[GridPathfinding] Target cell ({targetGrid.x},{targetGrid.y}) is not walkable!");
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
            
            // Check if this cell is walkable
            if (currentX >= 0 && currentX < gridWidth && currentZ >= 0 && currentZ < gridHeight)
            {
                if (!grid[currentX, currentZ].walkable)
                {
                    Debug.LogWarning($"[GridPathfinding] Horizontal path blocked at ({currentX},{currentZ}) - cell is not walkable!");
                    return null; // Path is blocked
                }
            }
            
            Vector3 intermediatePos = GetWorldPosition(currentX, currentZ);
            path.Add(intermediatePos);
            Debug.Log($"[GridPathfinding] Added horizontal waypoint: ({currentX},{currentZ}) at {intermediatePos}");
        }
        
        // Then move vertically
        while (currentZ != targetGrid.y)
        {
            if (currentZ < targetGrid.y) currentZ++;
            else currentZ--;
            
            // Check if this cell is walkable
            if (currentX >= 0 && currentX < gridWidth && currentZ >= 0 && currentZ < gridHeight)
            {
                if (!grid[currentX, currentZ].walkable)
                {
                    Debug.LogWarning($"[GridPathfinding] Vertical path blocked at ({currentX},{currentZ}) - cell is not walkable!");
                    return null; // Path is blocked
                }
            }
            
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
        //#if UNITY_EDITOR
        //if (showGridInfo)
        //{
        //    UnityEditor.Handles.Label(gridCenter + Vector3.up * 2f, 
        //        $"Pathfinding Grid: {gridWidth}x{gridHeight}\nCell Size: {cellSize:F2}\nTotal Size: {gridSize.x:F1}x{gridSize.z:F1}\nOrigin: {gridOrigin}");
        //}
        //#endif
        
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
                    
                    // Draw obstacle detection ray
                    if (useRayDetection)
                    {
                        Gizmos.color = grid[x, z].walkable ? Color.green : Color.red;
                        Vector3 rayStart = cellCenter;
                        Vector3 rayEnd = rayStart + Vector3.up * rayHeight;
                        Gizmos.DrawLine(rayStart, rayEnd);
                        
                        // Draw ray endpoint
                        Gizmos.DrawSphere(rayEnd, 0.05f);
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

    // Alternative pathfinding when direct path is blocked
    public List<Vector3> FindAlternativePath(Vector3 startPos, Vector3 targetPos)
    {
        Debug.Log($"[GridPathfinding] Trying alternative pathfinding: {startPos} -> {targetPos}");
        
        Vector2Int startGrid = GetGridPosition(startPos);
        Vector2Int targetGrid = GetGridPosition(targetPos);
        
        // Try different path strategies
        List<Vector3> path = TryVerticalFirstPath(startGrid, targetGrid);
        if (path != null)
        {
            path.Insert(0, startPos);
            path.Add(targetPos);
            Debug.Log($"[GridPathfinding] Alternative path found (vertical first) with {path.Count} waypoints");
            return path;
        }
        
        // If still no path, try to find any walkable path
        path = FindAnyWalkablePath(startGrid, targetGrid);
        if (path != null)
        {
            path.Insert(0, startPos);
            path.Add(targetPos);
            Debug.Log($"[GridPathfinding] Any walkable path found with {path.Count} waypoints");
            return path;
        }
        
        Debug.LogWarning($"[GridPathfinding] No alternative path found from {startPos} to {targetPos}");
        return null;
    }
    
    // Try vertical-first path
    List<Vector3> TryVerticalFirstPath(Vector2Int start, Vector2Int target)
    {
        List<Vector3> path = new List<Vector3>();
        int currentX = start.x;
        int currentZ = start.y;
        
        // Move vertically first
        while (currentZ != target.y)
        {
            if (currentZ < target.y) currentZ++;
            else currentZ--;
            
            if (currentX >= 0 && currentX < gridWidth && currentZ >= 0 && currentZ < gridHeight)
            {
                if (!grid[currentX, currentZ].walkable)
                {
                    return null; // Path blocked
                }
            }
            
            path.Add(GetWorldPosition(currentX, currentZ));
        }
        
        // Then move horizontally
        while (currentX != target.x)
        {
            if (currentX < target.x) currentX++;
            else currentX--;
            
            if (currentX >= 0 && currentX < gridWidth && currentZ >= 0 && currentZ < gridHeight)
            {
                if (!grid[currentX, currentZ].walkable)
                {
                    return null; // Path blocked
                }
            }
            
            path.Add(GetWorldPosition(currentX, currentZ));
        }
        
        return path;
    }
    
    // Find any walkable path using simple A* approach
    List<Vector3> FindAnyWalkablePath(Vector2Int start, Vector2Int target)
    {
        // Simple flood fill approach
        bool[,] visited = new bool[gridWidth, gridHeight];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        
        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            if (current == target)
            {
                // Reconstruct path
                return ReconstructPath(cameFrom, start, target);
            }
            
            // Check neighbors
            int[] dx = { 0, 1, 0, -1 };
            int[] dz = { -1, 0, 1, 0 };
            
            for (int i = 0; i < 4; i++)
            {
                int newX = current.x + dx[i];
                int newZ = current.y + dz[i];
                
                if (newX >= 0 && newX < gridWidth && newZ >= 0 && newZ < gridHeight)
                {
                    if (!visited[newX, newZ] && grid[newX, newZ].walkable)
                    {
                        visited[newX, newZ] = true;
                        queue.Enqueue(new Vector2Int(newX, newZ));
                        cameFrom[new Vector2Int(newX, newZ)] = current;
                    }
                }
            }
        }
        
        return null; // No path found
    }
    
    // Reconstruct path from cameFrom dictionary
    List<Vector3> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int target)
    {
        List<Vector3> path = new List<Vector3>();
        Vector2Int current = target;
        
        while (current != start)
        {
            path.Add(GetWorldPosition(current.x, current.y));
            current = cameFrom[current];
        }
        
        path.Reverse();
        return path;
    }
}
