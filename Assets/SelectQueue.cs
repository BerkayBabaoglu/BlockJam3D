using UnityEngine;

public class SelectQueue : MonoBehaviour
{
    public static SelectQueue Instance { get; private set; }

    [Header("Kuyruk noktalarını sırayla ekle (1 → 2 → 3)")]
    public Transform[] queuePoints;

    private void Awake()
    {
        Instance = this;
    }
}