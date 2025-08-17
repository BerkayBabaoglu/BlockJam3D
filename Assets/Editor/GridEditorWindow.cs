using UnityEngine;
using UnityEditor;

public class GridEditorWindow : EditorWindow
{
    private GridData gridData;
    private Vector2 scrollPos;

    private int cellsX = 10;
    private int cellsZ = 10;
    private string jsonPath = "Assets/LevelData/level1.json";

    private const float cellSize = 25f;

    [MenuItem("Tools/Grid Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<GridEditorWindow>("Grid Level Editor");
    }

    private void OnEnable()
    {
        LoadGrid();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Grid Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        cellsX = EditorGUILayout.IntField("Cells X", cellsX);
        cellsZ = EditorGUILayout.IntField("Cells Z", cellsZ);
        jsonPath = EditorGUILayout.TextField("JSON Path", jsonPath);

        if (GUILayout.Button("New Grid"))
        {
            gridData = new GridData(cellsX, cellsZ);
        }

        if (gridData != null)
        {
            if (gridData.cellsX != cellsX || gridData.cellsZ != cellsZ)
            {
                gridData = new GridData(cellsX, cellsZ);
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));

            for (int z = gridData.cellsZ - 1; z >= 0; z--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < gridData.cellsX; x++)
                {
                    int cellValue = 0;
                    if (gridData != null && gridData.cells != null)
                    {
                        cellValue = gridData.GetCell(x, z);
                    }
                    
                    GUIStyle style = new GUIStyle(GUI.skin.button);

                    switch (cellValue)
                    {
                        case 0: style.normal.textColor = Color.white; break;  // 0: bos
                        case 1: style.normal.textColor = Color.green; break; // 1: grass
                        case 2: style.normal.textColor = Color.red; break;   // 2: kirmizi
                        case 3: style.normal.textColor = Color.green; break; // 3: yesil
                        case 4: style.normal.textColor = Color.blue; break;  // 4: mavi 
                        case 5: style.normal.textColor = Color.yellow; break; // 5: sari
                    }

                    if (GUILayout.Button(cellValue.ToString(), style, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        if (gridData != null && gridData.cells != null)
                        {
                            int current = gridData.GetCell(x, z);
                            current = (current + 1) % 6; // 0-5 arasi doner
                            gridData.SetCell(x, z, current);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Grid To JSON"))
            {
                if (gridData != null)
                {
                    GridDataIO.SaveGridData(gridData, jsonPath);
                    AssetDatabase.Refresh();
                }
            }
            if (GUILayout.Button("Load Grid From JSON"))
            {
                LoadGrid();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Grid data yok. New Grid yap veya JSON yükle.", MessageType.Info);
        }
    }

    private void LoadGrid()
    {
        GridData loaded = GridDataIO.LoadGridData(jsonPath);
        if (loaded != null)
        {
            gridData = loaded;
            cellsX = gridData.cellsX;
            cellsZ = gridData.cellsZ;
            Debug.Log($"Grid yüklendi: {cellsX}x{cellsZ}");
        }
        else
        {
            Debug.LogWarning("JSON yüklenemedi, yeni grid oluşturuluyor");
            gridData = new GridData(cellsX, cellsZ);
        }
    }
}
