using System.Collections;
using UnityEngine;

public class VoronoiTerrain : TerrainGenerationParent
{
    public int size = 256;
    public int seed = 42;
    public static int static_seed;
    public int pointCount = 10;

    void Awake()     {
        if(!generate) return;

        StartCoroutine(GenerateTerrainCoroutine());
    }

    protected override TerrainData GenerateTerrain(TerrainData terrainData)     {
        static_seed = seed;
        terrainData.heightmapResolution = size + 1;
        terrainData.size = new Vector3(size, heightScale, size);
        terrainData.SetHeights(0, 0, GenerateHeights(size, pointCount, seed, false));
        return terrainData;
    }

    public static float[,] GenerateHeights(int _size, int _pointCount, int seed, bool random = true) {        

        float[,] heights = new float[_size, _size];
        Vector2[] points = GeneratePoints(random, _pointCount, _size, seed);
        for (int x = 0; x < _size; x++) {
            for (int y = 0; y < _size; y++) {
                heights[x, y] = CalculateHeight(x, y, points, _size);
            }
        }
        return heights;
    }

    private static Vector2[] GeneratePoints(bool random, int _pointCount, float _size, int seed) {

        if(random == false) {
            //Random.InitState(static_seed);
            Random.InitState(seed);
        }
        
        Vector2[] points = new Vector2[_pointCount];
        for (int i = 0; i < _pointCount; i++) {
            points[i] = new Vector2(Random.Range(0, _size), Random.Range(0, _size));
        }
        return points;
    }

    private static float CalculateHeight(int x, int y, Vector2[] points, int _size) {
        float minDistance = float.MaxValue;
        foreach (var point in points) {
            float distance = Vector2.Distance(new Vector2(x, y), point);
            if (distance < minDistance) {
                minDistance = distance;
            }
        }
        return minDistance / _size;
    }
}
