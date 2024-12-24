using System.Collections.Generic;
using UnityEngine;
public static class HexMetrics 
{

	public const float outerRadius = 1f;

	public const float innerRadius = outerRadius * 0.866025404f;

    public const float borderThickness = 0.45f; //0.15

    public const float fullDiameter = outerRadius * 2 + borderThickness * 2;

    public const float levelHeight = 0.3f;

    public static readonly float Cos30 = Mathf.Cos(Mathf.Deg2Rad * 30f);

    public static readonly Vector3Int MissingNeighbor = Vector3Int.one * int.MaxValue;

    public static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
    };

    public static readonly Vector3Int[] directions = {
        new Vector3Int(0, 1, -1),  // Северо-восток
        new Vector3Int(1, 0, -1),  // Восток
        new Vector3Int(1, -1, 0),  // Юго-восток
        new Vector3Int(0, -1, 1),  // Юго-запад
        new Vector3Int(-1, 0, 1),  // Запад
        new Vector3Int(-1, 1, 0),  // Северо-запад
    };

    public static Texture2D noiseSource;
    public const float cellPerturbStrength = 1f;
    public const float elevationPerturbStrength = 0.75f;
    public const float noiseScale = 0.003f;

    public static HexCell GetNeighbor(HexCell current, Vector3Int direction, List<HexCell> cells)
    {
        Vector3Int neighborPos = current.inGridPosition + direction;
        return cells.Find(c => c.inGridPosition == neighborPos);
    }

    public static Vector2 HexToWorldPosition(Vector3Int hexPosition)
    {
        float x = (hexPosition.x + 0.5f * hexPosition.y) * fullDiameter * Cos30;
        float z = hexPosition.y * fullDiameter * 0.75f;

        
        return new Vector2(x, z);
    }

    public static Vector3 HexToWorldPosition(Vector3Int hexPosition, float height)
    {
        float x = (hexPosition.x + 0.5f * hexPosition.y) * fullDiameter * Cos30;
        float z = hexPosition.y * fullDiameter * 0.75f;


        return new Vector3(x, height, z);
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale,
            position.z * noiseScale
        );
    }
}