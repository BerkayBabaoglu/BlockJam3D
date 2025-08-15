using UnityEngine;

public class SelectQueue : MonoBehaviour
{
    public static SelectQueue Instance { get; private set; }

    [Header("Queue noktaları sırayla ekle")]
    public Transform[] queuePoints;

    [HideInInspector] public string[] queueColors;
    [HideInInspector] public GameObject[] queueObjects;

    private void Awake()
    {
        Instance = this;
        queueColors = new string[queuePoints.Length];
        queueObjects = new GameObject[queuePoints.Length];
        for (int i = 0; i < queuePoints.Length; i++)
        {
            queueColors[i] = "";
            queueObjects[i] = null;
        }
    }
}
