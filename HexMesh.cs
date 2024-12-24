using UnityEngine;
using System.Collections.Generic;
using UnityEditor.iOS.Xcode;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{

    Mesh hexMesh;
    List<Vector3> vertices;
    List<int> triangles;
    List<Color> colors;

    [SerializeField]
    public Texture2D noiseSource;

    [SerializeField]
    private bool drawGizmos = true;

    private void OnDrawGizmos()
    {
        if (!drawGizmos || hexMesh == null)
            return;

        // Получение массивов вершин и треугольников
        Vector3[] vertices = hexMesh.vertices;
        int[] triangles = hexMesh.triangles;

        Gizmos.color = Color.yellow;

        // Отображение треугольников
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v1 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 2]]);

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }

        // Отображение вершин
        Gizmos.color = Color.red;
        foreach (Vector3 vertex in vertices)
        {
            Gizmos.DrawSphere(transform.TransformPoint(vertex), 0.01f);
        }
    }


    void SetSettings()
    {
        HexMetrics.noiseSource = noiseSource;

        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        hexMesh.name = "Hex Mesh";
        vertices = new List<Vector3>();
        colors = new List<Color>();
        triangles = new List<int>();
        hexMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    public void Triangulate(List<HexCell> cells)
    {
        SetSettings();
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();

        foreach (HexCell cell in cells)
        {
            TriangulateCell(cell, cells);
        }

        UpdateMesh();
    }

    void TriangulateCell(HexCell cell, List<HexCell> cells)
    {
        TriangulateEdges(cell, cells);
        TriangulateNodes(cell, cells);
        TriangulateHex(cell);
    }

    void UpdateMesh()
    {
        hexMesh.vertices = vertices.ToArray();
        hexMesh.colors = colors.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.RecalculateNormals();
    }


    void TriangulateNodes(HexCell cell, List<HexCell> cells)
    {
        Vector3 currentCenter = cell.transform.localPosition;
        Vector3 neighbor1Center;
        Vector3 neighbor2Center;

        for (int i = 0; i < 6; i++)
        {
            HexCell neighbor1 = HexMetrics.GetNeighbor(cell, HexMetrics.directions[i], cells);
            HexCell neighbor2 = HexMetrics.GetNeighbor(cell, HexMetrics.directions[(i + 1) % 6], cells);

            // Проверка приоритета генерации (только для уникальных нод)
            if (HasGenerationPriority(cell, neighbor1, neighbor2))
            {
                // Если нет одного из соседей, генерируем виртуальный центр
                neighbor1Center = neighbor1 != null
                ? neighbor1.transform.localPosition
                : HexMetrics.HexToWorldPosition(cell.inGridPosition + HexMetrics.directions[i], -20);
                neighbor2Center = neighbor2 != null
                    ? neighbor2.transform.localPosition
                    : HexMetrics.HexToWorldPosition(cell.inGridPosition + HexMetrics.directions[(i + 1) % 6], -20);

                // Вычисляем вершины для ноды
                Vector3 vertex1 = currentCenter + HexMetrics.corners[i + 1];
                Vector3 vertex2 = neighbor1Center + HexMetrics.corners[(i + 2) % 6 + 1];
                Vector3 vertex3 = neighbor2Center + HexMetrics.corners[(i + 4) % 6 + 1];

                // Создаём ноду
                AddNode(new Vector3[3] { 
                    vertex1, vertex2, vertex3},
                    new int[3] {
                    cell.height,
                    (neighbor1 != null ? neighbor1.height : -20),
                    (neighbor2 != null ? neighbor2.height : -20) });
            }
        }
    }

    bool HasGenerationPriority(HexCell current, HexCell neighbor1, HexCell neighbor2)
    {
        Vector3Int currentPos = current.inGridPosition;

        Vector3Int neighbor1Pos = neighbor1?.inGridPosition ?? HexMetrics.MissingNeighbor;
        Vector3Int neighbor2Pos = neighbor2?.inGridPosition ?? HexMetrics.MissingNeighbor;

        // Проверка приоритета: текущая ячейка должна быть "меньше" обоих соседей
        return IsLexicographicallySmaller(currentPos, neighbor1Pos) && IsLexicographicallySmaller(currentPos, neighbor2Pos);
    }

    bool IsLexicographicallySmaller(Vector3Int a, Vector3Int b)
    {
        // Лексикографическое сравнение
        if (a.x != b.x) return a.x < b.x;
        if (a.y != b.y) return a.y < b.y;
        return a.z < b.z;
    }


    void AddNode(Vector3[] verts, int[] heights)
    {
        MinHexPoint(ref verts, ref heights);


        switch (CheckNodeType(heights[0], heights[1], heights[2]))
        {
            case (NodeType.simple):
                AddSimpleNode(verts, heights);
                break;
            case (NodeType.stairs):
                AddStairsNode(verts, heights);
                break;
            case (NodeType.stairs1to2):
                AddStairs1to2Node(verts, heights);
                break;
            case (NodeType.stairs2to1):
                AddStairs2to1Node(verts, heights);
                break;
            case (NodeType.brockenStairs):
                AddBrokenStairsNode(verts, heights);
                break;

        }
    }

    void AddSimpleNode(Vector3[] verts, int[] heights) //DONE!!!
    {
        AddTriangle(verts[0], verts[1], verts[2], Color.white);
    }

    void AddStairsNode(Vector3[] verts, int[] heights) //DONE!!!
    {
        Vector3 vertex1;
        Vector3 vertex2;
        Vector3 vertex3;
        Vector3 vertex4;

        Vector3 mathVert1 = verts[1] - verts[0];
        Vector3 mathVert2 = verts[2] - verts[0];

        int steps1 = Mathf.Abs(heights[0] - heights[1]);
        int steps2 = Mathf.Abs(heights[0] - heights[2]);
        int neededSteps1 = steps1 * 2 - 1 <= 2 ? 2 : steps1 * 2 - 1;
        int neededSteps2 = steps2 * 2 - 1 <= 2 ? 2 : steps2 * 2 - 1;

        for (int i = 1; i < Mathf.Min(neededSteps1, neededSteps2); i++)
        {
            vertex1 = new Vector3(  mathVert1.x / neededSteps1 * i,
                                    mathVert1.y / steps1 * ((i + 1) / 2),
                                    mathVert1.z / neededSteps1 * i);
            vertex1 += verts[0];

            vertex2 = new Vector3(  mathVert2.x / neededSteps2 * i,
                                    mathVert2.y / steps2 * ((i + 1) / 2),
                                    mathVert2.z / neededSteps2 * i);
            vertex2 += verts[0];

            vertex3 = new Vector3(  mathVert1.x / neededSteps1 * (i + 1),
                                    mathVert1.y / steps1 * ((i + 2) / 2),
                                    mathVert1.z / neededSteps1 * (i + 1));
            vertex3 += verts[0];

            vertex4 = new Vector3(  mathVert2.x / neededSteps2 * (i + 1),
                                    mathVert2.y / steps2 * ((i + 2) / 2),
                                    mathVert2.z / neededSteps2 * (i + 1));
            vertex4 += verts[0];

            AddTriangle(vertex1, vertex3, vertex2, Color.white);
            AddTriangle(vertex2, vertex3, vertex4, Color.white);
        }

        vertex3 = new Vector3(  mathVert1.x / neededSteps1,
                                mathVert1.y / steps1,
                                mathVert1.z / neededSteps1);
        vertex3 += verts[0];

        vertex4 = new Vector3(  mathVert2.x / neededSteps2,
                                mathVert2.y / steps2,
                                mathVert2.z / neededSteps2);
        vertex4 += verts[0];

        AddTriangle(verts[0], vertex3, vertex4, Color.white);

        if (heights[1] > heights[2])
        {
            int mod = neededSteps2;
            steps2 = Mathf.Abs(heights[1] - heights[2]);
            neededSteps2 = steps2 * 2 - 1 <= 2 ? 2 : steps2 * 2 - 1;
            mathVert2 = verts[1] - verts[2];

            for (int i = 0; i < neededSteps2 - 1; i++)
            {
                vertex1 = new Vector3(  mathVert1.x / neededSteps1 * (i + mod),
                                        mathVert1.y / steps1 * ((i + 1 + mod) / 2),
                                        mathVert1.z / neededSteps1 * (i + mod));
                vertex1 += verts[0];

                vertex2 = new Vector3(  mathVert2.x / neededSteps2 * i,
                                        mathVert2.y / steps2 * ((i + 1) / 2),
                                        mathVert2.z / neededSteps2 * i);
                vertex2 += verts[2];

                vertex3 = new Vector3(  mathVert1.x / neededSteps1 * (i + 1 + mod),
                                        mathVert1.y / steps1 * ((i + 2 + mod) / 2),
                                        mathVert1.z / neededSteps1 * (i + 1 + mod));
                vertex3 += verts[0];

                vertex4 = new Vector3(  mathVert2.x / neededSteps2 * (i + 1),
                                        mathVert2.y / steps2 * ((i + 2) / 2),
                                        mathVert2.z / neededSteps2 * (i + 1));
                vertex4 += verts[2];

                AddTriangle(vertex1, vertex4, vertex2, Color.white);
                AddTriangle(vertex1, vertex3, vertex4, Color.white);
            }

            vertex1 = new Vector3(  mathVert1.x / neededSteps1 * (neededSteps2 - 1 + mod),
                                    mathVert1.y / steps1 * ((neededSteps2 + mod) / 2),
                                    mathVert1.z / neededSteps1 * (neededSteps2 - 1 + mod));
            vertex1 += verts[0];

            vertex2 = new Vector3(  mathVert2.x / neededSteps2 * (neededSteps2 - 1),
                                    mathVert2.y / steps2 * (neededSteps2 / 2),
                                    mathVert2.z / neededSteps2 * (neededSteps2 - 1));
            vertex2 += verts[2];

            AddTriangle(vertex1, verts[1], vertex2, Color.white);
        }
        else
        {
            int mod = neededSteps1;
            steps1 = Mathf.Abs(heights[1] - heights[2]);
            neededSteps1 = steps1 * 2 - 1 <= 2 ? 2 : steps1 * 2 - 1;
            mathVert1 = verts[2] - verts[1];

            for (int i = 0; i < neededSteps1 - 1; i++)
            {
                vertex1 = new Vector3(  mathVert1.x / neededSteps1 * i,
                                        mathVert1.y / steps1 * ((i + 1) / 2),
                                        mathVert1.z / neededSteps1 * i);
                vertex1 += verts[1];

                vertex2 = new Vector3(  mathVert2.x / neededSteps2 * (i + mod),
                                        mathVert2.y / steps2 * ((i + 1 + mod) / 2),
                                        mathVert2.z / neededSteps2 * (i + mod));
                vertex2 += verts[0];

                vertex3 = new Vector3(  mathVert1.x / neededSteps1 * (i + 1),
                                        mathVert1.y / steps1 * ((i + 2) / 2),
                                        mathVert1.z / neededSteps1 * (i + 1));
                vertex3 += verts[1];

                vertex4 = new Vector3(  mathVert2.x / neededSteps2 * (i + 1 + mod),
                                        mathVert2.y / steps2 * ((i + 2 + mod) / 2),
                                        mathVert2.z / neededSteps2 * (i + 1 + mod));
                vertex4 += verts[0];

                AddTriangle(vertex1, vertex3, vertex2, Color.white);
                AddTriangle(vertex2, vertex3, vertex4, Color.white);
            }

            vertex1 = new Vector3(  mathVert1.x / neededSteps1 * (neededSteps1 - 1),
                                    mathVert1.y / steps1 * ((neededSteps1 - 1 + 1) / 2),
                                    mathVert1.z / neededSteps1 * (neededSteps1 - 1));
            vertex1 += verts[1];

            vertex2 = new Vector3(  mathVert2.x / neededSteps2 * (neededSteps1 - 1 + mod),
                                    mathVert2.y / steps2 * ((neededSteps1 - 1 + 1 + mod) / 2),
                                    mathVert2.z / neededSteps2 * (neededSteps1 - 1 + mod));
            vertex2 += verts[0];


            AddTriangle(vertex1, verts[2], vertex2, Color.white);
        }
    }

    void AddStairs1to2Node(Vector3[] verts, int[] heights) //DONE!!!
    {
        Vector3 vertex1;
        Vector3 vertex2;
        Vector3 vertex3;
        Vector3 vertex4;

        Vector3 mathVert1 = verts[1] - verts[0];
        Vector3 mathVert2 = verts[2] - verts[0];

        int steps = Mathf.Abs(heights[0] - heights[1]);
        int neededSteps = steps * 2 - 1 <= 2 ? 2 : steps * 2 - 1;

        for (int i = 1; i < neededSteps; i++)
        {
            vertex1 = new Vector3(  mathVert1.x / neededSteps * i,
                                    mathVert1.y / steps * ((i + 1) / 2),
                                    mathVert1.z / neededSteps * i);
            vertex1 += verts[0];

            vertex2 = new Vector3(  mathVert2.x / neededSteps * i,
                                    mathVert2.y / steps * ((i + 1) / 2),
                                    mathVert2.z / neededSteps * i);
            vertex2 += verts[0];

            vertex3 = new Vector3(  mathVert1.x / neededSteps * (i + 1),
                                    mathVert1.y / steps * ((i + 2) / 2),
                                    mathVert1.z / neededSteps * (i + 1));
            vertex3 += verts[0];

            vertex4 = new Vector3(  mathVert2.x / neededSteps * (i + 1),
                                    mathVert2.y / steps * ((i + 2) / 2),
                                    mathVert2.z / neededSteps * (i + 1));
            vertex4 += verts[0];

            AddTriangle(vertex1, vertex3, vertex2, Color.white);
            AddTriangle(vertex2, vertex3, vertex4, Color.white);
        }

        vertex3 = new Vector3(  mathVert1.x / neededSteps,
                                mathVert1.y / steps,
                                mathVert1.z / neededSteps);
        vertex3 += verts[0];

        vertex4 = new Vector3(  mathVert2.x / neededSteps,
                                mathVert2.y / steps,
                                mathVert2.z / neededSteps);
        vertex4 += verts[0];

        AddTriangle(verts[0], vertex3, vertex4, Color.white);

    }

    void AddStairs2to1Node(Vector3[] verts, int[] heights) //DONE!!!
    {
        Vector3 vertex1;
        Vector3 vertex2;
        Vector3 vertex3;
        Vector3 vertex4;

        Vector3 mathVert1 = verts[2] - verts[1];
        Vector3 mathVert2 = verts[2] - verts[0];

        int steps = Mathf.Abs(heights[0] - heights[2]);
        int neededSteps = steps * 2 - 1 <= 2 ? 2 : steps * 2 - 1;

        for (int i = 0; i < neededSteps - 1; i++)
        {
            vertex1 = new Vector3(  mathVert1.x / neededSteps * i,
                                    mathVert1.y / steps * ((i + 1) / 2),
                                    mathVert1.z / neededSteps * i);
            vertex1 += verts[1];

            vertex2 = new Vector3(  mathVert2.x / neededSteps * i,
                                    mathVert2.y / steps * ((i + 1) / 2),
                                    mathVert2.z / neededSteps * i);
            vertex2 += verts[0];

            vertex3 = new Vector3(  mathVert1.x / neededSteps * (i + 1),
                                    mathVert1.y / steps * ((i + 2) / 2),
                                    mathVert1.z / neededSteps * (i + 1));
            vertex3 += verts[1];

            vertex4 = new Vector3(  mathVert2.x / neededSteps * (i + 1),
                                    mathVert2.y / steps * ((i + 2) / 2),
                                    mathVert2.z / neededSteps * (i + 1));
            vertex4 += verts[0];

            AddTriangle(vertex1, vertex3, vertex2, Color.white);
            AddTriangle(vertex2, vertex3, vertex4, Color.white);
        }

        vertex1 = new Vector3(  mathVert1.x / neededSteps * (neededSteps - 1),
                                mathVert1.y / steps * (neededSteps / 2),
                                mathVert1.z / neededSteps * (neededSteps - 1));
        vertex1 += verts[1];

        vertex2 = new Vector3(  mathVert2.x / neededSteps * (neededSteps - 1),
                                mathVert2.y / steps * (neededSteps / 2),
                                mathVert2.z / neededSteps * (neededSteps - 1));
        vertex2 += verts[0];

        AddTriangle(vertex1, verts[2], vertex2, Color.white);
    }

    void AddBrokenStairsNode(Vector3[] verts, int[] heights) //DONE!!!
    {
        if (heights[1] < heights[2])
        {
            Vector3 midVert = (verts[2] - verts[0]) * (verts[1].y - verts[0].y) / (verts[2].y - verts[0].y) + verts[0];

            Vector3 vertex1;
            Vector3 vertex2;

            Vector3 mathVert;

            int steps;
            int neededSteps;

            steps = heights[1] - heights[0];

            if (steps > 3)
            {
                AddTriangle(verts[0], verts[1], midVert, Color.white);
            }
            else
            {
                mathVert = verts[1] - verts[0];

                neededSteps = steps * 2 - 1 <= 2 ? 2 : steps * 2 - 1;

                for (int i = 0; i < neededSteps; i++)
                {
                    vertex1 = new Vector3(  mathVert.x / neededSteps * i,
                                            mathVert.y / steps * ((i + 1) / 2),
                                            mathVert.z / neededSteps * i);
                    vertex1 += verts[0];

                    vertex2 = new Vector3(  mathVert.x / neededSteps * (i + 1),
                                            mathVert.y / steps * ((i + 2) / 2),
                                            mathVert.z / neededSteps * (i + 1));
                    vertex2 += verts[0];

                    AddTriangle(vertex1, vertex2, midVert, Color.white);
                }
            }

            steps = heights[2] - heights[1];

            if (steps > 3)
            {
                AddTriangle(verts[1], verts[2], midVert, Color.white);
            }
            else
            {
                mathVert = verts[2] - verts[1];

                neededSteps = steps * 2 - 1 <= 2 ? 2 : steps * 2 - 1;

                for (int i = 0; i < neededSteps; i++)
                {
                    vertex1 = new Vector3(  mathVert.x / neededSteps * i,
                                            mathVert.y / steps * ((i + 1) / 2),
                                            mathVert.z / neededSteps * i);
                    vertex1 += verts[1];

                    vertex2 = new Vector3(  mathVert.x / neededSteps * (i + 1),
                                            mathVert.y / steps * ((i + 2) / 2),
                                            mathVert.z / neededSteps * (i + 1));
                    vertex2 += verts[1];

                    AddTriangle(vertex1, vertex2, midVert, Color.white);
                }
            }

        }
        else
        {
            Vector3 midVert = (verts[1] - verts[0]) * (verts[2].y - verts[0].y) / (verts[1].y - verts[0].y) + verts[0];

            Vector3 vertex1;
            Vector3 vertex2;

            Vector3 mathVert;

            int steps;
            int neededSteps;

            steps = heights[2] - heights[0];

            if (steps > 3)
            {
                AddTriangle(verts[0], midVert, verts[2], Color.white);
            }
            else
            {
                mathVert = verts[2] - verts[0];

                neededSteps = steps * 2 - 1 <= 2 ? 2 : steps * 2 - 1;

                for (int i = 0; i < neededSteps; i++)
                {
                    vertex1 = new Vector3(  mathVert.x / neededSteps * i,
                                            mathVert.y / steps * ((i + 1) / 2),
                                            mathVert.z / neededSteps * i);
                    vertex1 += verts[0];

                    vertex2 = new Vector3(  mathVert.x / neededSteps * (i + 1),
                                            mathVert.y / steps * ((i + 2) / 2),
                                            mathVert.z / neededSteps * (i + 1));
                    vertex2 += verts[0];

                    AddTriangle(vertex1, midVert, vertex2, Color.white);
                }
            }

            steps = heights[1] - heights[2];

            if (steps > 3)
            {
                AddTriangle(midVert, verts[1], verts[2], Color.white);
            }
            else
            {
                mathVert = verts[1] - verts[2];

                neededSteps = steps * 2 - 1 <= 2 ? 2 : steps * 2 - 1;

                for (int i = 0; i < neededSteps; i++)
                {
                    vertex1 = new Vector3(  mathVert.x / neededSteps * i,
                                            mathVert.y / steps * ((i + 1) / 2),
                                            mathVert.z / neededSteps * i);
                    vertex1 += verts[2];

                    vertex2 = new Vector3(  mathVert.x / neededSteps * (i + 1),
                                            mathVert.y / steps * ((i + 2) / 2),
                                            mathVert.z / neededSteps * (i + 1));
                    vertex2 += verts[2];

                    AddTriangle(midVert, vertex2, vertex1, Color.white);
                }
            }
        }
    }

    private NodeType CheckNodeType(int height1, int height2, int height3)
    {
        int dif1 = Mathf.Abs(height1 - height2);
        int dif2 = Mathf.Abs(height1 - height3);
        int dif3 = Mathf.Abs(height2 - height3);

        if(dif1 > 3 && dif2 > 3 && dif3 > 3)
            return NodeType.simple;
        if (dif1 == 0 && dif2 > 3 && dif3 > 3)
            return NodeType.simple;
        if (dif1 > 3 && dif2 == 0 && dif3 > 3)
            return NodeType.simple;
        if (dif1 > 3 && dif2 > 3 && dif3 == 0)
            return NodeType.simple;
        if (dif1 == 0 && dif2 == 0 && dif3 == 0)
            return NodeType.simple;

        if (dif3 == 0 && dif1 <= 3)
            return NodeType.stairs1to2;

        if (dif1 == 0 && dif2 <= 3)
            return NodeType.stairs2to1;

        if (dif1 <= 3 && dif2 <= 3 && dif3 <= 3)
            return NodeType.stairs;

        return NodeType.brockenStairs;
    }

    public enum NodeType
    {
        simple = 0,
        stairs = 1,
        stairs1to2 = 2,
        stairs2to1 = 3,
        brockenStairs = 4,
    }

    private void MinHexPoint(ref Vector3[] vectors, ref int[] ints)
    {
        while (true)
        {
            int h = ints[0];
            Vector3 v = vectors[0];

            vectors[0] = vectors[1];
            ints[0] = ints[1];
            vectors[1] = vectors[2];
            ints[1] = ints[2];

            ints[2] = h;
            vectors[2] = v;

            if (ints[0] <= ints[1] && ints[0] <= ints[2])
                break;
        }

        if (ints[0] == ints[2])
        {
            int h = ints[2];
            Vector3 v = vectors[2];

            vectors[2] = vectors[1];
            ints[2] = ints[1];
            vectors[1] = vectors[0];
            ints[1] = ints[0];

            ints[0] = h;
            vectors[0] = v;
        }
    }

    void TriangulateEdges(HexCell cell, List<HexCell> cells)
    {
        Vector3 currentCenter = cell.transform.localPosition;
        Vector3 neighborCenter;

        // Вычисляем точки для мостика
        Vector3 vertex1;
        Vector3 vertex2;
        Vector3 vertex3;
        Vector3 vertex4;

        for (int i = 0; i < 6; i++)
        {
            HexCell neighbor = HexMetrics.GetNeighbor(cell, HexMetrics.directions[i], cells);

            if (neighbor != null && !IsLexicographicallySmaller(cell.inGridPosition, neighbor.inGridPosition))
                continue;

            if (neighbor == null)
                neighborCenter = HexMetrics.HexToWorldPosition(cell.inGridPosition + HexMetrics.directions[i], -20);
            else 
                neighborCenter = neighbor.transform.localPosition;

            // Вычисляем точки для мостика
            vertex1 = currentCenter + HexMetrics.corners[i];       // Текущая грань 1
            vertex2 = currentCenter + HexMetrics.corners[i + 1];   // Текущая грань 2
            vertex3 = neighborCenter + HexMetrics.corners[(i + 3) % 6 + 1]; // Грань соседа 1
            vertex4 = neighborCenter + HexMetrics.corners[(i + 2) % 6 + 1]; // Грань соседа 2

            int steps = neighbor == null ? 4 : Mathf.Abs(cell.height - neighbor.height);
            // Добавляем два треугольника для моста


            int left = (i + 5) % 6;
            int right = (i + 1) % 6;
            if (vertex1.y > vertex3.y)
            {
                Vector3 v = vertex1;
                vertex1 = vertex4;
                vertex4 = v;
                v = vertex2;
                vertex2 = vertex3;
                vertex3 = v;

                int t = left;
                left = right;
                right = t;
            }

            if (steps <= 3 && steps > 0)
            {
                AddBridge(vertex1, vertex2, vertex3, vertex4, Color.white, steps);
            }
            else
            {
                HexCell neighborLeft = HexMetrics.GetNeighbor(cell, HexMetrics.directions[left], cells);
                HexCell neighborRight = HexMetrics.GetNeighbor(cell, HexMetrics.directions[right], cells);

                Vector3 dotLeft = new Vector3(0, -999, 0);
                Vector3 dotRight = new Vector3(0, -999, 0);

                if (neighborLeft != null && neighborLeft.transform.localPosition.y < vertex3.y && neighborLeft.transform.localPosition.y > vertex1.y)
                {
                    int stepsNeg = neighbor == null ? 4 : Mathf.Abs(neighborLeft.height - neighbor.height);
                    stepsNeg = stepsNeg <= 3 ? stepsNeg : Mathf.Abs(neighborLeft.height - cell.height);
                    if (stepsNeg <= 3)
                    {

                        dotLeft =   (vertex3 - vertex1)
                                    * (neighborLeft.transform.localPosition.y - vertex1.y)
                                    / (vertex3.y - vertex1.y) + vertex1;
                    }
                }

                if (neighborRight != null && neighborRight.transform.localPosition.y < vertex4.y && neighborRight.transform.localPosition.y > vertex2.y)
                {
                    int stepsNeg = neighbor == null ? 4 : Mathf.Abs(neighborRight.height - neighbor.height);
                    stepsNeg = stepsNeg <= 3 ? stepsNeg : Mathf.Abs(neighborRight.height - cell.height);
                    if (stepsNeg <= 3)
                    {
                        dotRight =  (vertex4 - vertex2)
                                    * (neighborRight.transform.localPosition.y - vertex2.y)
                                    / (vertex4.y - vertex2.y) + vertex2;
                    }
                }

                AddSimpleBridge(vertex1, vertex2, vertex3, vertex4, Color.white, steps, dotLeft, dotRight);
            }
        }
    }

    void AddSimpleBridge(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color col, int steps, Vector3 dotLeft, Vector3 dotRight)
    {
        if (steps == 0)
        {
            AddTriangle(v1, v3, v2, col);
            AddTriangle(v2, v3, v4, col);
        }
        else if (steps > 3)
        {
            if(dotLeft != new Vector3(0, -999, 0) && dotRight != new Vector3(0, -999, 0))
            {
                if (dotLeft.y < dotRight.y)
                {
                    AddTriangle(v1, dotLeft, v2, col);
                    AddTriangle(v2, dotLeft, dotRight, col);
                    AddTriangle(dotLeft, v3, dotRight, col);
                    AddTriangle(dotRight, v3, v4, col);
                }
                else
                {
                    AddTriangle(v1, dotRight, v2, col);
                    AddTriangle(v1, dotLeft, dotRight, col);
                    AddTriangle(dotLeft, v4, dotRight, col);
                    AddTriangle(dotLeft, v3, v4, col);
                }
            }
            else if(dotLeft != new Vector3(0, -999, 0))
            {
                AddTriangle(v1, dotLeft, v2, col);
                AddTriangle(v2, dotLeft, v4, col);
                AddTriangle(dotLeft, v3, v4, col);
            }
            else if(dotRight != new Vector3(0, -999, 0))
            {
                AddTriangle(v1, dotRight, v2, col);
                AddTriangle(v1, v3, dotRight, col);
                AddTriangle(v3, v4, dotRight, col);
            }
            else
            {
                AddTriangle(v1, v3, v2, col);
                AddTriangle(v2, v3, v4, col);
            }
        }
    }


    void AddBridge(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color col, int steps)
    {
        Vector3 vertex1;
        Vector3 vertex2;
        Vector3 vertex3;
        Vector3 vertex4;

        Vector3 mathVert1 = v3 - v1;
        Vector3 mathVert2 = v4 - v2;

        int neededSteps = steps * 2 - 1 <= 2 ? 2 : steps * 2 - 1;

        for (int i = 0; i < neededSteps; i++)
        {
            vertex1 = new Vector3(mathVert1.x / neededSteps * i,
                                    mathVert1.y / steps * ((i + 1) / 2),
                                    mathVert1.z / neededSteps * i);
            vertex1 += v1;

            vertex2 = new Vector3(mathVert1.x / neededSteps * i,
                                    mathVert1.y / steps * ((i + 1) / 2),
                                    mathVert1.z / neededSteps * i);
            vertex2 += v2;

            vertex3 = new Vector3(mathVert2.x / neededSteps * (i + 1),
                                    mathVert2.y / steps * ((i + 2) / 2),
                                    mathVert2.z / neededSteps * (i + 1));
            vertex3 += v1;

            vertex4 = new Vector3(mathVert2.x / neededSteps * (i + 1),
                                    mathVert2.y / steps * ((i + 2) / 2),
                                    mathVert2.z / neededSteps * (i + 1));
            vertex4 += v2;

            AddTriangle(vertex1, vertex3, vertex2, col);
            AddTriangle(vertex2, vertex3, vertex4, col);
        }

    }

    void TriangulateHex(HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        for (int i = 0; i < 6; i++)
        {
            AddTriangle(
                center,
                center + HexMetrics.corners[i],
                center + HexMetrics.corners[i + 1],
                cell.color
            );
        }

        cell.transform.localPosition += Vector3.up * 0.1f;
        cell.transform.localPosition = Perturb(cell.transform.localPosition);
    }

    void AddVertex(Vector3 vertex, Color color)
    {
        vertices.Add(vertex);
        colors.Add(color);
    }

    void AddTriangleIndices(int v1, int v2, int v3)
    {
        triangles.Add(v1);
        triangles.Add(v2);
        triangles.Add(v3);
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
    {
        int vertexIndex = vertices.Count;
        AddVertex(Perturb(v1), color);
        AddVertex(Perturb(v2), color);
        AddVertex(Perturb(v3), color);

        AddTriangleIndices(vertexIndex, vertexIndex + 1, vertexIndex + 2);
    }

    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.y += (sample.y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }
}
