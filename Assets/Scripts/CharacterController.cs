using UnityEngine;
using System.Collections.Generic;

public class CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public string colorCode; // K, S, M, Y
    
    [Header("Pathfinding")]
    public GridPathfinding pathfinding;
    public LayerMask obstacleLayer = -1;
    
    private Transform targetPoint;
    private bool isMoving = false;
    private int targetIndex = -1;
    
    // Pathfinding variables
    private List<Vector3> currentPath;
    private int currentWaypointIndex = 0;
    private bool isFollowingPath = false;

    void Start()
    {
        // Find pathfinding system if not assigned
        if (pathfinding == null)
        {
            pathfinding = FindObjectOfType<GridPathfinding>();
            if (pathfinding == null)
            {
                Debug.LogWarning("GridPathfinding not found! Character will move directly to target.");
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isMoving && !isFollowingPath)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    targetIndex = FindInsertIndexForColor(colorCode);
                    if (targetIndex != -1)
                    {
                        ShiftRightFrom(targetIndex); // Öncekileri sağa kaydır
                        targetPoint = SelectQueue.Instance.queuePoints[targetIndex];
                        SelectQueue.Instance.queueColors[targetIndex] = colorCode;
                        SelectQueue.Instance.queueObjects[targetIndex] = gameObject;
                        
                        // Start pathfinding to queue position
                        StartPathfindingToQueue();
                        
                        GetComponent<Collider>().enabled = false;
                    }
                    else
                    {
                        Debug.Log("Tüm noktalar dolu!");
                    }
                }
            }
        }

        // Update pathfinding movement
        if (isFollowingPath && currentPath != null && currentPath.Count > 0)
        {
            UpdatePathfindingMovement();
        }
        // Direct movement to queue position (after grid pathfinding is complete)
        else if (isMoving && targetPoint != null)
        {
            // Move directly to the queue position, not to any intermediate grid cell
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPoint.position) < 0.01f)
            {
                transform.SetParent(targetPoint); // Noktaya bağla
                isMoving = false;
                CheckMatchAndClear();
            }
        }
    }

    // Start pathfinding to queue position
    void StartPathfindingToQueue()
    {
        if (pathfinding != null && targetPoint != null)
        {
            // Find the last walkable grid cell before the queue position
            // This ensures the character follows grid until the last possible cell, then goes directly to queue
            Vector3 lastGridPosition = FindLastWalkableGridPosition(targetPoint.position);
            
            // First, find path to the last walkable grid position
            currentPath = pathfinding.FindPath(transform.position, lastGridPosition);
            
            if (currentPath != null && currentPath.Count > 0)
            {
                // Start following the grid-based path
                isFollowingPath = true;
                currentWaypointIndex = 0;
                isMoving = false;
                
                Debug.Log("Grid pathfinding started with " + currentPath.Count + " waypoints to last grid cell: " + lastGridPosition);
            }
            else
            {
                // If no grid path found, use direct movement
                Debug.LogWarning("No grid path found, using direct movement");
                isMoving = true;
            }
        }
        else
        {
            // Fallback to direct movement if no pathfinding system
            Debug.LogWarning("No pathfinding system found, using direct movement");
            isMoving = true;
        }
    }
    
    // Find the last walkable grid position before the queue position
    Vector3 FindLastWalkableGridPosition(Vector3 queuePosition)
    {
        if (pathfinding == null) return queuePosition;
        
        // Get the grid position of the queue
        Vector2Int queueGridPos = pathfinding.GetGridPosition(queuePosition);
        
        // Find the closest walkable grid cell to the queue
        // We'll look for a cell that's walkable and close to the queue
        Vector3 closestWalkable = queuePosition;
        float closestDistance = float.MaxValue;
        
        // Check a small area around the queue position for walkable cells
        int searchRadius = 3;
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int z = -searchRadius; z <= searchRadius; z++)
            {
                Vector2Int checkPos = new Vector2Int(queueGridPos.x + x, queueGridPos.y + z);
                
                // Check if this position is within grid bounds
                if (checkPos.x >= 0 && checkPos.x < pathfinding.gridWidth && 
                    checkPos.y >= 0 && checkPos.y < pathfinding.gridHeight)
                {
                    // Check if this position is walkable
                    if (pathfinding.IsPositionWalkable(pathfinding.GetWorldPosition(checkPos.x, checkPos.y)))
                    {
                        Vector3 worldPos = pathfinding.GetWorldPosition(checkPos.x, checkPos.y);
                        float distance = Vector3.Distance(worldPos, queuePosition);
                        
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestWalkable = worldPos;
                        }
                    }
                }
            }
        }
        
        // If no walkable grid cell found, return the original queue position
        if (closestDistance == float.MaxValue)
        {
            Debug.LogWarning("No walkable grid cell found near queue, using direct movement");
            return queuePosition;
        }
        
        Debug.Log($"Found last walkable grid position: {closestWalkable} (distance: {closestDistance:F2})");
        return closestWalkable;
    }

    // Update movement along the path
    void UpdatePathfindingMovement()
    {
        if (currentPath == null || currentPath.Count == 0) return;
        
        Vector3 currentWaypoint = currentPath[currentWaypointIndex];
        
        // Debug: Show current waypoint and progress
        Debug.Log($"Following waypoint {currentWaypointIndex + 1}/{currentPath.Count}: {currentWaypoint}");
        
        // Check if we've reached the current waypoint (with very small tolerance for precise grid movement)
        if (Vector3.Distance(transform.position, currentWaypoint) <= 0.0001f)
        {
            // Snap to exact waypoint position to ensure we're exactly on grid center
            transform.position = currentWaypoint;
            currentWaypointIndex++;
            
            Debug.Log($"Reached waypoint {currentWaypointIndex}, moving to next...");
            
            // Check if we've reached the end of the grid path
            if (currentWaypointIndex >= currentPath.Count)
            {
                // Grid path completed, now start direct movement to final queue position
                Debug.Log("Grid path completed, starting direct movement to queue");
                isFollowingPath = false;
                currentPath = null;
                currentWaypointIndex = 0;
                isMoving = true;
                
                // Ensure we start moving directly to the queue position, not to the last grid cell
                // This prevents the character from going back to the nearest grid cell
                // The character will now move directly from the last grid cell to the queue position
                return;
            }
            
            currentWaypoint = currentPath[currentWaypointIndex];
        }
        
        // Move towards current waypoint with grid-based precision
        Vector3 direction = (currentWaypoint - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        
        // Ensure we don't overshoot the waypoint
        if (Vector3.Distance(transform.position, currentWaypoint) < 0.01f)
        {
            transform.position = currentWaypoint;
        }
        
        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    int FindInsertIndexForColor(string color)
    {
        var colors = SelectQueue.Instance.queueColors;
        int lastSameColor = -1;

        // Kuyrukta en sağdaki aynı renk
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == color)
                lastSameColor = i;
        }

        if (lastSameColor != -1)
        {
            // Onun sağı boşsa oraya
            if (lastSameColor + 1 < colors.Length && colors[lastSameColor + 1] == "")
                return lastSameColor + 1;

            // Sağ doluysa araya girecek
            return lastSameColor + 1;
        }

        // Aynı renk yoksa ilk boş yer
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == "")
                return i;
        }

        return -1;
    }

    void ShiftRightFrom(int index)
    {
        var colors = SelectQueue.Instance.queueColors;
        var objs = SelectQueue.Instance.queueObjects;

        // Eğer en sağdaki yer doluysa kaydırma yapılamaz
        if (colors[colors.Length - 1] != "")
        {
            Debug.Log("Kaydırma yapılamıyor, en sağ dolu!");
            return;
        }

        // En sağdan başlayıp index'in sağındaki her şeyi bir sağa kaydır
        for (int i = colors.Length - 1; i > index; i--)
        {
            colors[i] = colors[i - 1];
            objs[i] = objs[i - 1];

            if (objs[i] != null)
            {
                objs[i].transform.SetParent(SelectQueue.Instance.queuePoints[i]);
                // Hedef pozisyona anında ışınlanmak yerine istersen animasyonla gidebilir
                objs[i].transform.position = SelectQueue.Instance.queuePoints[i].position;
            }
        }

        // Araya girecek yer boşaltıldı
        colors[index] = "";
        objs[index] = null;
    }

    void CheckMatchAndClear()
    {
        var colors = SelectQueue.Instance.queueColors;
        var objs = SelectQueue.Instance.queueObjects;

        for (int i = 0; i < colors.Length - 2; i++)
        {
            if (colors[i] != "" && colors[i] == colors[i + 1] && colors[i] == colors[i + 2])
            {
                // 3'lü eşleşmeyi temizle
                for (int j = 0; j < 3; j++)
                {
                    if (objs[i + j] != null)
                        Destroy(objs[i + j]); // sahneden sil
                    colors[i + j] = "";
                    objs[i + j] = null;
                }

                // Sağdakileri sola kaydır
                ShiftLeftFrom(i);
                break;
            }
        }
    }

    void ShiftLeftFrom(int startIndex)
    {
        var colors = SelectQueue.Instance.queueColors;
        var objs = SelectQueue.Instance.queueObjects;

        for (int i = startIndex; i < colors.Length - 1; i++)
        {
            colors[i] = colors[i + 1];
            objs[i] = objs[i + 1];

            if (objs[i] != null)
                objs[i].transform.SetParent(SelectQueue.Instance.queuePoints[i]);
        }

        // Son noktayı boşalt
        colors[colors.Length - 1] = "";
        objs[colors.Length - 1] = null;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw current path
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            
            // Draw waypoints
            Gizmos.color = Color.green;
            foreach (Vector3 waypoint in currentPath)
            {
                Gizmos.DrawWireSphere(waypoint, 0.3f);
            }
        }
        
        // Draw target position
        if (targetPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPoint.position, 0.5f);
        }
    }
    
    // Debug visualization of the current path
    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            // Draw the current path
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            
            // Draw current waypoint
            if (currentWaypointIndex < currentPath.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentPath[currentWaypointIndex], 0.2f);
            }
            
            // Draw remaining waypoints
            Gizmos.color = Color.green;
            for (int i = currentWaypointIndex + 1; i < currentPath.Count; i++)
            {
                Gizmos.DrawSphere(currentPath[i], 0.1f);
            }
        }
    }
}