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

    private List<Vector3> currentPath;
    private int currentWaypointIndex = 0;
    private bool isFollowingPath = false;

    void Start()
    {

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
                        ShiftRightFrom(targetIndex);
                        targetPoint = SelectQueue.Instance.queuePoints[targetIndex];
                        SelectQueue.Instance.queueColors[targetIndex] = colorCode;
                        SelectQueue.Instance.queueObjects[targetIndex] = gameObject;

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

        if (isFollowingPath && currentPath != null && currentPath.Count > 0)
        {
            UpdatePathfindingMovement();
        }

        else if (isMoving && targetPoint != null)
        {

            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPoint.position) < 0.01f)
            {
                transform.SetParent(targetPoint);
                isMoving = false;
                CheckMatchAndClear();
            }
        }
    }

    void StartPathfindingToQueue()
    {
        if (pathfinding != null && targetPoint != null)
        {

            Vector3 lastGridPosition = FindLastWalkableGridPosition(targetPoint.position);
            

            currentPath = pathfinding.FindPath(transform.position, lastGridPosition);
            
            if (currentPath != null && currentPath.Count > 0)
            {

                isFollowingPath = true;
                currentWaypointIndex = 0;
                isMoving = false;
                
                Debug.Log("Grid pathfinding started with " + currentPath.Count + " waypoints to last grid cell: " + lastGridPosition);
            }
            else
            {

                Debug.LogWarning("No grid path found, using direct movement");
                isMoving = true;
            }
        }
        else
        {

            Debug.LogWarning("No pathfinding system found, using direct movement");
            isMoving = true;
        }
    }

    Vector3 FindLastWalkableGridPosition(Vector3 queuePosition)
    {
        if (pathfinding == null) return queuePosition;

        Vector2Int queueGridPos = pathfinding.GetGridPosition(queuePosition);

        Vector3 closestWalkable = queuePosition;
        float closestDistance = float.MaxValue;

        int searchRadius = 3;
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int z = -searchRadius; z <= searchRadius; z++)
            {
                Vector2Int checkPos = new Vector2Int(queueGridPos.x + x, queueGridPos.y + z);
                

                if (checkPos.x >= 0 && checkPos.x < pathfinding.gridWidth && 
                    checkPos.y >= 0 && checkPos.y < pathfinding.gridHeight)
                {

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

        if (closestDistance == float.MaxValue)
        {
            Debug.LogWarning("No walkable grid cell found near queue, using direct movement");
            return queuePosition;
        }
        
        Debug.Log($"Found last walkable grid position: {closestWalkable} (distance: {closestDistance:F2})");
        return closestWalkable;
    }

    void UpdatePathfindingMovement()
    {
        if (currentPath == null || currentPath.Count == 0) return;
        
        Vector3 currentWaypoint = currentPath[currentWaypointIndex];
        

        Debug.Log($"Following waypoint {currentWaypointIndex + 1}/{currentPath.Count}: {currentWaypoint}");
        

        if (Vector3.Distance(transform.position, currentWaypoint) <= 0.0001f)
        {

            transform.position = currentWaypoint;
            currentWaypointIndex++;
            
            Debug.Log($"Reached waypoint {currentWaypointIndex}, moving to next...");
            

            if (currentWaypointIndex >= currentPath.Count)
            {

                Debug.Log("Grid path completed, starting direct movement to queue");
                isFollowingPath = false;
                currentPath = null;
                currentWaypointIndex = 0;
                isMoving = true;

                return;
            }
            
            currentWaypoint = currentPath[currentWaypointIndex];
        }

        Vector3 direction = (currentWaypoint - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, currentWaypoint) < 0.01f)
        {
            transform.position = currentWaypoint;
        }

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

        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == color)
                lastSameColor = i;
        }

        if (lastSameColor != -1)
        {

            if (lastSameColor + 1 < colors.Length && colors[lastSameColor + 1] == "")
                return lastSameColor + 1;

            return lastSameColor + 1;
        }

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

        if (colors[colors.Length - 1] != "")
        {
            Debug.Log("Kaydırma yapılamıyor, en sağ dolu!");
            return;
        }

        for (int i = colors.Length - 1; i > index; i--)
        {
            colors[i] = colors[i - 1];
            objs[i] = objs[i - 1];

            if (objs[i] != null)
            {
                objs[i].transform.SetParent(SelectQueue.Instance.queuePoints[i]);
                objs[i].transform.position = SelectQueue.Instance.queuePoints[i].position;
            }
        }

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
                for (int j = 0; j < 3; j++)
                {
                    if (objs[i + j] != null)
                        Destroy(objs[i + j]); 
                    colors[i + j] = "";
                    objs[i + j] = null;
                }


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


        colors[colors.Length - 1] = "";
        objs[colors.Length - 1] = null;
    }


    void OnDrawGizmosSelected()
    {

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            Gizmos.color = Color.green;
            foreach (Vector3 waypoint in currentPath)
            {
                Gizmos.DrawWireSphere(waypoint, 0.3f);
            }
        }

        if (targetPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPoint.position, 0.5f);
        }
    }
    
    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            if (currentWaypointIndex < currentPath.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentPath[currentWaypointIndex], 0.2f);
            }

            Gizmos.color = Color.green;
            for (int i = currentWaypointIndex + 1; i < currentPath.Count; i++)
            {
                Gizmos.DrawSphere(currentPath[i], 0.1f);
            }
        }
    }
}