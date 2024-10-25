using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Diagnostics.Tracing;

public class Tester : MonoBehaviour
{
    [SerializeField] private int size = 241;
    [SerializeField] private float noiseScale = 50f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float heightMultiplier;
    [SerializeField] private AnimationCurve mesh_height_curve;
    [SerializeField] private Material mesh_material, terrain_material;
    void Start()
    {
        TestBuiltInNoiseAndImprovedNoiseSpeed();
    }

    public void TestMeshTerrainSpeedGeneration() {

        if (Application.isPlaying) {
            var mesh_object = GameObject.Find("Mesh Chunk");
            var terrain_object = GameObject.Find("Terrain Chunk");
            if(mesh_object != null) {
                Destroy(mesh_object);
            }
            if(terrain_object != null) {
                Destroy(terrain_object);
            }
        }
        

        float[,] height_map = FractalPerlinNoise.GenerateHeights(size, 0, noiseScale, octaves, 0.5f , 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Global, FractalPerlinNoise.Noise.UnityPerlin);

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();


        int width = height_map.GetLength(0);
        int height = height_map.GetLength(1);
        Color[] mesh_color_map = new Color[width * height];
        Color[] terrain_color_map = new Color[width * height];
        for(int i = 0; i < height; i++) {
            for(int j = 0; j < width; j++) {
                mesh_color_map[i*width + j] = Color.Lerp(Color.black, Color.white, height_map[j,i]);
                terrain_color_map[i*width + j] = Color.Lerp(Color.black, Color.white, height_map[i,j]);
            }
        }

        // my mesh
        sw.Start();
        GameObject meshObject = new("Mesh Chunk");
        
        var meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mesh_material;
        meshRenderer.sharedMaterial.mainTexture = MapDisplay.CreateTexture(mesh_color_map, size, size);

        var meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshData mesh_data = MeshGenerator.GenerateTerrainMesh(height_map, heightMultiplier, mesh_height_curve, 0, false);

        meshFilter.sharedMesh = mesh_data.CreateMesh();
        sw.Stop();
        Debug.Log($"creation time of own mesh terrain: {sw.ElapsedMilliseconds} ms");


        sw.Restart();
        GameObject terrainObject = new("Terrain Chunk");
        terrainObject.transform.position = new Vector3(size/2, 0, -size/2);

        Terrain terrain = terrainObject.AddComponent<Terrain>();        
        TerrainCollider terrainCollider = terrainObject.AddComponent<TerrainCollider>();
        TerrainData terrainData = terrain.terrainData = new TerrainData();

        terrain.materialTemplate = terrain_material;
        terrain.materialTemplate.mainTexture = MapDisplay.CreateTexture(terrain_color_map, size, size);

         // Create a new TerrainLayer
        TerrainLayer terrainLayer = new TerrainLayer();
        terrainLayer.diffuseTexture = MapDisplay.CreateTexture(terrain_color_map, size, size);
        terrainLayer.tileSize = new Vector2(size, size); // Adjust tile size
        terrainData.terrainLayers = new TerrainLayer[] { terrainLayer };

        terrainData.heightmapResolution = size + 1;
        terrainData.size = new Vector3(size, heightMultiplier, size);
        terrainData.SetHeights(0, 0, height_map);

        terrainCollider.terrainData = terrainData;

        terrain.terrainData = terrainData;
        sw.Stop();
        Debug.Log($"creation time of unity terrain: {sw.ElapsedMilliseconds} ms");
    }

    public void TestBuiltInNoiseAndImprovedNoiseSpeed() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        
        sw.Start();
        float[,] height_map = FractalPerlinNoise.GenerateHeights(size, 0, noiseScale, octaves, 0.5f , 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Global, FractalPerlinNoise.Noise.UnityPerlin);
        sw.Stop();
        Debug.Log($"creation time of unity noise: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        height_map = FractalPerlinNoise.GenerateHeights(size, 0, noiseScale, octaves, 0.5f , 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Global, FractalPerlinNoise.Noise.FastNoiseLiteSimplex);
        sw.Stop();
        Debug.Log($"creation time of simplex noise: {sw.ElapsedMilliseconds} ms");

    }
}
