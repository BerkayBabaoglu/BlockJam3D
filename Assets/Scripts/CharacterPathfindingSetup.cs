using UnityEngine;

public class CharacterPathfindingSetup : MonoBehaviour
{
    [Header("Pathfinding Setup")]
    public bool autoSetupOnStart = true;
    public bool findPathfindingInParent = true;
    
    private CharacterController characterController;
    private GridPathfinding pathfindingSystem;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupPathfinding();
        }
    }
    
    void SetupPathfinding()
    {
        // Get character controller component
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning($"CharacterController not found on {gameObject.name}");
            return;
        }
        
        // Find pathfinding system
        if (findPathfindingInParent)
        {
            // Look in parent hierarchy
            pathfindingSystem = GetComponentInParent<GridPathfinding>();
            if (pathfindingSystem == null)
            {
                // Look for GridGenerator and get its pathfinding system
                GridGenerator gridGenerator = GetComponentInParent<GridGenerator>();
                if (gridGenerator != null)
                {
                    // Try to find pathfinding system in children
                    pathfindingSystem = gridGenerator.GetComponentInChildren<GridPathfinding>();
                }
            }
        }
        
        // If still not found, search in scene
        if (pathfindingSystem == null)
        {
            pathfindingSystem = FindObjectOfType<GridPathfinding>();
        }
        
        // Assign pathfinding system to character controller
        if (pathfindingSystem != null)
        {
            characterController.pathfinding = pathfindingSystem;
            Debug.Log($"Pathfinding system assigned to {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"No pathfinding system found for {gameObject.name}");
        }
    }
    
    // Manual setup method
    [ContextMenu("Setup Pathfinding")]
    public void ManualSetupPathfinding()
    {
        SetupPathfinding();
    }
    
    // Get current pathfinding system
    public GridPathfinding GetPathfindingSystem()
    {
        return pathfindingSystem;
    }
    
    // Check if pathfinding is properly setup
    public bool IsPathfindingSetup()
    {
        return characterController != null && characterController.pathfinding != null;
    }
}
