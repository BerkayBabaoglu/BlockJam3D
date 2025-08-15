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
                        ShiftRightFrom(targetIndex); // Öncekileri saða kaydýr
                        targetPoint = SelectQueue.Instance.queuePoints[targetIndex];
                        SelectQueue.Instance.queueColors[targetIndex] = colorCode;
                        SelectQueue.Instance.queueObjects[targetIndex] = gameObject;
                        isMoving = true;

                        GetComponent<Collider>().enabled = false;
                    }
                    else
                    {
                        Debug.Log("Tüm noktalar dolu!");
                    }
                }
            }
        }

        if (isMoving && targetPoint != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPoint.position) < 0.01f)
            {
                transform.SetParent(targetPoint); // Noktaya baðla
                isMoving = false;
                CheckMatchAndClear();
            }
        }
    }

    int FindInsertIndexForColor(string color)
    {
        var colors = SelectQueue.Instance.queueColors;
        int lastSameColor = -1;

        // Kuyrukta en saðdaki ayný renk
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] == color)
                lastSameColor = i;
        }

        if (lastSameColor != -1)
        {
            // Onun saðý boþsa oraya
            if (lastSameColor + 1 < colors.Length && colors[lastSameColor + 1] == "")
                return lastSameColor + 1;

            // Sað doluysa araya girecek
            return lastSameColor + 1;
        }

        // Ayný renk yoksa ilk boþ yer
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

        // Eðer en saðdaki yer doluysa kaydýrma yapýlamaz
        if (colors[colors.Length - 1] != "")
        {
            Debug.Log("Kaydýrma yapýlamýyor, en sað dolu!");
            return;
        }

        // En saðdan baþlayýp index'in saðýndaki her þeyi bir saða kaydýr
        for (int i = colors.Length - 1; i > index; i--)
        {
            colors[i] = colors[i - 1];
            objs[i] = objs[i - 1];

            if (objs[i] != null)
            {
                objs[i].transform.SetParent(SelectQueue.Instance.queuePoints[i]);
                // Hedef pozisyona anýnda ýþýnlamak yerine istersen animasyonla gidebilir
                objs[i].transform.position = SelectQueue.Instance.queuePoints[i].position;
            }
        }

        // Araya girecek yer boþaltýldý
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
                // 3'lü eþleþmeyi temizle
                for (int j = 0; j < 3; j++)
                {
                    if (objs[i + j] != null)
                        Destroy(objs[i + j]); // sahneden sil
                    colors[i + j] = "";
                    objs[i + j] = null;
                }

                // Saðdakileri sola kaydýr
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

        // Son noktayý boþalt
        colors[colors.Length - 1] = "";
        objs[colors.Length - 1] = null;
    }
}