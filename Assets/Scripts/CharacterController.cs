using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public float speed = 3f;
    public string colorCode; // K, S, M, Y

    private Transform targetPoint;
    private bool isMoving = false;
    private int targetIndex = -1;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isMoving)
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
                        ShiftRightFrom(targetIndex); // �ncekileri sa�a kayd�r
                        targetPoint = SelectQueue.Instance.queuePoints[targetIndex];
                        SelectQueue.Instance.queueColors[targetIndex] = colorCode;
                        SelectQueue.Instance.queueObjects[targetIndex] = gameObject;
                        isMoving = true;

                        GetComponent<Collider>().enabled = false;
                    }
                    else
                    {
                        Debug.Log("T�m noktalar dolu!");
                    }
                }
            }
        }

        if (isMoving && targetPoint != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPoint.position) < 0.01f)
            {
                transform.SetParent(targetPoint); // Noktaya ba�la
                isMoving = false;
                CheckMatchAndClear();
            }
        }
    }

    int FindInsertIndexForColor(string color)
    {
        var colors = SelectQueue.Instance.queueColors;
        int lastSameColor = -1;

        // Kuyrukta en sa�daki ayn� renk
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == color)
                lastSameColor = i;
        }

        if (lastSameColor != -1)
        {
            // Onun sa�� bo�sa oraya
            if (lastSameColor + 1 < colors.Length && colors[lastSameColor + 1] == "")
                return lastSameColor + 1;

            // Sa� doluysa araya girecek
            return lastSameColor + 1;
        }

        // Ayn� renk yoksa ilk bo� yer
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

        // E�er en sa�daki yer doluysa kayd�rma yap�lamaz
        if (colors[colors.Length - 1] != "")
        {
            Debug.Log("Kayd�rma yap�lam�yor, en sa� dolu!");
            return;
        }

        // En sa�dan ba�lay�p index'in sa��ndaki her �eyi bir sa�a kayd�r
        for (int i = colors.Length - 1; i > index; i--)
        {
            colors[i] = colors[i - 1];
            objs[i] = objs[i - 1];

            if (objs[i] != null)
            {
                objs[i].transform.SetParent(SelectQueue.Instance.queuePoints[i]);
                // Hedef pozisyona an�nda ���nlamak yerine istersen animasyonla gidebilir
                objs[i].transform.position = SelectQueue.Instance.queuePoints[i].position;
            }
        }

        // Araya girecek yer bo�alt�ld�
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
                // 3'l� e�le�meyi temizle
                for (int j = 0; j < 3; j++)
                {
                    if (objs[i + j] != null)
                        Destroy(objs[i + j]); // sahneden sil
                    colors[i + j] = "";
                    objs[i + j] = null;
                }

                // Sa�dakileri sola kayd�r
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

        // Son noktay� bo�alt
        colors[colors.Length - 1] = "";
        objs[colors.Length - 1] = null;
    }
}