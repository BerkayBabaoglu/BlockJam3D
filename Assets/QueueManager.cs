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
        Instance = this;

        colors = new string[points.Length];
        objs = new GameObject[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            colors[i] = "";
            objs[i] = null;
        }
    }

    public bool TryInsert(GameObject unit, string color, out int index)
    {
        index = -1;

        if (boardLocked) return false;

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

        if (index == -1) return false;

        if (objs[index] != null)
        {
            int empty = LastEmptyIndex();
            if (empty == -1) return false;

            for (int i = empty; i > index; i--)
            {
                objs[i] = objs[i - 1];
                colors[i] = colors[i - 1];
                if (objs[i] != null)
                {
                    var q = objs[i].GetComponent<QueueUnit>();
                    q.SetIndex(i);
                }
            }
            objs[index] = null;
            colors[index] = "";
        }

        objs[index] = unit;
        colors[index] = color;

        var newUnit = unit.GetComponent<QueueUnit>();
        newUnit.SetIndex(index);

        return true;
    }

    int LastEmptyIndex()
    {
        for (int i = objs.Length - 1; i >= 0; i--)
            if (objs[i] == null) return i;
        return -1;
    }

    public void OnUnitArrived(QueueUnit unit)
    {

        if (!boardLocked && !AnyBlockMoving())
            StartCoroutine(DelayedCheckForMatches());
    }

    bool AnyBlockMoving()
    {
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] != null)
            {
                var q = objs[i].GetComponent<QueueUnit>();
                if (q != null && q.IsMoving) return true;
            }
        }
        return false;
    }

    IEnumerator DelayedCheckForMatches()
    {
        yield return new WaitForSeconds(0.05f);
        TryStartMatchLoop();
    }

    void TryStartMatchLoop()
    {
        if (matchLoopRoutine == null && !boardLocked)
            matchLoopRoutine = StartCoroutine(MatchLoop());
    }

    IEnumerator MatchLoop()
    {
        boardLocked = true;
        isProcessingMatches = true;

        while (FindFirstMatch(out int start))
        {

            GameObject[] blocks = new GameObject[3];
            for (int j = 0; j < 3; j++) blocks[j] = objs[start + j];

            for (int j = 0; j < 3; j++)
            {
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
        for (int i = 0; i <= colors.Length - 3; i++)
        {
            string c = colors[i];
            if (!string.IsNullOrEmpty(c) && c == colors[i + 1] && c == colors[i + 2])
            {
                startIndex = i;
                return true;
            }
        }
        startIndex = -1;
        return false;
    }

    IEnumerator AnimateExplosion(GameObject[] blocks)
    {
        if (blocks == null || blocks.Length != 3) yield break;

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

        yield return null;
    }

    IEnumerator LiftBlock(GameObject block, float duration)
    {
        if (block == null) yield break;
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
        if (block == null) yield break;
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

        int write = 0;
        List<(QueueUnit unit, int toIndex)> moves = new List<(QueueUnit, int)>();

        for (int read = 0; read < objs.Length; read++)
        {
            if (objs[read] != null)
            {
                if (write != read)
                {
                    var unit = objs[read].GetComponent<QueueUnit>();
                    moves.Add((unit, write));

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
            m.unit.SetIndex(m.toIndex);

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
        string status = "Queue Status:\n";
        for (int i = 0; i < colors.Length; i++)
            status += $"Pos {i}: {(objs[i] != null ? colors[i] : "Empty")}\n";
        Debug.Log(status);
    }
}