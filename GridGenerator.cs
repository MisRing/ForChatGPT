using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GridGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject hexPref;

    [SerializeField, Range(0, 5)]
    private int abyssRad = 1;
    [SerializeField, Range(0, 10)]
    private int terrainRad = 5;
    [SerializeField, Range(0, 10)]
    private int sceneryRad = 2;

    [SerializeField, Range(0.1f, 1f)]
    private float noiseScale = 0.2f;
    [SerializeField, Range(0, 10)]
    private int sceneryHeightModifire = 2;

    [SerializeField]
    private List<HexCell> hexCells = new List<HexCell>();
    [SerializeField]
    private HexMesh hexMesh;

    [SerializeField]
    public int seed = 0;

    [SerializeField]
    private bool drawGizmos = true;

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, abyssRad * HexMetrics.fullDiameter * 0.75f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, (terrainRad + abyssRad) * HexMetrics.fullDiameter * 0.75f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, (abyssRad + terrainRad + sceneryRad) * HexMetrics.fullDiameter * 0.75f);
    }

    [ContextMenu("Generation/Generate Terrain")]
    public void GenerateTerrain()
    {
        if (!hexPref)
        {
            Debug.LogError("Hex Prefabs are not assigned!");
            return;
        }

        // Удаление старых гексов
        while (hexCells.Count > 0)
        {
            #if UNITY_EDITOR
                if (hexCells[0] != null)
                    DestroyImmediate(hexCells[0].gameObject);
            #else
                if (hexCells[0] != null)
                    Destroy(hexCells[0]);
            #endif

            hexCells.RemoveAt(0);
        }

        int absolutRad = abyssRad + terrainRad + sceneryRad;

        for (int xH = -absolutRad; xH <= absolutRad; xH++)
        {
            for (int zH = -absolutRad; zH <= absolutRad; zH++)
            {

                int cH = -xH - zH; // Третья координата

                int range = Mathf.Max(Mathf.Abs(xH), Mathf.Abs(zH), Mathf.Abs(cH));

                if (range >= absolutRad || range < abyssRad)
                    continue;

                // Мировые координаты
                Vector2 position2D = HexMetrics.HexToWorldPosition(new Vector3Int(xH, zH, cH));
                int height = (int)(Mathf.PerlinNoise((position2D.x + seed) * noiseScale, (position2D.y + seed) * noiseScale) * 5) * (int)Mathf.Sqrt(range) + range;
                if (range >= absolutRad - sceneryRad)
                    height += sceneryHeightModifire;
                float smoothHeight = height * HexMetrics.levelHeight;

                Vector3 position = new Vector3(position2D.x, smoothHeight, position2D.y);

                GameObject hexObj = Instantiate(hexPref, transform);
                hexObj.transform.localPosition = position;
                HexCell hex = hexObj.GetComponent<HexCell>();
                hexCells.Add(hex);
                hex.inGridPosition = new Vector3Int(xH, zH, cH);
                hex.height = height;
                if (range >= absolutRad - sceneryRad)
                    hex.color = Color.grey;
                else hex.color = Color.white;
            }
        }

        hexMesh.Triangulate(hexCells);

        FindObjectOfType<CameraController>().radiusBounds = (abyssRad + terrainRad) * HexMetrics.fullDiameter * 0.75f;
    }
}
