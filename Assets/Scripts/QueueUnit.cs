using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class QueueUnit : MonoBehaviour
{
    [Tooltip("K, S, M, Y")]
    public string colorCode;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float arrivalThreshold = 0.01f;
    public bool useSmoothMovement = true;

    public int CurrentIndex { get; private set; } = -1;


    public bool IsMoving { get; private set; }

    bool hasJoined = false;
    Coroutine moveRoutine;
    Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
    }

    void OnMouseDown()
    {
        if (hasJoined) return;

        int idx;
        if (QueueManager.Instance.TryInsert(gameObject, colorCode, out idx))
        {
            hasJoined = true;
            if (col) col.enabled = false;
            StartCoroutine(JoinFeedback());
        }
    }

    public void SetIndex(int index)
    {
        CurrentIndex = index;
        Transform target = QueueManager.Instance.points[index];

        if (moveRoutine != null) StopCoroutine(moveRoutine);

        IsMoving = true;
        if (useSmoothMovement)
            moveRoutine = StartCoroutine(MoveToSmooth(target));
        else
            moveRoutine = StartCoroutine(MoveTo(target));
    }

    public void SetIndexInstant(int index)
    {
        CurrentIndex = index;
        Transform target = QueueManager.Instance.points[index];
        transform.SetParent(target, true);
        transform.position = target.position;
        IsMoving = false;
    }

    public void RemoveFromQueue()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;
        IsMoving = false;

        transform.SetParent(null);
        gameObject.SetActive(false);
        CurrentIndex = -1;
    }

    public void DestroySelf()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = null;
        IsMoving = false;

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
        transform.SetParent(target, true); 
        IsMoving = false;
        QueueManager.Instance.OnUnitArrived(this);
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
        transform.SetParent(target, true);
        IsMoving = false;
        QueueManager.Instance.OnUnitArrived(this);
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
}