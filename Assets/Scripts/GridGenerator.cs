using Unity.VisualScripting;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject grassPrefab;               // cellType == 1 (Grass)
    public GameObject[] characterPrefabs;        // cellType == 2,3,4,5 -> index = cellType - 2 (Kırmızı, Yeşil, Mavi, Sarı karakterler)
    [Header("Data")]
    public string jsonPath = "Assets/LevelData/level1.json"; // GridDataIO y�kler
    float grassPrefabHeight = 0.5f;

    private GridData gridData;
    private Vector3 planeSize = Vector3.one;

    private void Start()
    {
        Debug.Log($"[{name}] GridGenerator Start başladı");
        
        // Plane / mesh renderer al
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            Debug.LogError($"[{name}] MeshRenderer bulunamadı. GridGenerator bu GameObject üzerinde bir MeshRenderer bekliyor.");
            // Yine de devam etmeyeceğiz çünkü planeSize bilinmezse hesaplar bozulur.
            return;
        }
        planeSize = mr.bounds.size;
        Debug.Log($"[{name}] MeshRenderer bulundu, plane size: {planeSize}");

        // grassPrefab yüksekliğini al (güvenli)
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

        // characterPrefabs kontrolü
        if (characterPrefabs != null && characterPrefabs.Length > 0)
        {
            Debug.Log($"[{name}] {characterPrefabs.Length} character prefab bulundu");
            for (int i = 0; i < characterPrefabs.Length; i++)
            {
                if (characterPrefabs[i] != null)
                    Debug.Log($"[{name}] Character prefab {i}: {characterPrefabs[i].name}");
                else
                    Debug.LogWarning($"[{name}] Character prefab {i} null!");
            }
        }
        else
        {
            Debug.LogWarning("Character prefabs atanmamış veya boş array.");
        }

        // Grid datayı yükle
        Debug.Log($"[{name}] JSON yükleniyor: {jsonPath}");
        gridData = GridDataIO.LoadGridData(jsonPath);
        if (gridData == null)
        {
            Debug.LogError($"GridData yüklenemedi: {jsonPath}");
            return;
        }

        Debug.Log($"[{name}] GridData yüklendi: {gridData.cellsX}x{gridData.cellsZ}");

        // Basit validasyon
        if (gridData.cellsX <= 0 || gridData.cellsZ <= 0)
        {
            Debug.LogError("GridData geçersiz cellsX/cellsZ değerleri içeriyor.");
            return;
        }

        // characterPrefabs null ise boş array yap
        if (characterPrefabs == null) characterPrefabs = new GameObject[0];

        // Generate
        Debug.Log($"[{name}] GenerateFromData çağrılıyor...");
        GenerateFromData();
    }

    void GenerateFromData()
    {
        // Güvenlik: gridData ve planeSize hazır olmalı
        if (gridData == null)
        {
            Debug.LogError("GenerateFromData çağrıldı ama gridData null.");
            return;
        }

        Debug.Log($"[{name}] Grid oluşturuluyor: {gridData.cellsX}x{gridData.cellsZ}");

        // Hücre boyutları (0 bölme koruması)
        float cellWidth = planeSize.x / Mathf.Max(1, gridData.cellsX);
        float cellHeight = planeSize.z / Mathf.Max(1, gridData.cellsZ);

        Vector3 startPos = transform.position - new Vector3(planeSize.x * 0.5f, 0f, planeSize.z * 0.5f);
        
        Debug.Log($"[{name}] Plane size: {planeSize}, Cell size: {cellWidth}x{cellHeight}");
        Debug.Log($"[{name}] Start position: {startPos}");

        int spawnedCount = 0;

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
                    int index = cellType - 2; // 0,1,2,3 -> characterPrefabs array indeksi
                    if (index >= 0 && index < characterPrefabs.Length && characterPrefabs[index] != null)
                    {
                        prefabToSpawn = characterPrefabs[index];
                        Debug.Log($"[{name}] Character prefab bulundu: {characterPrefabs[index].name} (index: {index}, cellType: {cellType})");
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

                // Instantiate ve instance üzerinden güvenli renderer/scale işlemi
                if (prefabToSpawn != null)
                {
                    Debug.Log($"[{name}] Spawning {prefabToSpawn.name} at position {cellPos}");
                    
                    GameObject obj = Instantiate(prefabToSpawn, cellPos, Quaternion.identity, transform);
                    spawnedCount++;

                    // Scale işlemi - karakterler için sabit scale, grass için hesaplanan scale
                    if (cellType == 1) // Grass - hesaplanan scale
                    {
                        // MeshRenderer çocuklarda olabilir -> GetComponentInChildren kullan
                        MeshRenderer instRenderer = obj.GetComponentInChildren<MeshRenderer>();
                        if (instRenderer != null)
                        {
                            Vector3 originalSize = instRenderer.bounds.size;

                            // orijinalSize'da 0 olma durumuna karşı koruma
                            if (originalSize.x > 0.0001f && originalSize.z > 0.0001f)
                            {
                                float scaleX = cellWidth / originalSize.x;
                                float scaleZ = cellHeight / originalSize.z;

                                // Mevcut localScale ile çarp (böylece prefab içindeki scale korunur)
                                Vector3 local = obj.transform.localScale;
                                obj.transform.localScale = new Vector3(local.x * scaleX, local.y, local.z * scaleZ);
                                
                                Debug.Log($"[{name}] {obj.name} (grass) scaled to {obj.transform.localScale}");
                            }
                            else
                            {
                                // Eğer bounds ölçülemezse default bir scale işlemi yapma ama uyar
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
                    }
                }
            }
        }
        
        Debug.Log($"[{name}] Toplam {spawnedCount} obje spawn edildi");
    }

    void OnDrawGizmos()
    {
        // Sadece oynat�rken ve gridData varsa �iz
        if (!Application.isPlaying || gridData == null) return;

        if (gridData.cellsX <= 0 || gridData.cellsZ <= 0) return;

        Gizmos.color = Color.black;

        float cellWidth = planeSize.x / Mathf.Max(1, gridData.cellsX);
        float cellHeight = planeSize.z / Mathf.Max(1, gridData.cellsZ);
        Vector3 startPos = transform.position - new Vector3(planeSize.x * 0.5f, 0f, planeSize.z * 0.5f);

        // Dikey �izgiler
        for (int x = 0; x <= gridData.cellsX; x++)
        {
            Vector3 from = startPos + new Vector3(x * cellWidth, 0, 0);
            Vector3 to = from + new Vector3(0, 0, planeSize.z);
            Gizmos.DrawLine(from, to);
        }

        // Yatay �izgiler
        for (int z = 0; z <= gridData.cellsZ; z++)
        {
            Vector3 from = startPos + new Vector3(0, 0, z * cellHeight);
            Vector3 to = from + new Vector3(planeSize.x, 0, 0);
            Gizmos.DrawLine(from, to);
        }
    }
}