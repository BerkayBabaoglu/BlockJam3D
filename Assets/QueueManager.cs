using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    public static QueueManager Instance { get; private set; }

    [Header("Queue noktalarını sırayla (soldan sağa) ekle")]
    public Transform[] points;

    [HideInInspector] public string[] colors;
    [HideInInspector] public GameObject[] objs;

    private bool boardLocked = false;
    private bool isProcessingMatches = false;

    Coroutine matchLoopRoutine;

    void Awake()
    {
        if (this == null) return; 
        
        Instance = this;

        Debug.Log($"[QueueManager] Awake başladı - points.Length: {points.Length}");

        if (points == null || points.Length == 0)
        {
            Debug.LogError("[QueueManager] Points array'i boş veya null! Inspector'da points ekleyin!");
            return;
        }

        colors = new string[points.Length];
        objs = new GameObject[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
            {
                Debug.LogError($"[QueueManager] Point {i} null! Inspector'da kontrol edin!");
                continue;
            }
            
            colors[i] = "";
            objs[i] = null;
            Debug.Log($"[QueueManager] Point {i}: {points[i].name}");
        }
        
        Debug.Log($"[QueueManager] Awake tamamlandı");
    }
    
    void OnDestroy()
    {
        if (matchLoopRoutine != null)
        {
            StopCoroutine(matchLoopRoutine);
            matchLoopRoutine = null;
        }
        StopAllCoroutines();
    }
    
    void OnDisable()
    {
        if (matchLoopRoutine != null)
        {
            StopCoroutine(matchLoopRoutine);
            matchLoopRoutine = null;
        }
        StopAllCoroutines();
    }

    public void CleanupAllCoroutines()
    {
        if (matchLoopRoutine != null)
        {
            StopCoroutine(matchLoopRoutine);
            matchLoopRoutine = null;
        }
        StopAllCoroutines();
        Debug.Log("[QueueManager] All coroutines cleaned up");
    }

    [ContextMenu("Force Cleanup All Coroutines")]
    void ForceCleanup()
    {
        Debug.Log("[QueueManager] Force cleanup requested");
        CleanupAllCoroutines();
    }

    [ContextMenu("Show Coroutine Status")]
    void ShowCoroutineStatus()
    {
        Debug.Log($"[QueueManager] Coroutine Status - matchLoopRoutine: {(matchLoopRoutine != null ? "Running" : "Null")}");
        Debug.Log($"[QueueManager] Board Locked: {boardLocked}, Processing Matches: {isProcessingMatches}");
    }

    [ContextMenu("Test Memory Leak")]
    void TestMemoryLeak()
    {
        Debug.Log("[QueueManager] Testing memory leak...");

        CleanupAllCoroutines();

        if (matchLoopRoutine != null)
        {
            Debug.LogError("[QueueManager] MEMORY LEAK DETECTED! Coroutines not cleaned up properly!");
        }
        else
        {
            Debug.Log("[QueueManager] No memory leak detected. All coroutines cleaned up successfully.");
        }
    }

    public bool TryInsert(GameObject unit, string color, out int index)
    {
        index = -1;

        if (this == null) return false; 
        
        if (unit == null)
        {
            Debug.LogError("[QueueManager] TryInsert: unit null!");
            return false;
        }

        Debug.Log($"[QueueManager] TryInsert çağrıldı - unit: {unit.name}, color: {color}");

        if (!CanMove(unit)) 
        {
            Debug.Log($"[QueueManager] CanMove false - ekleme iptal edildi");
            return false;
        }
        if (boardLocked) 
        {
            Debug.Log($"[QueueManager] Board kilitli - ekleme iptal edildi");
            return false;
        }

        int lastSame = -1;
        for (int i = 0; i < colors.Length; i++)
            if (colors[i] == color) lastSame = i;

        if (lastSame >= 0)
            index = lastSame + 1;
        else
        {
            for (int i = 0; i < colors.Length; i++)
                if (objs[i] == null) { index = i; break; }
        }

        Debug.Log($"[QueueManager] Hesaplanan index: {index}");

        if (index == -1) 
        {
            Debug.Log($"[QueueManager] Geçerli index bulunamadı - ekleme iptal edildi");
            return false;
        }

        if (index >= points.Length)
        {
            Debug.LogError($"[QueueManager] Index {index} points array'inin dışında! points.Length: {points.Length}");
            return false;
        }

        if (objs[index] != null)
        {
            int empty = LastEmptyIndex();
            if (empty == -1) 
            {
                Debug.Log($"[QueueManager] Boş yer bulunamadı - ekleme iptal edildi");
                return false;
            }

            Debug.Log($"[QueueManager] Sağa kaydırma yapılıyor - index: {index}, empty: {empty}");
            for (int i = empty; i > index; i--)
            {
                objs[i] = objs[i - 1];
                colors[i] = colors[i - 1];
                if (objs[i] != null)
                {
                    var q = objs[i].GetComponent<QueueUnit>();
                    if (q != null)
                    {
                        q.SetIndexDirect(i);
                    }
                    else
                    {
                        Debug.LogWarning($"[QueueManager] Obj {i}'de QueueUnit component'i bulunamadı!");
                    }
                }
            }
            objs[index] = null;
            colors[index] = "";
        }

        objs[index] = unit;
        colors[index] = color;

        var newUnit = unit.GetComponent<QueueUnit>();
        if (newUnit != null)
        {
            newUnit.SetIndex(index);
            Debug.Log($"[QueueManager] Başarıyla eklendi - index: {index}");
        }
        else
        {
            Debug.LogError($"[QueueManager] Unit'ta QueueUnit component'i bulunamadı: {unit.name}");
            return false;
        }

        return true;
    }

    public void ResetUnitJoinedStatus(GameObject unit)
    {
        if (this == null) return; 
        
        if (unit == null)
        {
            Debug.LogWarning("[QueueManager] ResetUnitJoinedStatus: unit null!");
            return;
        }
        
        var queueUnit = unit.GetComponent<QueueUnit>();
        if (queueUnit != null)
        {
            queueUnit.ResetJoinedStatus();
        }
        else
        {
            Debug.LogWarning($"[QueueManager] ResetUnitJoinedStatus: Unit'ta QueueUnit component'i bulunamadı: {unit.name}");
        }
    }

    int LastEmptyIndex()
    {
        if (this == null) return -1; 
        
        if (objs == null)
        {
            Debug.LogError("[QueueManager] LastEmptyIndex: objs array'i null!");
            return -1;
        }
        
        for (int i = objs.Length - 1; i >= 0; i--)
            if (objs[i] == null) 
            {
                Debug.Log($"[QueueManager] LastEmptyIndex bulundu: {i}");
                return i;
            }
        Debug.Log($"[QueueManager] LastEmptyIndex bulunamadı - tüm yerler dolu");
        return -1;
    }

    public void OnUnitArrived(QueueUnit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("[QueueManager] OnUnitArrived: unit null!");
            return;
        }
        
        if (this == null) return; 
        
        if (!boardLocked && !AnyBlockMoving())
            StartCoroutine(DelayedCheckForMatches());
    }

    bool AnyBlockMoving()
    {
        if (this == null) return false; 
        
        if (objs == null)
        {
            Debug.LogError("[QueueManager] AnyBlockMoving: objs array'i null!");
            return false;
        }
        
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] != null)
            {
                var q = objs[i].GetComponent<QueueUnit>();
                if (q != null && q.IsMoving) 
                {
                    Debug.Log($"[QueueManager] Block hareket ediyor: {objs[i].name}");
                    return true;
                }
            }
        }
        return false;
    }

    IEnumerator DelayedCheckForMatches()
    {
        yield return new WaitForSeconds(0.05f);
        
        if (this != null) 
        {
            TryStartMatchLoop();
        }
    }

    void TryStartMatchLoop()
    {
        if (this == null) return; 
        
        if (matchLoopRoutine == null && !boardLocked)
            matchLoopRoutine = StartCoroutine(MatchLoop());
    }

    IEnumerator MatchLoop()
    {
        if (this == null) yield break; 
        
        boardLocked = true;
        isProcessingMatches = true;

        while (FindFirstMatch(out int start))
        {
            if (start < 0 || start + 2 >= objs.Length)
            {
                Debug.LogError($"[QueueManager] MatchLoop: Geçersiz start index: {start}, objs.Length: {objs.Length}");
                break;
            }
            
            GameObject[] blocks = new GameObject[3];
            for (int j = 0; j < 3; j++) 
            {
                blocks[j] = objs[start + j];
                objs[start + j] = null;
                colors[start + j] = "";
            }

            yield return StartCoroutine(AnimateExplosion(blocks));
            yield return StartCoroutine(CompactLeftAnimated());
            yield return null;
        }

        isProcessingMatches = false;
        boardLocked = false;
        matchLoopRoutine = null;
    }

    bool FindFirstMatch(out int startIndex)
    {
        startIndex = -1;
        
        if (this == null) return false; 
        
        if (colors == null)
        {
            Debug.LogError("[QueueManager] FindFirstMatch: colors array'i null!");
            return false;
        }
        
        for (int i = 0; i <= colors.Length - 3; i++)
        {
            string c = colors[i];
            if (!string.IsNullOrEmpty(c) && c == colors[i + 1] && c == colors[i + 2])
            {
                startIndex = i;
                return true;
            }
        }
        return false;
    }

    public bool CanMove(GameObject unit)
    {
        if (this == null) return false; 
        
        if (unit == null) 
        {
            Debug.LogWarning("[QueueManager] CanMove: unit null!");
            return false;
        }

        RayKontrol rk = unit.GetComponent<RayKontrol>();
        if (rk != null && rk.isMovementLocked) 
        {
            Debug.Log($"[QueueManager] CanMove false - RayKontrol hareket kilitli: {unit.name}");
            return false;
        }

        bool canMove = !boardLocked && !isProcessingMatches;
        Debug.Log($"[QueueManager] CanMove: {canMove} - unit: {unit.name}, boardLocked: {boardLocked}, isProcessingMatches: {isProcessingMatches}");
        return canMove;
    }

    IEnumerator AnimateExplosion(GameObject[] blocks)
    {
        if (this == null) yield break; 
        
        if (blocks == null || blocks.Length != 3) 
        {
            Debug.LogError($"[QueueManager] AnimateExplosion: blocks null veya yanlış boyut: {blocks?.Length}");
            yield break;
        }

        List<Coroutine> lifts = new List<Coroutine>();
        for (int i = 0; i < 3; i++)
            if (blocks[i] != null) lifts.Add(StartCoroutine(LiftBlock(blocks[i], 0.25f)));
        foreach (var c in lifts) yield return c;

        List<Coroutine> moves = new List<Coroutine>();
        if (blocks[1] != null)
        {
            Vector3 center = blocks[1].transform.position;
            if (blocks[0] != null) moves.Add(StartCoroutine(MoveTo(blocks[0], center, 0.20f)));
            if (blocks[2] != null) moves.Add(StartCoroutine(MoveTo(blocks[2], center, 0.20f)));
        }
        foreach (var c in moves) yield return c;

        yield return new WaitForSeconds(0.08f);
        for (int i = 0; i < 3; i++)
            if (blocks[i] != null) Object.Destroy(blocks[i]);
    }

    IEnumerator LiftBlock(GameObject block, float duration)
    {
        if (this == null) yield break; 
        
        if (block == null) 
        {
            Debug.LogWarning("[QueueManager] LiftBlock: block null!");
            yield break;
        }
        
        Vector3 start = block.transform.position;
        Vector3 end = start + Vector3.up * 2f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            block.transform.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }
        block.transform.position = end;
    }

    IEnumerator MoveTo(GameObject block, Vector3 targetPos, float duration)
    {
        if (this == null) yield break; 
        
        if (block == null) 
        {
            Debug.LogWarning("[QueueManager] MoveTo: block null!");
            yield break;
        }
        
        Vector3 start = block.transform.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            block.transform.position = Vector3.Lerp(start, targetPos, t / duration);
            yield return null;
        }
        block.transform.position = targetPos;
    }

    IEnumerator CompactLeftAnimated()
    {
        if (this == null) yield break; 
        
        if (objs == null || colors == null)
        {
            Debug.LogError("[QueueManager] CompactLeftAnimated: objs veya colors array'i null!");
            yield break;
        }
        
        int write = 0;
        List<(QueueUnit unit, int toIndex)> moves = new List<(QueueUnit, int)>();

        for (int read = 0; read < objs.Length; read++)
        {
            if (objs[read] != null)
            {
                if (write != read)
                {
                    var unit = objs[read].GetComponent<QueueUnit>();
                    if (unit != null)
                    {
                        moves.Add((unit, write));
                    }
                    else
                    {
                        Debug.LogWarning($"[QueueManager] CompactLeftAnimated: Obj {read}'de QueueUnit component'i bulunamadı!");
                    }

                    objs[write] = objs[read];
                    colors[write] = colors[read];

                    objs[read] = null;
                    colors[read] = "";
                }
                write++;
            }
        }
        for (int k = write; k < objs.Length; k++)
        {
            objs[k] = null;
            colors[k] = "";
        }

        if (moves.Count == 0) yield break;

        foreach (var m in moves)
            m.unit.SetIndexDirect(m.toIndex);

        yield return new WaitUntil(() =>
        {
            for (int i = 0; i < moves.Count; i++)
                if (moves[i].unit != null && moves[i].unit.IsMoving) return false;
            return true;
        });
    }

    [ContextMenu("Show Queue Status")]
    public void ShowQueueStatus()
    {
        if (this == null) return; 
        
        if (colors == null || objs == null)
        {
            Debug.LogError("[QueueManager] ShowQueueStatus: colors veya objs array'i null!");
            return;
        }
        
        string status = "Queue Status:\n";
        for (int i = 0; i < colors.Length; i++)
        {
            string objName = objs[i] != null ? objs[i].name : "Empty";
            string color = colors[i] != "" ? colors[i] : "Empty";
            status += $"Pos {i}: {color} ({objName})\n";
        }
        Debug.Log(status);
        

        Debug.Log("Ray Kontrol Durumları:");
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] != null)
            {
                var rayKontrol = objs[i].GetComponent<RayKontrol>();
                if (rayKontrol != null)
                {
                    Debug.Log($"  {objs[i].name}: isMovementLocked = {rayKontrol.isMovementLocked}");
                }
                else
                {
                    Debug.Log($"  {objs[i].name}: RayKontrol component'i yok!");
                }
            }
        }
    }
}