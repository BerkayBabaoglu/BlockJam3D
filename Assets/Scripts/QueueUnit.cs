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

        FixPhysicsSettings();

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
        
        if (unitRenderer != null && unitRenderer.material != null)
        {
            originalMaterial = unitRenderer.material;
            normalColor = originalMaterial.color;
        }

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

        if (col != null && !isSelected)
        {
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
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = false;
        }
        
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
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.mass = 1f;
        rb.linearDamping = 2f; 
        rb.angularDamping = 5f; 
        rb.useGravity = false; 
        rb.isKinematic = false; 
        rb.interpolation = RigidbodyInterpolation.Interpolate; 
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 
        

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        

        rb.detectCollisions = true;
        
        Debug.Log($"[{gameObject.name}] Physics settings fixed - Mass: {rb.mass}, Drag: {rb.linearDamping}, Constraints: {rb.constraints}, DetectCollisions: {rb.detectCollisions}");
        

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

        if (col != null && !isSelected)
        {

            col.enabled = true;
            col.isTrigger = false;
            
            Debug.Log($"[{gameObject.name}] Collider kept enabled for ray control and movement");
        }
        else if (col != null && isSelected)
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.detectCollisions = true;
            Debug.Log($"[{gameObject.name}] Rigidbody collision detection kept enabled for movement");
        }
        
        SetupCollisionLayers();
    }
    
    void SetupCollisionLayers()
    {

        if (col != null && !isSelected)
        {

            col.enabled = true;
            col.isTrigger = false;

            QueueUnit[] allUnits = FindObjectsOfType<QueueUnit>();
            foreach (QueueUnit unit in allUnits)
            {
                if (unit != this && unit.col != null)
                {
                    Physics.IgnoreCollision(col, unit.col, true);
                }
            }
        }
        else if (col != null && isSelected)
        {
            Debug.Log($"[{gameObject.name}] Unit is selected - collision prevention handled by selection system");
        }
    }

    void OnMouseDown()
    {
        if (pathfinding != null && useGridPathfinding)
        {
            pathfinding.UpdateCellWalkability(transform.position);
            Debug.Log($"[{gameObject.name}] Updated grid walkability on click for position: {transform.position}");
        }

        SelectThisUnit();
        
        if (IsMoving) return;

        if (rayKontrol != null && rayKontrol.isMovementLocked)
        {
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

    void SelectThisUnit()
    {
        if (selectedUnit != null && selectedUnit != this)
        {
            selectedUnit.Deselect();
        }
        

        isSelected = true;
        selectedUnit = this;
        
        if (unitRenderer != null)
        {
            unitRenderer.material.color = selectedColor;
        }

        ApplyCollisionPrevention();
        
        Debug.Log($"[{gameObject.name}] Selected - Collision prevention applied");
    }
    
    public void Deselect()
    {
        isSelected = false;

        if (unitRenderer != null && originalMaterial != null)
        {
            unitRenderer.material.color = normalColor;
        }

        RemoveCollisionPrevention();
        
        Debug.Log($"[{gameObject.name}] Deselected - Collision prevention removed");
    }
    
    void ApplyCollisionPrevention()
    {
        if (col != null)
        {
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
                    Debug.LogWarning($"[{gameObject.name}] No direct grid path found, trying alternative pathfinding");

                    currentPath = pathfinding.FindAlternativePath(transform.position, target.position);
                    if (currentPath != null && currentPath.Count > 0)
                    {
                        Debug.Log($"[{gameObject.name}] Alternative grid path found with {currentPath.Count} waypoints");
                        isFollowingPath = true;
                        currentWaypointIndex = 0;
                        IsMoving = true;
                        moveRoutine = StartCoroutine(MoveAlongGridPath(target));
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[{gameObject.name}] No alternative grid path found, using direct movement");
                    }
                }
            }

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

        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding explicitly disabled for direct movement");
        
        if (QueueManager.Instance != null)
        {
            Transform target = QueueManager.Instance.points[index];

            if (moveRoutine != null) 
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }

            StopAllCoroutines();

            IsMoving = true;
            Debug.Log($"[{gameObject.name}] Direct movement başlıyor - hedef: {target.name}");

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

        if (pathfinding != null && useGridPathfinding)
        {
            pathfinding.UpdateCellWalkability(startPos);
            Debug.Log($"[{gameObject.name}] Updated grid walkability for current position: {startPos}");
        }

        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of movement");
        }

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

        transform.position = targetPos;

        if (pathfinding != null && useGridPathfinding)
        {
            pathfinding.UpdateCellWalkability(targetPos);
            Debug.Log($"[{gameObject.name}] Updated grid walkability for destination position: {targetPos}");
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false;
        }

        
        lockedPosition = targetPos;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        
        IsMoving = false;

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

        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of movement");
        }

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

        transform.position = targetPos;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false;
        }

        lockedPosition = targetPos;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());

        
        IsMoving = false;

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

        if (pathfinding != null && useGridPathfinding)
        {
            pathfinding.UpdateCellWalkability(transform.position);
            Debug.Log($"[{gameObject.name}] Updated grid walkability for current position: {transform.position}");
        }

        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of grid movement");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log($"[{gameObject.name}] Made kinematic at START of grid movement");
        }

        float totalDistance = 0f;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }
        totalDistance += Vector3.Distance(currentPath[currentPath.Count - 1], target.position);
        
        float elapsedTime = 0f;
        float totalDuration = totalDistance / moveSpeed;
        
        Debug.Log($"[{gameObject.name}] Total path distance: {totalDistance:F2}, Duration: {totalDuration:F2}s");

        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / totalDuration;

            Vector3 currentPosition = GetPositionAlongPath(progress, currentPath, target.position);
            transform.position = currentPosition;
            
            yield return null;
        }

        transform.position = target.position;
        Debug.Log($"[{gameObject.name}] Grid path completed, reached final target");

        if (pathfinding != null && useGridPathfinding)
        {
            pathfinding.UpdateCellWalkability(target.position);
            Debug.Log($"[{gameObject.name}] Updated grid walkability for destination position: {target.position}");
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false;
        }


        lockedPosition = target.position;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        IsMoving = false;

        useGridPathfinding = false;
        Debug.Log($"[{gameObject.name}] Grid pathfinding disabled - future movements will be direct queue-based");
        
        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.OnUnitArrived(this);
        }
        
        Debug.Log($"[{gameObject.name}] Grid-based movement completed");
    }

    Vector3 GetPositionAlongPath(float progress, List<Vector3> waypoints, Vector3 finalTarget)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return Vector3.Lerp(transform.position, finalTarget, progress);
        }

        float totalLength = 0f;
        List<float> segmentLengths = new List<float>();

        float startToFirst = Vector3.Distance(transform.position, waypoints[0]);
        totalLength += startToFirst;
        segmentLengths.Add(startToFirst);

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            totalLength += segmentLength;
            segmentLengths.Add(segmentLength);
        }

        float lastToTarget = Vector3.Distance(waypoints[waypoints.Count - 1], finalTarget);
        totalLength += lastToTarget;
        segmentLengths.Add(lastToTarget);

        float targetDistance = progress * totalLength;

        float currentDistance = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = waypoints.Count > 0 ? waypoints[0] : finalTarget;

        if (targetDistance <= segmentLengths[0])
        {
            float segmentProgress = targetDistance / segmentLengths[0];
            return Vector3.Lerp(startPos, endPos, segmentProgress);
        }
        currentDistance += segmentLengths[0];

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (targetDistance <= currentDistance + segmentLengths[i + 1])
            {
                float segmentProgress = (targetDistance - currentDistance) / segmentLengths[i + 1];
                return Vector3.Lerp(waypoints[i], waypoints[i + 1], segmentProgress);
            }
            currentDistance += segmentLengths[i + 1];
        }

        float finalSegmentProgress = (targetDistance - currentDistance) / segmentLengths[segmentLengths.Count - 1];
        return Vector3.Lerp(waypoints[waypoints.Count - 1], finalTarget, finalSegmentProgress);
    }

    IEnumerator MoveToSmoothDirect(Transform target)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = target.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / Mathf.Max(0.01f, moveSpeed * 2f); 
        float elapsed = 0f;

        if (col != null)
        {
            col.enabled = false;
            Debug.Log($"[{gameObject.name}] Collider disabled at START of movement");
        }

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
            Vector3 newPosition = Vector3.Lerp(startPos, targetPos, t);
            newPosition.y = startPos.y; 
            transform.position = newPosition;
            yield return null;
        }

        Vector3 finalPosition = new Vector3(targetPos.x, startPos.y, targetPos.z);
        transform.position = finalPosition;

        CompleteMovement(targetPos);
        
        Debug.Log($"[{gameObject.name}] Smooth direct movement completed to {targetPos}");
    }

    void CompleteMovement(Vector3 targetPos)
    {
        Vector3 finalPosition = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        

        transform.position = finalPosition;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = false; 
            rb.linearVelocity = Vector3.zero;
            Debug.Log($"[{gameObject.name}] Physics completely disabled, position locked");
        }

        if (col != null)
        {
            Debug.Log($"[{gameObject.name}] Collider remains disabled after movement completion");
        }

        if (col != null)
        {
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

        lockedPosition = finalPosition;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        IsMoving = false;

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

            transform.position = lockedPosition;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.detectCollisions = false; 
                rb.useGravity = false;
            }

            if (col != null)
            {
                col.enabled = false; 
            }
            
            yield return new WaitForSeconds(0.1f); 
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

    [ContextMenu("Show Selection Status")]
    void ShowSelectionStatus()
    {

        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"  Rigidbody Detect Collisions: {rb.detectCollisions}");
            Debug.Log($"  Rigidbody Is Kinematic: {rb.isKinematic}");
        }
    }
}
