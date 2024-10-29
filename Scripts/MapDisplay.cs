using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum DrawMode {
    NoiseMap, ColorMap, Mesh
}
public class MapDisplay : MonoBehaviour
{
    [SerializeField] private Renderer texture_renderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    
    [SerializeField] private  TerrainType[] regions;

    public void DrawNoiseMap(float[,] noise_map) {
        int width = noise_map.GetLength(0);
        int height = noise_map.GetLength(1);
        Debug.Log($"width: {width}, height: {height}");
        Color[] color_map = new Color[width * height];
        for(int i = 0; i < height; i++) {
            for(int j = 0; j < width; j++) {
                color_map[i*width + j] = Color.Lerp(Color.black, Color.white, noise_map[i,j]);
            }
        }
        DrawTexture(color_map, width, height);
    }

    public void DrawNoiseMap(float[] noise_map, int size) {
        Debug.Log($"size: {size}");
        Color[] color_map = new Color[size * size];
        for(int i = 0; i < size; i++) {
            for(int j = 0; j < size; j++) {
                color_map[i*size + j] = Color.Lerp(Color.black, Color.white, noise_map[i*size + j]);
            }
        }
        DrawTexture(color_map, size, size);
    }

    public void DrawColorMap(float[,] noise_map) {
        int width = noise_map.GetLength(0);
        int height = noise_map.GetLength(1);
        Debug.Log($"draw color map without colorMap. width: {width}, height: {height}");
        DrawTexture(CreateColorMap(noise_map), width, height);
    }
    public void DrawColorMap(float[,] noise_map, Color[] colourMap) {
        int width = noise_map.GetLength(0);
        int height = noise_map.GetLength(1);
        Debug.Log($"draw color map with colorMap. width: {width}, height: {height}");
        //DrawTexture(CreateColorMap(noise_map), width, height);
        DrawTexture(colourMap, width-2, height-2);
    }

    public void DrawFalloffMap(int size) {
        Texture2D texture = TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(size));
        texture_renderer.sharedMaterial.mainTexture = texture;
        texture_renderer.transform.localScale = new Vector3(size, 1, size);
    }

    private Color[] CreateColorMap(float[,] noise_map) {
        int width = noise_map.GetLength(0);
        int height = noise_map.GetLength(1);

        Color[] color_map = new Color[width * height];

        for(int i = 0; i < height; i++) {
            for(int j = 0; j < width; j++) {
                for(int r = 0; r < regions.Length; r++) {
                    if(noise_map[j,i] <= regions[r].max_height) {
                        color_map[i*width + j] = regions[r].color;
                        break;
                    }
                }
            }
        }
        return color_map;
    }

    public static Texture2D CreateTexture(Color[] color_map, int width, int height) {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(color_map);
        texture.Apply();
        return texture;
    }

    private void DrawTexture(Color[] color_map, int width, int height) {
        texture_renderer.sharedMaterial.mainTexture = CreateTexture(color_map, width, height);
        texture_renderer.transform.localScale = new Vector3(width, 1, height);
    }

    public void DrawMesh(float[,] noise_map, float height_multiplier, AnimationCurve mesh_height_curve, bool useFlatShading) {
        Color[] color_map = CreateColorMap(noise_map);
        DrawMesh(noise_map, color_map, height_multiplier, mesh_height_curve, 0, useFlatShading);
    }

    public void DrawMesh(float[,] noise_map, Color[] colourMap, float height_multiplier, AnimationCurve mesh_height_curve, int level_of_detail, bool useFlatShading, bool is_in_editor=false) {        

        //Color[] color_map = CreateColorMap(noise_map);
        MeshData data = MeshGenerator.GenerateTerrainMesh(noise_map, height_multiplier*meshRenderer.transform.localScale.x, mesh_height_curve, level_of_detail, useFlatShading);

        meshFilter.sharedMesh = data.CreateMesh();        

        int width = noise_map.GetLength(0);
        int height = noise_map.GetLength(1);
        if(is_in_editor) {
            meshRenderer.sharedMaterial.mainTexture = CreateTexture(colourMap, width-2, height-2); // problem !!!

        }else {
            meshRenderer.sharedMaterial.mainTexture = CreateTexture(colourMap, width, height); // problem !!!
        }
    }

}
