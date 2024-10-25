using System.Collections;
using UnityEngine;

public class PerlinNoiseTerrain : TerrainGenerationParent
{   
    public static int size = 256;
    public float scale = 20f;

    void Awake() {
        if(!generate) return;

        StartCoroutine(GenerateTerrainCoroutine());
    }

    protected override TerrainData GenerateTerrain(TerrainData terrainData) {
        terrainData.heightmapResolution = size + 1;
        terrainData.size = new Vector3(size, heightScale, size);
        terrainData.SetHeights(0, 0, GenerateHeights(size, scale));

        return terrainData;
    }
    
    public static  float[,] GenerateHeights(int _size, float _scale) {

        float[,] heights = new float[_size, _size];
        
        for (int x = 0; x < _size; x++) {
            for (int y = 0; y < _size; y++) {
                heights[x, y] = CalculateHeight(x, y, _scale, _size);
            }
        }
        return heights;
    }
    public static float CalculateHeight(int x, int y, float _scale, int _size) {
        
        float xCoord = (float)x / _size * _scale;
        float yCoord = (float)y / _size * _scale;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }


    
}
