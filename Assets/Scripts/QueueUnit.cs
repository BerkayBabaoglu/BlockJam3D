using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class QueueUnit : MonoBehaviour
{
    [Tooltip("K, S, M, Y")]
    public string colorCode;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float arrivalThreshold = 0.01f;
    public bool useSmoothMovement = true;
    
    [Header("Pathfinding Settings")]
    public bool useGridPathfinding = true;
    private GridPathfinding pathfinding;
    private List<Vector3> currentPath;
    private int currentWaypointIndex;
    private bool isFollowingPath = false;

    [Header("Selection System")]
    public bool isSelected = false;
    private static QueueUnit selectedUnit = null;
    private Material originalMaterial;
    private Renderer unitRenderer;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    public int CurrentIndex { get; private set; } = -1;

    private RayKontrol rayKontrol;
    private Animator animator;
    public bool IsMoving { get; private set; }

    private bool hasJoined = false;
    Coroutine moveRoutine;
    Collider col;
    
    //pozisyon kitleme icin
    private Vector3 lockedPosition;
    private bool isPositionLocked = false;
    private Coroutine positionLockRoutine;
    

    


    void Awake()
    {
        col = GetComponent<Collider>();
        rayKontrol = GetComponent<RayKontrol>();
        animator = GetComponent<Animator>();
        unitRenderer = GetComponent<Renderer>();

        if (useGridPathfinding)
        {
            pathfinding = FindObjectOfType<GridPathfinding>();
            if (pathfinding != null)
            {
                Debug.Log($"[{gameObject.name}] GridPathfinding found: {pathfinding.name}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] GridPathfinding not found! Will use direct movement.");
            }
        }
        
        // Fix physics settings to prevent characters from flying and colliding
        FixPhysicsSettings();
        
        // Additional collision prevention measures only if not selected
        if (!isSelected)
        {
            PreventAllCollisions();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
        
        Debug.Log($"[{gameObject.name}] Awake - Collider: {(col != null ? "OK" : "MISSING")}, RayKontrol: {(rayKontrol != null ? "OK" : "MISSING")}, Animator: {(animator != null ? "OK" : "MISSING")}");
    }
    
    void Start()
    {
        Debug.Log($"[{gameObject.name}] Start() called - setting up smart collision prevention");
        
        // Save original material for selection system
        if (unitRenderer != null && unitRenderer.material != null)
        {
            originalMaterial = unitRenderer.material;
            normalColor = originalMaterial.color;
        }
        
        // Only ensure QueueUnit collisions are ignored if not selected, preserve ray control and movement
        if (!isSelected)
        {
            EnsureNoCollisions();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
    }
    
    void OnEnable()
    {
        // Only ensure QueueUnit collisions are ignored when object becomes active if not selected
        if (col != null && !isSelected)
        {
            SetupCollisionLayers();
            Debug.Log($"[{gameObject.name}] OnEnable: QueueUnit collision prevention applied");
        }
        else if (col != null && isSelected)
        {
            Debug.Log($"[{gameObject.name}] OnEnable: Unit is selected - collision prevention handled by selection system");
        }
    }
    
    void EnsureNoCollisions()
    {
        // Only prevent collisions between QueueUnit objects if this unit is NOT selected
        if (col != null && !isSelected)
        {
            // Keep collider enabled and not a trigger for ray control and movement
            col.enabled = true;
            col.isTrigger = false;
        }
        else if (col != null && isSelected)
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Keep physics enabled for movement
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = false;
        }
        
        // Re-apply collision ignoring ONLY with other QueueUnit objects if not selected
        if (col != null && !isSelected)
        {
            QueueUnit[] allUnits = FindObjectsOfType<QueueUnit>();
            foreach (QueueUnit unit in allUnits)
            {
                if (unit != this && unit.col != null)
                {
                    Physics.IgnoreCollision(col, unit.col, true);
                }
            }
            Debug.Log($"[{gameObject.name}] Start: Only QueueUnit collisions ignored, ray control and movement preserved");
        }
    }
    
    void FixPhysicsSettings()
    {
        // Get or add Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Set physics properties to prevent flying but allow movement
        rb.mass = 1f;
        rb.linearDamping = 2f; // Moderate drag to allow movement but prevent flying
        rb.angularDamping = 5f; // High angular drag to prevent spinning
        rb.useGravity = false; // No gravity to prevent falling
        rb.isKinematic = false; // Allow physics for movement but control it
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection for movement
        
        // Constrain rotation and Y position to prevent falling and Y movement
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        
        // Keep collision detection enabled for ray control and movement
        rb.detectCollisions = true;
        
        Debug.Log($"[{gameObject.name}] Physics settings fixed - Mass: {rb.mass}, Drag: {rb.linearDamping}, Constraints: {rb.constraints}, DetectCollisions: {rb.detectCollisions}");
        
        // Set up layer and tag for collision management only if not selected
        if (!isSelected)
        {
            SetupCollisionLayers();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
    }
    
    void PreventAllCollisions()
    {
        // Only prevent collisions between QueueUnit objects if this unit is NOT selected
        if (col != null && !isSelected)
        {
            // Keep collider enabled for ray control and movement
            col.enabled = true;
            // Don't make it a trigger - keep normal collision detection
            col.isTrigger = false;
            
            Debug.Log($"[{gameObject.name}] Collider kept enabled for ray control and movement");
        }
        else if (col != null && isSelected)
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
        
        // Keep rigidbody collision detection enabled for movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.detectCollisions = true;
            Debug.Log($"[{gameObject.name}] Rigidbody collision detection kept enabled for movement");
        }
        
        // Set up proper collision ignoring with other QueueUnit objects only
        SetupCollisionLayers();
    }
    
    void SetupCollisionLayers()
    {
        // Keep the existing layer (Character) - don't change it
        Debug.Log($"[{gameObject.name}] Keeping existing layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        // Keep the existing tag - don't change it
        Debug.Log($"[{gameObject.name}] Keeping existing tag: {gameObject.tag}");
        
        // Only disable collisions between QueueUnit objects if this unit is NOT selected
        if (col != null && !isSelected)
        {
            // Keep collider enabled and not a trigger for ray control and movement
            col.enabled = true;
            col.isTrigger = false;
            
            // Find all other QueueUnit objects and ignore collisions ONLY with them
            QueueUnit[] allUnits = FindObjectsOfType<QueueUnit>();
            foreach (QueueUnit unit in allUnits)
            {
                if (unit != this && unit.col != null)
                {
                    Physics.IgnoreCollision(col, unit.col, true);
                    Debug.Log($"[{gameObject.name}] Collision ignored with other QueueUnit: {unit.gameObject.name}");
                }
            }
            
            Debug.Log($"[{gameObject.name}] Collider kept enabled for ray control, collisions ignored ONLY with other QueueUnit objects");
        }
        else if (col != null && isSelected)
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
    }

    void OnMouseDown()
    {
        Debug.Log($"[{gameObject.name}] OnMouseDown çağrıldı - IsMoving: {IsMoving}, hasJoined: {hasJoined}");
        
        // Handle selection first
        SelectThisUnit();
        
        if (IsMoving) return;

        if (rayKontrol != null && rayKontrol.isMovementLocked)
        {
            Debug.Log($"[{gameObject.name}] Hareket kilitli - CantMove animasyonu oynatılıyor");
            if (animator != null)
            {
                animator.SetBool("CantMove", true);
                StartCoroutine(ResetCantMove());
            }
            return;
        }
        else if (rayKontrol == null)
        {
            Debug.LogWarning($"[{gameObject.name}] RayKontrol component'i bulunamadı - hareket kontrolü yapılamıyor!");
        }

        if (hasJoined) return; // zaten kuyrukta ise

        Debug.Log($"[{gameObject.name}] Kuyruğa katılmaya çalışıyor - colorCode: {colorCode}");

        if (QueueManager.Instance != null)
        {
            int index;
            if (QueueManager.Instance.TryInsert(gameObject, colorCode, out index))
            {
                hasJoined = true;
                Debug.Log($"[{gameObject.name}] Başarıyla kuyruğa eklendi - index: {index}");
                
                if (animator != null)
                {
                    animator.Play("ArmatureNotMe", 0, 0f);
                }

                StartCoroutine(JoinFeedback());
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Kuyruğa eklenemedi!");
                QueueManager.Instance.ResetUnitJoinedStatus(gameObject);
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] QueueManager.Instance bulunamadı! QueueManager script'i sahneye eklenmiş mi?");
        }
    }
    
    IEnumerator ResetCantMove()
    {
        yield return new WaitForSeconds(0.5f); 
        if (animator != null)
            animator.SetBool("CantMove", false);
    }

    // Selection System Methods
    void SelectThisUnit()
    {
        // Deselect previous unit if any
        if (selectedUnit != null && selectedUnit != this)
        {
            selectedUnit.Deselect();
        }
        
        // Select this unit
        isSelected = true;
        selectedUnit = this;
        
        // Visual feedback
        if (unitRenderer != null)
        {
            unitRenderer.material.color = selectedColor;
        }
        
        // Apply collision prevention only for selected unit
        ApplyCollisionPrevention();
        
        Debug.Log($"[{gameObject.name}] Selected - Collision prevention applied");
    }
    
    public void Deselect()
    {
        isSelected = false;
        
        // Restore normal color
        if (unitRenderer != null && originalMaterial != null)
        {
            unitRenderer.material.color = normalColor;
        }
        
        // Remove collision prevention
        RemoveCollisionPrevention();
        
        Debug.Log($"[{gameObject.name}] Deselected - Collision prevention removed");
    }
    
    void ApplyCollisionPrevention()
    {
        if (col != null)
        {
            // Find all other QueueUnit objects and ignore collisions with them
            QueueUnit[] allUnits = FindObjectsOfType<QueueUnit>();
            foreach (QueueUnit unit in allUnits)
            {
                if (unit != this && unit.col != null)
                {
                    Physics.IgnoreCollision(col, unit.col, true);
                    Debug.Log($"[{gameObject.name}] Collision ignored with: {unit.gameObject.name}");
                }
            }
        }
    }
    
    void RemoveCollisionPrevention()
    {
        if (col != null)
        {
            // Re-enable collisions with all QueueUnit objects
            QueueUnit[] allUnits = FindObjectsOfType<QueueUnit>();
            foreach (QueueUnit unit in allUnits)
            {
                if (unit != this && unit.col != null)
                {
                    Physics.IgnoreCollision(col, unit.col, false);
                    Debug.Log($"[{gameObject.name}] Collision re-enabled with: {unit.gameObject.name}");
                }
            }
        }
    }

    public void SetIndex(int index)
    {
        Debug.Log($"[{gameObject.name}] SetIndex çağrıldı - index: {index}");

        if (rayKontrol != null && rayKontrol.isMovementLocked)
        {
            Debug.Log($"[{gameObject.name}] Hareket kilitli - SetIndex iptal edildi");
            return; // burada artik CantMove animasyonu yok
        }
        else if (rayKontrol == null)
        {
            Debug.LogWarning($"[{gameObject.name}] RayKontrol component'i bulunamadı!");
        }

        CurrentIndex = index;
        
        if (QueueManager.Instance != null)
        {
            Transform target = QueueManager.Instance.points[index];
            
            if (moveRoutine != null) StopCoroutine(moveRoutine);

            // Only use grid pathfinding if it's still enabled (first time leaving grid)
            if (useGridPathfinding && pathfinding != null)
            {
                currentPath = pathfinding.FindPath(transform.position, target.position);
                if (currentPath != null && currentPath.Count > 0)
                {
                    Debug.Log($"[{gameObject.name}] Grid path found with {currentPath.Count} waypoints");
                    isFollowingPath = true;
                    currentWaypointIndex = 0;
                    IsMoving = true;
                    moveRoutine = StartCoroutine(MoveAlongGridPath(target));
                    return;
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] No grid path found, using direct movement");
                }
            }

            // If grid pathfinding is disabled or no path found, use direct movement
            IsMoving = true;
            Debug.Log($"[{gameObject.name}] Direct movement başlıyor (grid pathfinding disabled) - hedef: {target.name}");
            
            if (useSmoothMovement)
                moveRoutine = StartCoroutine(MoveToSmooth(target));
            else
                moveRoutine = StartCoroutine(MoveTo(target));
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] QueueManager.Instance bulunamadı! SetIndex iptal edildi.");
            IsMoving = false;
        }
    }

    public void SetIndexDirect(int index)
    {
        Debug.Log($"[{gameObject.name}] SetIndexDirect çağrıldı - index: {index} (direct movement, no grid pathfinding)");

        CurrentIndex = index;
        
        // Explicitly disable grid pathfinding for this unit
        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding explicitly disabled for direct movement");
        
        if (QueueManager.Instance != null)
        {
            Transform target = QueueManager.Instance.points[index];
            
            // Clean up any existing coroutines first
            if (moveRoutine != null) 
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }
            
            // Clean up timeout coroutine if exists
            StopAllCoroutines();

            IsMoving = true;
            Debug.Log($"[{gameObject.name}] Direct movement başlıyor - hedef: {target.name}");
            
            // Use direct movement without grid pathfinding for queue shifting
            moveRoutine = StartCoroutineSafe(MoveToSmoothDirect(target));
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] QueueManager.Instance bulunamadı! SetIndexDirect iptal edildi.");
            IsMoving = false;
        }
    }

    public void SetIndexInstant(int index)
    {
        CurrentIndex = index;
        
        // Disable grid pathfinding since instant positioning means we're in queue system
        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding disabled for instant positioning");
        
        if (QueueManager.Instance != null)
        {
            Transform target = QueueManager.Instance.points[index];

            transform.position = target.position;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.detectCollisions = false;
            }

            if (col != null)
            {
                col.enabled = false;
            }

            lockedPosition = target.position;
            isPositionLocked = true;
            if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
            positionLockRoutine = StartCoroutine(LockPosition());
            IsMoving = false;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] QueueManager.Instance bulunamadı! SetIndexInstant iptal edildi.");
        }
    }

    public void ResetJoinedStatus()
    {
        hasJoined = false;

        if (positionLockRoutine != null)
        {
            StopCoroutine(positionLockRoutine);
            positionLockRoutine = null;
        }
        isPositionLocked = false;
        
        Debug.Log($"[{gameObject.name}] hasJoined reset edildi");
    }

    public void RemoveFromQueue()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;
        IsMoving = false;

        if (positionLockRoutine != null)
        {
            StopCoroutine(positionLockRoutine);
            positionLockRoutine = null;
        }
        isPositionLocked = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
        }

        if (col != null)
        {
            col.enabled = true;
        }

        transform.SetParent(null);
        
        gameObject.SetActive(false);
        CurrentIndex = -1;
    }

    public void DestroySelf()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;
        IsMoving = false;

        if (positionLockRoutine != null)
        {
            StopCoroutine(positionLockRoutine);
            positionLockRoutine = null;
        }
        isPositionLocked = false;


        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
        }

        if (col != null)
        {
            col.enabled = true;
        }
        
        transform.SetParent(null); //parentten cik

        Destroy(gameObject);
        CurrentIndex = -1;
    }

    IEnumerator MoveToSmooth(Transform target)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = target.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / Mathf.Max(0.01f, moveSpeed);
        float elapsed = 0f;

        // Disable collider at the START of movement
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of movement");
        }

        // Make completely kinematic during movement to prevent any physics effects
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"[{gameObject.name}] Made kinematic at START of movement");
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // Ensure exact final position
        transform.position = targetPos;

        // Keep kinematic and disable all physics permanently
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false;
        }

        // Collider already disabled at start, no need to disable again
        
        lockedPosition = targetPos;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        
        IsMoving = false;
        
        // IMPORTANT: Disable grid pathfinding after reaching destination
        // This ensures all future movements (queue shifting) happen directly without returning to grid
        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding disabled - future movements will be direct queue-based");
        
        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.OnUnitArrived(this);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] QueueManager.Instance bulunamadı! OnUnitArrived çağrılamadı.");
        }
    }

    IEnumerator MoveTo(Transform target)
    {
        Vector3 targetPos = target.position;
        
        // Disable collider at the START of movement
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of movement");
        }

        // Make completely kinematic during movement to prevent any physics effects
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"[{gameObject.name}] Made kinematic at START of movement");
        }
        
        while ((transform.position - targetPos).sqrMagnitude > arrivalThreshold * arrivalThreshold)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Ensure exact final position
        transform.position = targetPos;
        

        // Keep kinematic and disable all physics permanently
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false;
        }
        
        // Collider already disabled at start, no need to disable again

        lockedPosition = targetPos;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());

        
        IsMoving = false;
        
        // IMPORTANT: Disable grid pathfinding after reaching destination
        // This ensures all future movements (queue shifting) happen directly without returning to grid
        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding disabled - future movements will be direct queue-based");
        
        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.OnUnitArrived(this);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] QueueManager.Instance bulunamadı! OnUnitArrived çağrılamadı.");
        }
    }
    
    IEnumerator MoveAlongGridPath(Transform target)
    {
        Debug.Log($"[{gameObject.name}] Starting grid-based movement with {currentPath.Count} waypoints");

        // Disable collider at the START of movement
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of grid movement");
        }

        // Make completely kinematic during movement to prevent any physics effects
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"[{gameObject.name}] Made kinematic at START of grid movement");
        }

        // Calculate total path distance for smooth movement
        float totalDistance = 0f;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }
        totalDistance += Vector3.Distance(currentPath[currentPath.Count - 1], target.position);
        
        float elapsedTime = 0f;
        float totalDuration = totalDistance / moveSpeed;
        
        Debug.Log($"[{gameObject.name}] Total path distance: {totalDistance:F2}, Duration: {totalDuration:F2}s");

        // Smooth movement through all waypoints without stopping
        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / totalDuration;
            
            // Calculate current position along the entire path
            Vector3 currentPosition = GetPositionAlongPath(progress, currentPath, target.position);
            transform.position = currentPosition;
            
            yield return null;
        }

        // Ensure exact final position
        transform.position = target.position;
        Debug.Log($"[{gameObject.name}] Grid path completed, reached final target");

        // Keep kinematic and disable all physics permanently
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false;
        }

        // Collider already disabled at start, no need to disable again

        lockedPosition = target.position;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        IsMoving = false;
        
        // IMPORTANT: Disable grid pathfinding after leaving the grid
        // This ensures all future movements (queue shifting) happen directly without returning to grid
        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding disabled - future movements will be direct queue-based");
        
        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.OnUnitArrived(this);
        }
        
        Debug.Log($"[{gameObject.name}] Grid-based movement completed");
    }

    // Helper method to calculate position along the path at given progress
    Vector3 GetPositionAlongPath(float progress, List<Vector3> waypoints, Vector3 finalTarget)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return Vector3.Lerp(transform.position, finalTarget, progress);
        }

        // Calculate total path length
        float totalLength = 0f;
        List<float> segmentLengths = new List<float>();
        
        // Add distance from start to first waypoint
        float startToFirst = Vector3.Distance(transform.position, waypoints[0]);
        totalLength += startToFirst;
        segmentLengths.Add(startToFirst);
        
        // Add distances between waypoints
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            totalLength += segmentLength;
            segmentLengths.Add(segmentLength);
        }
        
        // Add distance from last waypoint to final target
        float lastToTarget = Vector3.Distance(waypoints[waypoints.Count - 1], finalTarget);
        totalLength += lastToTarget;
        segmentLengths.Add(lastToTarget);

        // Calculate target distance along path
        float targetDistance = progress * totalLength;
        
        // Find which segment we're in
        float currentDistance = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = waypoints.Count > 0 ? waypoints[0] : finalTarget;
        
        // Check if we're in the first segment (start to first waypoint)
        if (targetDistance <= segmentLengths[0])
        {
            float segmentProgress = targetDistance / segmentLengths[0];
            return Vector3.Lerp(startPos, endPos, segmentProgress);
        }
        currentDistance += segmentLengths[0];
        
        // Check intermediate segments
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (targetDistance <= currentDistance + segmentLengths[i + 1])
            {
                float segmentProgress = (targetDistance - currentDistance) / segmentLengths[i + 1];
                return Vector3.Lerp(waypoints[i], waypoints[i + 1], segmentProgress);
            }
            currentDistance += segmentLengths[i + 1];
        }
        
        // Must be in the last segment (last waypoint to target)
        float finalSegmentProgress = (targetDistance - currentDistance) / segmentLengths[segmentLengths.Count - 1];
        return Vector3.Lerp(waypoints[waypoints.Count - 1], finalTarget, finalSegmentProgress);
    }

    // Smooth movement for queue shifting (no grid pathfinding)
    IEnumerator MoveToSmoothDirect(Transform target)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = target.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / Mathf.Max(0.01f, moveSpeed * 2f); // 2x faster for queue shifting
        float elapsed = 0f;

        Debug.Log($"[{gameObject.name}] Smooth direct movement: {startPos} -> {targetPos}, duration: {duration:F2}s");

        // Disable collider at the START of movement
        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of movement");
        }

        // Make completely kinematic during movement to prevent any physics effects
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"[{gameObject.name}] Made kinematic at START of movement");
        }
        
        // Collider already disabled at start, no need to modify it during movement

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector3 newPosition = Vector3.Lerp(startPos, targetPos, t);
            newPosition.y = startPos.y; // Keep Y position unchanged
            transform.position = newPosition;
            yield return null;
        }

        // Ensure exact position - preserve Y position
        Vector3 finalPosition = new Vector3(targetPos.x, startPos.y, targetPos.z);
        transform.position = finalPosition;
        
        // Complete the movement
        CompleteMovement(targetPos);
        
        Debug.Log($"[{gameObject.name}] Smooth direct movement completed to {targetPos}");
    }

    // Helper function to complete movement
    void CompleteMovement(Vector3 targetPos)
    {
        // Preserve Y position when completing movement
        Vector3 finalPosition = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        
        // Ensure exact final position
        transform.position = finalPosition;
        
        // Completely disable physics and collisions after movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false; // Completely disable collision detection
            rb.linearVelocity = Vector3.zero; // Ensure no residual velocity
            Debug.Log($"[{gameObject.name}] Physics completely disabled, position locked");
        }
        
        // Keep collider disabled since it was disabled at start of movement
        if (col != null)
        {
            // Collider already disabled at start, keep it disabled
            // col.enabled = false; // Already disabled
            Debug.Log($"[{gameObject.name}] Collider remains disabled after movement completion");
        }
        
        // Ensure no collisions with other QueueUnit objects only
        if (col != null)
        {
            // Find all other QueueUnit objects and ignore collisions with them
            QueueUnit[] allUnits = FindObjectsOfType<QueueUnit>();
            foreach (QueueUnit unit in allUnits)
            {
                if (unit != this && unit.col != null)
                {
                    Physics.IgnoreCollision(col, unit.col, true);
                }
            }
            Debug.Log($"[{gameObject.name}] Collisions ignored with other QueueUnit objects only");
        }
        
        // Pozisyon kilitleme sistemini başlat - preserve Y position
        lockedPosition = finalPosition;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        IsMoving = false;
        
        // IMPORTANT: Disable grid pathfinding after reaching destination
        // This ensures all future movements (queue shifting) happen directly without returning to grid
        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding disabled - future movements will be direct queue-based");
        
        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.OnUnitArrived(this);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] QueueManager.Instance bulunamadı! OnUnitArrived çağrılamadı.");
        }
    }

    // Safe coroutine start method
    Coroutine StartCoroutineSafe(IEnumerator routine)
    {
        if (this != null && gameObject != null && gameObject.activeInHierarchy)
        {
            return StartCoroutine(routine);
        }
        return null;
    }

    IEnumerator JoinFeedback()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float duration = 0.05f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }
        transform.localScale = originalScale;
    }
    

    IEnumerator LockPosition()
    {
        while (isPositionLocked && hasJoined && !IsMoving)
        {
            // Force exact position to prevent any oscillation
            transform.position = lockedPosition;
            
            // Completely disable all physics to prevent any movement
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = Vector3.zero; // Ensure no residual velocity
                rb.isKinematic = true;
                rb.detectCollisions = false; // Ensure collisions remain disabled
                rb.useGravity = false;
            }
            
            // Keep collider disabled to prevent any physics interactions
            if (col != null)
            {
                col.enabled = false; // Keep collider disabled
            }
            
            yield return new WaitForSeconds(0.1f); // Check less frequently
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!string.IsNullOrEmpty(colorCode))
        {
            Color gizmoColor = Color.white;
            switch (colorCode)
            {
                case "K": gizmoColor = Color.red; break;
                case "S": gizmoColor = Color.green; break;
                case "M": gizmoColor = Color.blue; break;
                case "Y": gizmoColor = Color.yellow; break;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
    

    void OnDrawGizmos()
    {
        if (isFollowingPath && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentPath[currentWaypointIndex]);
            
            Gizmos.color = Color.blue;
            for (int i = currentWaypointIndex; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentPath[currentWaypointIndex], 0.4f);
            
            Gizmos.color = Color.green;
            for (int i = currentWaypointIndex + 1; i < currentPath.Count; i++)
            {
                Gizmos.DrawWireSphere(currentPath[i], 0.3f);
            }
        }
    }

    // Debug method to show current status
    [ContextMenu("Show Selection Status")]
    void ShowSelectionStatus()
    {
        Debug.Log($"[{gameObject.name}] Selection Status:");
        Debug.Log($"  Is Selected: {isSelected}");
        Debug.Log($"  Is Global Selected: {selectedUnit == this}");
        Debug.Log($"  Collider Enabled: {(col != null ? col.enabled : "NO COLLIDER")}");
        Debug.Log($"  Collider Is Trigger: {(col != null ? col.isTrigger : "NO COLLIDER")}");
        Debug.Log($"  Grid Pathfinding Enabled: {useGridPathfinding}");
        Debug.Log($"  Current Index: {CurrentIndex}");
        Debug.Log($"  Is Moving: {IsMoving}");
        Debug.Log($"  Is Following Path: {isFollowingPath}");
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"  Rigidbody Detect Collisions: {rb.detectCollisions}");
            Debug.Log($"  Rigidbody Is Kinematic: {rb.isKinematic}");
        }
    }
}
