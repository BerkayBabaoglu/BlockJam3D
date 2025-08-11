using UnityEngine;
using System.IO;
public class GridDataIO
{
    public static void SaveGridData(GridData data, string path)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log("Gridi kaydettik babba" + path);
    }

    public static GridData LoadGridData(string path)
    {
        if (!File.Exists(path))
        {
            Debug.Log("Grid nerde beeollum" + path);
            return null;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<GridData>(json);    
    }
}
