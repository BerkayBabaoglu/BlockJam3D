using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public float speed = 3f;
    private Transform targetPoint;
    private bool isMoving = false;

    void Update()
    {
        // T�klama kontrol�
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    targetPoint = FindEmptyPoint();
                    if (targetPoint != null)
                    {
                        targetPoint.tag = "Occupied";
                        isMoving = true;

                        // T�klanmay� engelle
                        GetComponent<Collider>().enabled = false;
                    }
                    else
                    {
                        Debug.Log("T�m noktalar dolu! Oyun bitti!");
                    }
                }
            }
        }

        // Hareket
        if (isMoving && targetPoint != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPoint.position) < 0.01f)
            {
                isMoving = false;
            }
        }
    }

    Transform FindEmptyPoint()
    {
        foreach (Transform point in SelectQueue.Instance.queuePoints)
        {
            if (point.CompareTag("Empty"))
            {
                return point;
            }
        }
        return null;
    }
}
