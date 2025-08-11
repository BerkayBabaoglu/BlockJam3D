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
    public static void ShowWindos()
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

        if(GUILayout.Button("New Grid"))
        {
            gridData = new GridData(cellsX, cellsZ);
        }

        if(gridData == null)
        {
            EditorGUILayout.HelpBox("Grid data yok.  New grid yap veya json yukle", MessageType.Info);
            return;
        }

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
                bool cellValue = gridData.GetCell(x, z);
                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.normal.textColor = cellValue ? Color.green : Color.red;

                if (GUILayout.Button(cellValue ? "1" : "0", style, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                {
                    gridData.SetCell(x, z, !cellValue);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Grid To JSON"))
        {
            GridDataIO.SaveGridData(gridData, jsonPath);
            AssetDatabase.Refresh();
        }

        if (GUILayout.Button("Load Grid From JSON"))
        {
            LoadGrid();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void LoadGrid()
    {
        GridData loaded = GridDataIO.LoadGridData(jsonPath);
        if (loaded != null)
        {
            gridData = loaded;
            cellsX = gridData.cellsX;
            cellsZ = gridData.cellsZ;
        }
        else
        {
            gridData = new GridData(cellsX, cellsZ);
        }
    }

}

