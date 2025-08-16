using System.Collections.Generic;
using UnityEngine;

public class GridPathfinding : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("Pathfinding Settings")]
    public LayerMask obstacleLayer = -1;
    public float raycastHeight = 1f;
    public bool showDebugPath = true;
    public bool showCellCenters = true;
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
        grid = new Node[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPoint = GetWorldPosition(x, z);
                
                // Check if this grid cell is walkable
                // Use a smaller radius to avoid false positives
                bool walkable = !Physics.CheckSphere(worldPoint, cellSize * 0.3f, obstacleLayer);
                
                // Additional check: make sure the cell is not too close to obstacles
                if (walkable)
                {
                    // Check if there are obstacles above the cell
                    if (Physics.Raycast(worldPoint + Vector3.up * 0.1f, Vector3.down, 1f, obstacleLayer))
                    {
                        walkable = false;
                    }
                }
                
                grid[x, z] = new Node(x, z, worldPoint, walkable);
            }
        }
    }
    
    // Convert grid coordinates to world position
    public Vector3 GetWorldPosition(int x, int z)
    {
        // This matches the GridGenerator's cell position calculation exactly
        // GridGenerator uses: startPos + new Vector3(cellWidth * (x + 0.5f), 0, cellHeight * (z + 0.5f))
        // Where startPos = transform.position - new Vector3(planeSize.x * 0.5f, 0f, planeSize.z * 0.5f)
        // So we need to match: gridOrigin + new Vector3(cellSize * (x + 0.5f), 0, cellSize * (z + 0.5f))
        return gridOrigin + new Vector3(cellSize * (x + 0.5f), 0, cellSize * (z + 0.5f));
    }
    
    // Convert world position to grid coordinates
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - gridOrigin;
        // Since we use (x + 0.5f) in GetWorldPosition, we need to subtract 0.5f here
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
    
    // A* Pathfinding algorithm
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Vector2Int startGrid = GetGridPosition(startPos);
        Vector2Int targetGrid = GetGridPosition(targetPos);
        
        // Ensure we're within grid bounds
        if (startGrid.x < 0 || startGrid.x >= gridWidth || startGrid.y < 0 || startGrid.y >= gridHeight ||
            targetGrid.x < 0 || targetGrid.x >= gridWidth || targetGrid.y < 0 || targetGrid.y >= gridHeight)
        {
            Debug.LogWarning($"Start or target position outside grid bounds! Start: {startGrid}, Target: {targetGrid}, Grid: {gridWidth}x{gridHeight}");
            return null;
        }
        
        Node startNode = grid[startGrid.x, startGrid.y];
        Node targetNode = grid[targetGrid.x, targetGrid.y];
        
        if (!startNode.walkable || !targetNode.walkable)
        {
            Debug.LogWarning("Start or target position is not walkable!");
            return null;
        }
        
        // Initialize A* algorithm
        openList.Clear();
        closedList.Clear();
        
        // Reset all nodes
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                grid[x, z].gCost = int.MaxValue;
                grid[x, z].hCost = 0;
                grid[x, z].parent = null;
            }
        }
        
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        openList.Add(startNode);
        
        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            
            // Find node with lowest fCost
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost || 
                    (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                {
                    currentNode = openList[i];
                }
            }
            
            openList.Remove(currentNode);
            closedList.Add(currentNode);
            
            // Path found
            if (currentNode == targetNode)
            {
                var path = RetracePath(startNode, targetNode);
                return path;
            }
            
            // Check neighbors
            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedList.Contains(neighbor))
                    continue;
                
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                
                if (newMovementCostToNeighbor < neighbor.gCost || !openList.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;
                    
                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }
        
        Debug.LogWarning("No path found!");
        return null;
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
        
        // Optimize path by removing unnecessary waypoints
        path = OptimizePath(path);
        
        return path;
    }
    
    // Optimize path by removing unnecessary waypoints (keep more waypoints for grid-based movement)
    List<Vector3> OptimizePath(List<Vector3> originalPath)
    {
        if (originalPath.Count <= 2) return originalPath;
        
        List<Vector3> optimizedPath = new List<Vector3>();
        optimizedPath.Add(originalPath[0]); // Add start
        
        for (int i = 1; i < originalPath.Count - 1; i++)
        {
            Vector3 prev = originalPath[i - 1];
            Vector3 current = originalPath[i];
            Vector3 next = originalPath[i + 1];
            
            // Always add the current waypoint
            optimizedPath.Add(current);
            
            // Add intermediate waypoints for smoother grid movement
            if (i < originalPath.Count - 2)
            {
                Vector3 direction = (next - current).normalized;
                Vector3 intermediate = current + direction * (cellSize * 0.5f);
                optimizedPath.Add(intermediate);
            }
        }
        
        optimizedPath.Add(originalPath[originalPath.Count - 1]); // Add end
        
        return optimizedPath;
    }
    
    // Check if a position is walkable using raycast
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
        Gizmos.DrawWireCube(gridOrigin + gridSize * 0.5f, gridSize);
        
        // Draw grid info text (for debugging)
        #if UNITY_EDITOR
        if (grid != null)
        {
            Vector3 centerPos = gridOrigin + gridSize * 0.5f;
            UnityEditor.Handles.Label(centerPos + Vector3.up * 2f, 
                $"Pathfinding Grid: {gridWidth}x{gridHeight}\nCell Size: {cellSize:F2}\nTotal Size: {gridSize.x:F1}x{gridSize.z:F1}");
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
                
                // Additional check: make sure the cell is not too close to obstacles
                if (walkable)
                {
                    // Check if there are obstacles above the cell
                    if (Physics.Raycast(worldPoint + Vector3.up * 0.1f, Vector3.down, 1f, obstacleLayer))
                    {
                        walkable = false;
                    }
                }
                
                grid[x, z].walkable = walkable;
            }
        }
        
        Debug.Log("Grid walkability updated");
    }
}
