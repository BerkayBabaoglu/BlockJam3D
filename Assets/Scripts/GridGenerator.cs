using Unity.VisualScripting;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject grassPrefab;
    public string jsonPath;
    float grassPrefabHeight;

    private GridData gridData;
    private Vector3 planeSize;

    private void Start()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        planeSize = mr.bounds.size;

        grassPrefabHeight = grassPrefab.GetComponentInChildren<MeshRenderer>().bounds.size.y;

        gridData = GridDataIO.LoadGridData(jsonPath);

        if (gridData == null)
        {
            Debug.Log("GridData yuklenemedi");
            return;
        }

        GenerateFromData();
    }

    void GenerateFromData()
    {
        float cellWidth = planeSize.x / gridData.cellsX;
        float cellHeight = planeSize.z / gridData.cellsZ;

        Debug.Log("PLANE SIZE : " + planeSize);
        Debug.Log("CELL WIDTH : " + cellWidth);
        Debug.Log("CELL HEIGHT: " + cellHeight);

        Vector3 startPos = transform.position - new Vector3(planeSize.x / 2, 0, planeSize.z / 2);

        for (int x = 0; x < gridData.cellsX; x++)
        {
            for (int z = 0; z < gridData.cellsZ; z++)
            {
                if (!gridData.GetCell(x, z)) continue;

                // Hücre merkezi
                Vector3 cellPos = startPos + new Vector3(
                    cellWidth * (x + 0.5f),
                    grassPrefabHeight + 2f,
                    cellHeight * (z + 0.5f)
                );

                GameObject grass = Instantiate(grassPrefab, cellPos, Quaternion.identity, transform);

                // Prefab boyutunu hücreye göre ayarla
                MeshRenderer grassRenderer = grass.GetComponent<MeshRenderer>();
                Vector3 originalSize = grassRenderer.bounds.size;

                float scaleX = cellWidth / originalSize.x;
                float scaleZ = cellHeight / originalSize.z;

                grass.transform.localScale = new Vector3(0.4969629f, 0.3911215f, 0.505403f);
            }
        }

        
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.black;

        float cellWidth = planeSize.x / gridData.cellsX;
        float cellHeight = planeSize.z / gridData.cellsZ;

        Vector3 startPos = transform.position - new Vector3(planeSize.x / 2, 0, planeSize.z / 2);

        // Dikey çizgiler
        for (int x = 0; x <= gridData.cellsX; x++)
        {
            Vector3 from = startPos + new Vector3(x * cellWidth, 0, 0);
            Vector3 to = from + new Vector3(0, 0, planeSize.z);
            Gizmos.DrawLine(from, to);

            Debug.Log("Dikey startpos:" + startPos);
            Debug.Log("Dikey planesize.z:" + planeSize.z);
            Debug.Log("Dikey x * cellWidth:" + x * cellWidth);
        }



        // Yatay çizgiler
        for (int z = 0; z <= gridData.cellsZ; z++)
        {
            Vector3 from = startPos + new Vector3(0, 0, z * cellHeight);
            Vector3 to = from + new Vector3(planeSize.x, 0, 0);
            Gizmos.DrawLine(from, to);

            Debug.Log("Yatay startpos:" + startPos);
            Debug.Log("Yatay planesize.x:" + planeSize.x);
            Debug.Log("Yatay z * cellHeight:" + z * cellHeight);
        }
    }
}