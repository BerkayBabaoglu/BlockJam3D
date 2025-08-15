using UnityEngine;

public class GridTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool testOnStart = true;
    public bool testGridData = true;
    public bool testGridDataIO = true;
    
    private void Start()
    {
        if (testOnStart)
        {
            RunTests();
        }
    }
    
    [ContextMenu("Run Tests")]
    public void RunTests()
    {
        Debug.Log("=== GRID TEST BAŞLADI ===");
        
        if (testGridData)
            TestGridData();
            
        if (testGridDataIO)
            TestGridDataIO();
            
        Debug.Log("=== GRID TEST BİTTİ ===");
    }
    
    private void TestGridData()
    {
        Debug.Log("--- GridData Test ---");
        
        // Test 1: Basit constructor
        GridData testGrid = new GridData(3, 3);
        Debug.Log($"Test Grid oluşturuldu: {testGrid.cellsX}x{testGrid.cellsZ}");
        Debug.Log($"Cells array null mu? {testGrid.cells == null}");
        
        // Test 2: SetCell ve GetCell
        testGrid.SetCell(1, 1, 5);
        int value = testGrid.GetCell(1, 1);
        Debug.Log($"SetCell(1,1,5) -> GetCell(1,1) = {value}");
        
        // Test 3: Bounds checking
        int outOfBounds = testGrid.GetCell(5, 5);
        Debug.Log($"Out of bounds GetCell(5,5) = {outOfBounds}");
        
        // Test 4: Tüm hücreleri yazdır
        Debug.Log("Grid içeriği:");
        for (int z = 0; z < testGrid.cellsZ; z++)
        {
            string row = "";
            for (int x = 0; x < testGrid.cellsX; x++)
            {
                row += testGrid.GetCell(x, z) + " ";
            }
            Debug.Log($"Row {z}: {row}");
        }
    }
    
    private void TestGridDataIO()
    {
        Debug.Log("--- GridDataIO Test ---");
        
        string testPath = "Assets/LevelData/level1.json";
        
        // Test 1: Dosya var mı?
        bool fileExists = System.IO.File.Exists(testPath);
        Debug.Log($"Dosya mevcut mu? {fileExists}");
        
        if (fileExists)
        {
            // Test 2: JSON yükle
            GridData loadedGrid = GridDataIO.LoadGridData(testPath);
            if (loadedGrid != null)
            {
                Debug.Log($"JSON'dan yüklendi: {loadedGrid.cellsX}x{loadedGrid.cellsZ}");
                Debug.Log($"Cells array null mu? {loadedGrid.cells == null}");
                
                if (loadedGrid.cells != null)
                {
                    // Test 3: İlk birkaç hücreyi yazdır
                    Debug.Log("İlk 3x3 hücre:");
                    for (int z = 0; z < Mathf.Min(3, loadedGrid.cellsZ); z++)
                    {
                        string row = "";
                        for (int x = 0; x < Mathf.Min(3, loadedGrid.cellsX); x++)
                        {
                            row += loadedGrid.GetCell(x, z) + " ";
                        }
                        Debug.Log($"Row {z}: {row}");
                    }
                }
            }
            else
            {
                Debug.LogError("JSON yüklenemedi!");
            }
        }
    }
}

