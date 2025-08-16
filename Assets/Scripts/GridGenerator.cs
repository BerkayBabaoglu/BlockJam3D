using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject grassPrefab;               // cellType == 1 (Grass)
    public GameObject[] characterPrefabs;        // cellType == 2,3,4,5 -> index = cellType - 2 (kirmizi, yesil, mavi, sari karakterler)
    [Header("Data")]
    public string jsonPath = "Assets/LevelData/level1.json"; // GridDataIO yükler
    float grassPrefabHeight = 0.5f;
    
    [Header("Pathfinding")]
    public bool createPathfindingSystem = true;
    public float cellSize = 1f;
    public LayerMask obstacleLayer = -1;

    private GridData gridData;
    private Vector3 planeSize = Vector3.one;
    private GridPathfinding pathfindingSystem;

    void Start()
    {
        Debug.Log($"[{name}] GridGenerator Start başladı");

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            Debug.LogError($"[{name}] MeshRenderer bulunamadı. GridGenerator bu GameObject üzerinde bir MeshRenderer bekliyor.");
            return;
        }
        planeSize = mr.bounds.size;
        Debug.Log($"[{name}] MeshRenderer bulundu, plane size: {planeSize}");

        if (grassPrefab != null)
        {
            MeshRenderer gmr = grassPrefab.GetComponentInChildren<MeshRenderer>();
            if (gmr != null) 
            {
                grassPrefabHeight = gmr.bounds.size.y;
                Debug.Log($"[{name}] Grass prefab height: {grassPrefabHeight}");
            }
            else
            {
                Debug.LogWarning("grassPrefab üzerinde MeshRenderer bulunamadı. Varsayılan height kullanılacak.");
                grassPrefabHeight = 0.5f;
            }
        }
        else
        {
            Debug.LogWarning("Grass prefab atanmamış. Grass spawn edilmeyecek.");
        }

        if (characterPrefabs != null && characterPrefabs.Length > 0)
        {
            Debug.Log($"[{name}] {characterPrefabs.Length} character prefab bulundu");
        }
        else
        {
            Debug.LogWarning("characterPrefabs array'i boş veya null. Karakter spawn edilmeyecek.");
        }

        Debug.Log($"[{name}] JSON yükleniyor: {jsonPath}");
        gridData = GridDataIO.LoadGridData(jsonPath);
        if (gridData == null)
        {
            Debug.LogError($"GridData yüklenemedi: {jsonPath}");
            return;
        }
        
        Debug.Log($"[{name}] GridData yüklendi: {gridData.cellsX}x{gridData.cellsZ}");

        if (gridData.cellsX <= 0 || gridData.cellsZ <= 0)
        {
            Debug.LogError("GridData geçersiz cellsX/cellsZ değerleri içeriyor.");
            return;
        }

        if (characterPrefabs == null) characterPrefabs = new GameObject[0];

        Debug.Log($"[name] GenerateFromData çağrılıyor...");
        GenerateFromData();
        
        if (createPathfindingSystem)
        {
            SetupPathfindingSystem();
        }
    }
    
    void GenerateFromData()
    {
        if (gridData == null)
        {
            Debug.LogError("GenerateFromData çağrıldı ama gridData null.");
            return;
        }

        Debug.Log($"[{name}] Grid oluşturuluyor: {gridData.cellsX}x{gridData.cellsZ}");

        float cellWidth = planeSize.x / Mathf.Max(1, gridData.cellsX);
        float cellHeight = planeSize.z / Mathf.Max(1, gridData.cellsZ);

        Vector3 startPos = transform.position - new Vector3(planeSize.x * 0.5f, 0f, planeSize.z * 0.5f);
        
        Debug.Log($"[{name}] Plane size: {planeSize}, Cell size: {cellWidth}x{cellHeight}");
        Debug.Log($"[{name}] Start position: {startPos}");

        int spawnedCount = 0;
        
        List<Vector3> occupiedPositions = new List<Vector3>();

        for (int x = 0; x < gridData.cellsX; x++)
        {
            for (int z = 0; z < gridData.cellsZ; z++)
            {
                int cellType = gridData.GetCell(x, z);
                
                Debug.Log($"[{name}] Cell ({x},{z}) = {cellType}");

                // 0 boş - hiçbir şey spawn edilmeyecek
                if (cellType == 0) 
                {
                    Debug.Log($"[{name}] Hücre ({x},{z}) boş, atlanıyor");
                    continue;
                }

                Vector3 cellPos = startPos + new Vector3(
                    cellWidth * (x + 0.5f),
                    grassPrefabHeight + 2f,
                    cellHeight * (z + 0.5f)
                );

                GameObject prefabToSpawn = null;

                if (cellType == 1) // Grass
                {
                    if (grassPrefab != null) 
                    {
                        prefabToSpawn = grassPrefab;
                        Debug.Log($"[{name}] Grass prefab bulundu: {grassPrefab.name}");
                    }
                    else
                    {
                        Debug.LogWarning("Cell wants grass (1) but grassPrefab is not assigned. Skipping.");
                        continue;
                    }
                }
                else if (cellType >= 2 && cellType <= 5) // 2,3,4,5 -> karakterler
                {
                    int index = cellType - 2;
                    if (index >= 0 && index < characterPrefabs.Length && characterPrefabs[index] != null)
                    {
                        prefabToSpawn = characterPrefabs[index];
                        Debug.Log($"[{name}] Character prefab bulundu: {characterPrefabs[index].name} (index: {index}, cellType: {cellType})");

                        occupiedPositions.Add(cellPos);
                    }
                    else
                    {
                        Debug.LogWarning($"CellType {cellType} istendi ama characterPrefabs[{index}] atanmadı. Hücre atlanıyor ({x},{z}).");
                        continue;
                    }
                }
                else
                {
                    Debug.LogWarning($"Bilinmeyen cellType {cellType} bulundu. Hücre atlanıyor ({x},{z}).");
                    continue;
                }

                if (prefabToSpawn != null)
                {
                    Debug.Log($"[{name}] Spawning {prefabToSpawn.name} at position {cellPos}");
                    
                    GameObject obj = Instantiate(prefabToSpawn, cellPos, Quaternion.identity, transform);
                    spawnedCount++;

                    if (cellType == 1) 
                    {

                        MeshRenderer instRenderer = obj.GetComponentInChildren<MeshRenderer>();
                        if (instRenderer != null)
                        {
                            Vector3 originalSize = instRenderer.bounds.size;


                            if (originalSize.x > 0.0001f && originalSize.z > 0.0001f)
                            {
                                float scaleX = cellWidth / originalSize.x;
                                float scaleZ = cellHeight / originalSize.z;

                                Vector3 local = obj.transform.localScale;
                                obj.transform.localScale = new Vector3(local.x * scaleX, local.y, local.z * scaleZ);
                                
                                Debug.Log($"[{name}] {obj.name} (grass) scaled to {obj.transform.localScale}");
                            }
                            else
                            {

                                Debug.LogWarning($"Instantiated '{obj.name}' renderer.bounds sıfır gibi ({originalSize}). Scale atlanıyor.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Instantiate edilen '{obj.name}' içinde MeshRenderer bulunamadı. Ölçek ayarlanmadı.");
                        }
                    }
                    else // Karakterler (2,3,4,5) - sabit scale
                    {
                        Vector3 fixedScale = new Vector3(0.514731765f, 0.514731765f, 0.514731765f);
                        obj.transform.localScale = fixedScale;
                        Debug.Log($"[{name}] {obj.name} (karakter) fixed scale: {obj.transform.localScale}");
                        

                        if (obj.GetComponent<CharacterPathfindingSetup>() == null)
                        {
                            obj.AddComponent<CharacterPathfindingSetup>();
                        }
                    }
                }
            }
        }
        
        Debug.Log($"[{name}] Toplam {spawnedCount} obje spawn edildi");
        Debug.Log($"[{name}] {occupiedPositions.Count} karakter pozisyonu kaydedildi");
    }

    void SetupPathfindingSystem()
    {
        if (pathfindingSystem != null)
        {
            Debug.Log($"[{name}] Pathfinding system already exists, skipping setup.");
            return;
        }

        GameObject pathfindingGO = new GameObject("GridPathfinding");
        pathfindingSystem = pathfindingGO.AddComponent<GridPathfinding>();

        pathfindingSystem.gridWidth = gridData.cellsX;
        pathfindingSystem.gridHeight = gridData.cellsZ;

        float actualCellWidth = planeSize.x / Mathf.Max(1, gridData.cellsX);
        float actualCellHeight = planeSize.z / Mathf.Max(1, gridData.cellsZ);

        pathfindingSystem.cellSize = Mathf.Min(actualCellWidth, actualCellHeight);

        pathfindingSystem.gridOrigin = transform.position - new Vector3(planeSize.x * 0.5f, 0f, planeSize.z * 0.5f);
        
        Debug.Log($"[{name}] Pathfinding grid setup details:");
        Debug.Log($"  Main grid: {gridData.cellsX}x{gridData.cellsZ}");
        Debug.Log($"  Main grid plane size: {planeSize}");
        Debug.Log($"  Main grid cell size: {actualCellWidth}x{actualCellHeight}");
        Debug.Log($"  Pathfinding grid: {pathfindingSystem.gridWidth}x{pathfindingSystem.gridHeight}");
        Debug.Log($"  Pathfinding cell size: {pathfindingSystem.cellSize}");
        Debug.Log($"  Pathfinding origin: {pathfindingSystem.gridOrigin}");
        Debug.Log($"  Pathfinding total size: {pathfindingSystem.gridWidth * pathfindingSystem.cellSize}x{pathfindingSystem.gridHeight * pathfindingSystem.cellSize}");
        pathfindingSystem.obstacleLayer = obstacleLayer;
        pathfindingSystem.showDebugPath = true;

        pathfindingGO.transform.SetParent(transform);
        
        Debug.Log($"[{name}] Pathfinding system setup completed:");
        Debug.Log($"  Grid: {gridData.cellsX}x{gridData.cellsZ}");
        Debug.Log($"  Plane Size: {planeSize}");
        Debug.Log($"  Calculated Cell Size: {pathfindingSystem.cellSize}");
        Debug.Log($"  Grid Origin: {pathfindingSystem.gridOrigin}");

        Invoke("UpdatePathfindingWalkability", 0.1f);
    }

    void UpdatePathfindingWalkability()
    {
        if (pathfindingSystem != null)
        {
            pathfindingSystem.UpdateGridWalkability();
            Debug.Log($"[{name}] Pathfinding walkability updated");
        }
    }

    void OnDrawGizmos()
    {
        if (gridData != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(planeSize.x, 0.1f, planeSize.z));

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Main Grid: {gridData.cellsX}x{gridData.cellsZ}\nPlane Size: {planeSize.x:F1}x{planeSize.z:F1}");
            #endif
        }
    }
}