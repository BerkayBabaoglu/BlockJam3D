using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Added missing import for List

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 0.1f;
    public bool canMove = true;
    
    [Header("Pathfinding")]
    public GridPathfinding pathfinding;
    public LayerMask obstacleLayer = -1;
    public float raycastDistance = 1f;
    
    [Header("Animation")]
    public Animator animator;
    public string walkAnimationName = "walking";
    public string idleAnimationName = "idle";
    
    private Vector3 targetPosition;
    private bool isMoving = false;
    private List<Vector3> currentPath;
    private int currentWaypointIndex = 0;
    
    void Start()
    {
        // Find pathfinding system if not assigned
        if (pathfinding == null)
        {
            pathfinding = FindObjectOfType<GridPathfinding>();
            if (pathfinding == null)
            {
                Debug.LogError("GridPathfinding not found! Please assign it or ensure it exists in the scene.");
            }
        }
        
        // Get animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Set initial position as target
        targetPosition = transform.position;
    }
    
    void Update()
    {
        if (!canMove) return;
        
        // Handle input for movement
        HandleInput();
        
        // Update movement
        UpdateMovement();
        
        // Update animations
        UpdateAnimations();
    }
    
    void HandleInput()
    {
        // Mouse click to set target position
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check if the hit point is walkable
                if (IsPositionWalkable(hit.point))
                {
                    SetTargetPosition(hit.point);
                }
                else
                {
                    Debug.Log("Target position is not walkable!");
                }
            }
        }
        
        // WASD movement for testing
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input.z += 1;
        if (Input.GetKey(KeyCode.S)) input.z -= 1;
        if (Input.GetKey(KeyCode.A)) input.x -= 1;
        if (Input.GetKey(KeyCode.D)) input.x += 1;
        
        if (input.magnitude > 0.1f && !isMoving)
        {
            Vector3 newTarget = transform.position + input.normalized * 2f;
            if (IsPositionWalkable(newTarget))
            {
                SetTargetPosition(newTarget);
            }
        }
    }
    
    void UpdateMovement()
    {
        if (isMoving && currentPath != null && currentPath.Count > 0)
        {
            Vector3 currentWaypoint = currentPath[currentWaypointIndex];
            
            // Check if we've reached the current waypoint
            if (Vector3.Distance(transform.position, currentWaypoint) <= stoppingDistance)
            {
                currentWaypointIndex++;
                
                // Check if we've reached the end of the path
                if (currentWaypointIndex >= currentPath.Count)
                {
                    // Path completed
                    isMoving = false;
                    currentPath = null;
                    currentWaypointIndex = 0;
                    return;
                }
                
                currentWaypoint = currentPath[currentWaypointIndex];
            }
            
            // Move towards current waypoint
            Vector3 direction = (currentWaypoint - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Set walking animation
        if (isMoving)
        {
            if (!string.IsNullOrEmpty(walkAnimationName))
                animator.SetBool(walkAnimationName, true);
            if (!string.IsNullOrEmpty(idleAnimationName))
                animator.SetBool(idleAnimationName, false);
        }
        else
        {
            if (!string.IsNullOrEmpty(walkAnimationName))
                animator.SetBool(walkAnimationName, false);
            if (!string.IsNullOrEmpty(idleAnimationName))
                animator.SetBool(idleAnimationName, true);
        }
    }
    
    // Set target position and start pathfinding
    public void SetTargetPosition(Vector3 newTarget)
    {
        if (!canMove || isMoving) return;
        
        targetPosition = newTarget;
        
        if (pathfinding != null)
        {
            // Use the pathfinding system to find a path
            List<Vector3> path = pathfinding.FindPath(transform.position, targetPosition);
            
            if (path != null && path.Count > 0)
            {
                StartMovement(path);
            }
            else
            {
                Debug.LogWarning("No path found to target position!");
            }
        }
        else
        {
            // Fallback to direct movement if no pathfinding system
            StartDirectMovement(targetPosition);
        }
    }
    
    // Start movement along a path
    void StartMovement(List<Vector3> path)
    {
        currentPath = path;
        currentWaypointIndex = 0;
        isMoving = true;
        
        Debug.Log($"Starting movement along path with {path.Count} waypoints");
    }
    
    // Fallback direct movement (no pathfinding)
    void StartDirectMovement(Vector3 target)
    {
        StartCoroutine(MoveDirectlyTo(target));
    }
    
    IEnumerator MoveDirectlyTo(Vector3 target)
    {
        isMoving = true;
        
        while (Vector3.Distance(transform.position, target) > stoppingDistance)
        {
            Vector3 direction = (target - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            yield return null;
        }
        
        isMoving = false;
        Debug.Log("Direct movement completed!");
    }
    
    // Check if a position is walkable using raycast
    bool IsPositionWalkable(Vector3 position)
    {
        // Check if there are obstacles at the position
        if (Physics.CheckSphere(position, 0.5f, obstacleLayer))
        {
            return false;
        }
        
        // Additional raycast check for obstacles above
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, raycastDistance, obstacleLayer))
        {
            return false;
        }
        
        return true;
    }
    
    // Stop current movement
    public void StopMovement()
    {
        isMoving = false;
        currentPath = null;
        currentWaypointIndex = 0;
    }
    
    // Check if character is currently moving
    public bool IsMoving()
    {
        return isMoving;
    }
    
    // Get current target position
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    // Set movement speed
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    // Set rotation speed
    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }
    
    // Enable/disable movement
    public void SetCanMove(bool canMove)
    {
        this.canMove = canMove;
        if (!canMove)
        {
            StopMovement();
        }
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        
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
        
        // Draw movement raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * raycastDistance);
    }
}
