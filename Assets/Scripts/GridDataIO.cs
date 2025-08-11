using UnityEngine;
using System.IO;
public class GridDataIO
{
    public static void SaveGridData(GridData data, string path)
    {
        // GridData'yı SerializableGrid'e çevir
        GridData.SerializableGrid serializable = data.ToSerializable();
        string json = JsonUtility.ToJson(serializable, true);
        File.WriteAllText(path, json);
        Debug.Log($"Gridi kaydettik: {path}");
        Debug.Log($"JSON içeriği: {json}");
    }

    public static GridData LoadGridData(string path)
    {
        Debug.Log($"GridDataIO: {path} dosyası yükleniyor...");
        
        if (!File.Exists(path))
        {
            Debug.LogError($"GridDataIO: Dosya bulunamadı: {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        Debug.Log($"GridDataIO: JSON okundu, uzunluk: {json.Length} karakter");
        Debug.Log($"GridDataIO: JSON içeriği: {json}");
        
        // Önce SerializableGrid olarak yükle
        GridData.SerializableGrid serializable = JsonUtility.FromJson<GridData.SerializableGrid>(json);
        
        if (serializable == null)
        {
            Debug.LogError("GridDataIO: JSON'dan SerializableGrid oluşturulamadı!");
            return null;
        }
        
        Debug.Log($"GridDataIO: SerializableGrid oluşturuldu: {serializable.cellsX}x{serializable.cellsZ}");
        Debug.Log($"GridDataIO: cells1D array null mu? {serializable.cells1D == null}");
        
        // SerializableGrid'i GridData'ya çevir
        GridData loadedData = serializable.ToGridData();
        
        if (loadedData == null)
        {
            Debug.LogError("GridDataIO: SerializableGrid'den GridData oluşturulamadı!");
            return null;
        }
        
        Debug.Log($"GridDataIO: GridData oluşturuldu: {loadedData.cellsX}x{loadedData.cellsZ}");
        Debug.Log($"GridDataIO: cells array null mu? {loadedData.cells == null}");
        
        if (loadedData.cells != null)
        {
            Debug.Log($"GridDataIO: Cells array mevcut, boyut: {loadedData.cells.GetLength(0)}x{loadedData.cells.GetLength(1)}");
            
            // İlk birkaç hücreyi yazdır
            Debug.Log("GridDataIO: İlk 3x3 hücre:");
            for (int z = 0; z < Mathf.Min(3, loadedData.cellsZ); z++)
            {
                string row = "";
                for (int x = 0; x < Mathf.Min(3, loadedData.cellsX); x++)
                {
                    row += loadedData.GetCell(x, z) + " ";
                }
                Debug.Log($"Row {z}: {row}");
            }
        }
        
        return loadedData;    
    }
}
