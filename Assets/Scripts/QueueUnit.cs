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
        
        Debug.Log($"[{gameObject.name}] Awake - Collider: {(col != null ? "OK" : "MISSING")}, RayKontrol: {(rayKontrol != null ? "OK" : "MISSING")}, Animator: {(animator != null ? "OK" : "MISSING")}");
    }
    


    void OnMouseDown()
    {
        Debug.Log($"[{gameObject.name}] OnMouseDown çağrıldı - IsMoving: {IsMoving}, hasJoined: {hasJoined}");
        
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
                    Debug.LogWarning($"[{gameObject.name}] No grid path found, using direct movement");
                }
            }

            IsMoving = true;
            Debug.Log($"[{gameObject.name}] Hareket başlıyor - hedef: {target.name}");
            
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

    public void SetIndexInstant(int index)
    {
        CurrentIndex = index;
        
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

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;

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
        
        lockedPosition = targetPos;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        
        IsMoving = false;
        
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
        while ((transform.position - targetPos).sqrMagnitude > arrivalThreshold * arrivalThreshold)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        

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

        lockedPosition = targetPos;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());

        
        IsMoving = false;
        
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

        while (currentWaypointIndex < currentPath.Count)
        {
            Vector3 currentWaypoint = currentPath[currentWaypointIndex];
            
            Debug.Log($"[{gameObject.name}] Moving to waypoint {currentWaypointIndex + 1}/{currentPath.Count}: {currentWaypoint}");

            while (Vector3.Distance(transform.position, currentWaypoint) > arrivalThreshold)
            {
                Vector3 direction = (currentWaypoint - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                yield return null;
            }
            

            transform.position = currentWaypoint;
            currentWaypointIndex++;
            
            Debug.Log($"[{gameObject.name}] Reached waypoint {currentWaypointIndex}, moving to next...");
        }

        Debug.Log($"[{gameObject.name}] Grid path completed, moving to final target");
        isFollowingPath = false;

        Vector3 targetPos = target.position;
        while (Vector3.Distance(transform.position, targetPos) > arrivalThreshold)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        

        transform.position = targetPos;
        

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
        

        lockedPosition = targetPos;
        isPositionLocked = true;
        if (positionLockRoutine != null) StopCoroutine(positionLockRoutine);
        positionLockRoutine = StartCoroutine(LockPosition());
        
        IsMoving = false;
        
        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.OnUnitArrived(this);
        }
        
        Debug.Log($"[{gameObject.name}] Grid-based movement completed");
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

            float distance = Vector3.Distance(transform.position, lockedPosition);
            if (distance > 0.01f)
            {
                Debug.Log($"[{gameObject.name}] Pozisyon düzeltiliyor - mesafe: {distance:F3}");
                transform.position = lockedPosition;
                

                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            
            yield return new WaitForSeconds(0.05f); 
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
}