using UnityEngine;

public class RayKontrol : MonoBehaviour
{
    public GameObject character6;
    public GameObject armature;
    public float rayDistance = 1f;
    public LayerMask characterLayer = -1; //tum layerlari kapsa

    public bool isMovementLocked { get; private set; }
    public Animator animator;

    private void Update()
    {
        bool allHit = true;
        string[] directionNames = { "Forward", "Back", "Left", "Right" };
        bool[] hitResults = new bool[4];

        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 dir = directions[i];
            hitResults[i] = Physics.Raycast(transform.position, dir, rayDistance, characterLayer);
            if (!hitResults[i])
            {
                allHit = false;
            }
        }

        isMovementLocked = allHit;


        if (Time.frameCount % 60 == 0) // her 60 frame'de bir log
        {
            string rayStatus = "";
            for (int i = 0; i < 4; i++)
            {
                rayStatus += $"{directionNames[i]}:{(hitResults[i] ? "Hit" : "Miss")} ";
            }
            Debug.Log($"[{gameObject.name}] Ray kontrol: {rayStatus} | allHit={allHit}, isMovementLocked={isMovementLocked}");
        }

        if (allHit)
        {
            if (character6 != null) character6.SetActive(false);
            if (armature != null) armature.SetActive(true);
        }
        else
        {
            if (armature != null) armature.SetActive(false);
            if (character6 != null) character6.SetActive(true);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        foreach (Vector3 dir in directions)
        {
            Gizmos.DrawRay(transform.position, dir * rayDistance);
        }
    }
}