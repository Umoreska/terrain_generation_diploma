using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HeightMapAlgorithm {
    PerlinNoise, FractalPerlinNoise, DiamondSquare, Voronoi, DLA
}

public class TerrainGenerationParent : MonoBehaviour
{
    [SerializeField] protected bool generate=false;
    [SerializeField] protected float generate_tick=1f;
    [SerializeField] protected float heightScale = 50f;
    [SerializeField] protected Terrain terrain;

    protected IEnumerator GenerateTerrainCoroutine() {
        while(true) {
            terrain.terrainData = GenerateTerrain(terrain.terrainData);
            yield return new WaitForSeconds(generate_tick);
        }
    }

    protected virtual TerrainData GenerateTerrain(TerrainData terrainData) {
        Debug.Log("TerrainGenerationParent GenerateTerrain call");
        return terrainData;
    }
}
