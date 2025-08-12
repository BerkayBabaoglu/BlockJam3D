using Unity.VisualScripting;
using UnityEngine;

public class RayKontrol : MonoBehaviour
{
    public GameObject character6;
    public GameObject armature;
    public float rayDistance = 1f;
    public LayerMask characterLayer;

    private void Update()
    {
        bool allHit = true;

        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };


        foreach(Vector3 dir in directions)
        {
            if(!Physics.Raycast(transform.position, dir, rayDistance, characterLayer))
            {
                allHit = false;
                break;
            }
        }

        if (allHit)
        {
            character6.SetActive(false);
            armature.SetActive(true);
        }
        else
        {
            armature.SetActive(false);
            character6.SetActive(true);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        foreach(Vector3 dir in directions)
        {
            Gizmos.DrawRay(transform.position, dir * rayDistance);
        }
    }
}
