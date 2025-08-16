using UnityEngine;

public class QueuePathfindingTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool showDebugInfo = true;
    public KeyCode testKey = KeyCode.T;
    public KeyCode resetKey = KeyCode.R;
    
    [Header("Pathfinding Info")]
    public GridPathfinding pathfindingSystem;
    public CharacterController[] characters;
    
    void Start()
    {
        // Find pathfinding system
        if (pathfindingSystem == null)
        {
            pathfindingSystem = FindObjectOfType<GridPathfinding>();
        }
        
        // Find all characters
        characters = FindObjectsOfType<CharacterController>();
        
        Debug.Log($"QueuePathfindingTest: Found {characters.Length} characters and pathfinding system: {(pathfindingSystem != null ? "Yes" : "No")}");
    }
    
    void Update()
    {
        // Test pathfinding
        if (Input.GetKeyDown(testKey))
        {
            TestPathfinding();
        }
        
        // Reset characters
        if (Input.GetKeyDown(resetKey))
        {
            ResetCharacters();
        }
    }
    
    void TestPathfinding()
    {
        if (pathfindingSystem == null)
        {
            Debug.LogWarning("No pathfinding system found!");
            return;
        }
        
        if (characters.Length == 0)
        {
            Debug.LogWarning("No characters found!");
            return;
        }
        
        // Test pathfinding for first character
        CharacterController testChar = characters[0];
        if (testChar != null)
        {
            // Find a random walkable position
            Vector3 randomTarget = GetRandomWalkablePosition();
            Debug.Log($"Testing pathfinding from {testChar.transform.position} to {randomTarget}");
            
            // Test if path exists
            var path = pathfindingSystem.FindPath(testChar.transform.position, randomTarget);
            if (path != null)
            {
                Debug.Log($"Path found with {path.Count} waypoints");
            }
            else
            {
                Debug.LogWarning("No path found!");
            }
        }
    }
    
    void ResetCharacters()
    {
        foreach (var character in characters)
        {
            if (character != null)
            {
                // Reset to original position (you might want to store original positions)
                character.transform.position = Vector3.zero;
                Debug.Log($"Reset {character.name} to origin");
            }
        }
    }
    
    Vector3 GetRandomWalkablePosition()
    {
        if (pathfindingSystem == null) return Vector3.zero;
        
        // Generate random position within grid bounds
        Vector3 randomPos = pathfindingSystem.gridOrigin + new Vector3(
            Random.Range(0, pathfindingSystem.gridWidth) * pathfindingSystem.cellSize + pathfindingSystem.cellSize * 0.5f,
            0,
            Random.Range(0, pathfindingSystem.gridHeight) * pathfindingSystem.cellSize + pathfindingSystem.cellSize * 0.5f
        );
        
        return randomPos;
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Queue Pathfinding Test");
        GUILayout.Label($"Test Key: {testKey}");
        GUILayout.Label($"Reset Key: {resetKey}");
        GUILayout.Label($"Characters: {characters.Length}");
        GUILayout.Label($"Pathfinding: {(pathfindingSystem != null ? "Active" : "Not Found")}");
        
        if (pathfindingSystem != null)
        {
            GUILayout.Label($"Grid: {pathfindingSystem.gridWidth}x{pathfindingSystem.gridHeight}");
            GUILayout.Label($"Cell Size: {pathfindingSystem.cellSize}");
        }
        
        GUILayout.EndArea();
    }
}
