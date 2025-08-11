using UnityEngine;

public class TestPrefab : MonoBehaviour
{
    [Header("Test Settings")]
    public string prefabName = "TestPrefab";
    public Color gizmoColor = Color.red;
    
    private void Start()
    {
        Debug.Log($"[{name}] TestPrefab başlatıldı! Position: {transform.position}");
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
        
        // Label ekle
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, prefabName);
        #endif
    }
}
