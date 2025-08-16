using UnityEngine;

public class PathfindingTest : MonoBehaviour
{
    [Header("Test Settings")]
    public GameObject testCharacter;
    public bool autoSetup = true;
    public bool showDebugInfo = true;
    
    [Header("Grid Setup")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    
    private GridPathfinding pathfinding;
    private CharacterMovement characterMovement;
    
    void Start()
    {
        if (autoSetup)
        {
            SetupPathfindingSystem();
        }
    }
    
    void SetupPathfindingSystem()
    {
        // Create pathfinding system
        GameObject pathfindingGO = new GameObject("GridPathfinding");
        pathfinding = pathfindingGO.AddComponent<GridPathfinding>();
        
        // Configure pathfinding
        pathfinding.gridWidth = gridWidth;
        pathfinding.gridHeight = gridHeight;
        pathfinding.cellSize = cellSize;
        pathfinding.gridOrigin = gridOrigin;
        pathfinding.showDebugPath = showDebugInfo;
        
        // Set obstacle layer (you can adjust this based on your project)
        pathfinding.obstacleLayer = LayerMask.GetMask("Default");
        
        Debug.Log("Pathfinding system setup completed!");
        
        // Setup character if available
        if (testCharacter != null)
        {
            SetupCharacter();
        }
    }
    
    void SetupCharacter()
    {
        // Add character movement component if not present
        characterMovement = testCharacter.GetComponent<CharacterMovement>();
        if (characterMovement == null)
        {
            characterMovement = testCharacter.AddComponent<CharacterMovement>();
        }
        
        // Configure character movement
        characterMovement.pathfinding = pathfinding;
        characterMovement.moveSpeed = 3f;
        characterMovement.rotationSpeed = 5f;
        characterMovement.obstacleLayer = pathfinding.obstacleLayer;
        
        Debug.Log("Character setup completed!");
    }
    
    void Update()
    {
        // Test input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestPathfinding();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCharacter();
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleGridDebug();
        }
    }
    
    void TestPathfinding()
    {
        if (pathfinding == null || testCharacter == null)
        {
            Debug.LogWarning("Pathfinding system or test character not available!");
            return;
        }
        
        // Generate a random target position within grid bounds
        Vector3 randomTarget = gridOrigin + new Vector3(
            Random.Range(0, gridWidth) * cellSize + cellSize * 0.5f,
            0,
            Random.Range(0, gridHeight) * cellSize + cellSize * 0.5f
        );
        
        Debug.Log($"Testing pathfinding to: {randomTarget}");
        
        // Start pathfinding
        if (characterMovement != null)
        {
            characterMovement.SetTargetPosition(randomTarget);
        }
        else
        {
            pathfinding.StartPathfinding(testCharacter.transform, randomTarget);
        }
    }
    
    void ResetCharacter()
    {
        if (testCharacter != null)
        {
            testCharacter.transform.position = gridOrigin + new Vector3(cellSize * 0.5f, 0, cellSize * 0.5f);
            if (characterMovement != null)
            {
                characterMovement.StopMovement();
            }
            Debug.Log("Character reset to origin!");
        }
    }
    
    void ToggleGridDebug()
    {
        if (pathfinding != null)
        {
            pathfinding.showDebugPath = !pathfinding.showDebugPath;
            Debug.Log($"Grid debug visualization: {(pathfinding.showDebugPath ? "ON" : "OFF")}");
        }
    }
    
    // Create a simple test scene with obstacles
    [ContextMenu("Create Test Scene")]
    void CreateTestScene()
    {
        if (pathfinding == null)
        {
            Debug.LogWarning("Please setup pathfinding system first!");
            return;
        }
        
        // Create some test obstacles
        CreateObstacle(new Vector3(2, 0, 2));
        CreateObstacle(new Vector3(3, 0, 3));
        CreateObstacle(new Vector3(4, 0, 4));
        CreateObstacle(new Vector3(5, 0, 2));
        CreateObstacle(new Vector3(6, 0, 3));
        
        // Update grid walkability
        pathfinding.UpdateGridWalkability();
        
        Debug.Log("Test scene created with obstacles!");
    }
    
    void CreateObstacle(Vector3 position)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "TestObstacle";
        obstacle.transform.position = position;
        obstacle.transform.localScale = Vector3.one * 0.8f;
        
        // Make it red to distinguish from walkable areas
        Renderer renderer = obstacle.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
    }
    
    // Manual grid setup
    [ContextMenu("Manual Grid Setup")]
    void ManualGridSetup()
    {
        if (pathfinding == null)
        {
            Debug.LogWarning("Please setup pathfinding system first!");
            return;
        }
        
        // You can manually configure the grid here
        pathfinding.gridWidth = gridWidth;
        pathfinding.gridHeight = gridHeight;
        pathfinding.cellSize = cellSize;
        pathfinding.gridOrigin = gridOrigin;
        
        Debug.Log("Manual grid setup completed!");
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Pathfinding Test Controls");
        GUILayout.Label("Space: Test random pathfinding");
        GUILayout.Label("R: Reset character");
        GUILayout.Label("G: Toggle grid debug");
        
        if (pathfinding != null)
        {
            GUILayout.Label($"Grid: {pathfinding.gridWidth}x{pathfinding.gridHeight}");
            GUILayout.Label($"Cell Size: {pathfinding.cellSize}");
        }
        
        if (characterMovement != null)
        {
            GUILayout.Label($"Character Moving: {characterMovement.IsMoving()}");
            GUILayout.Label($"Target: {characterMovement.GetTargetPosition()}");
        }
        
        GUILayout.EndArea();
    }
}
