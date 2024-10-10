using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private DrawMode draw_mode;
    [SerializeField] private MapDisplay map_display;
    [SerializeField] private MapGenerator map_generator;
    [SerializeField] private Terrain terrain;
    private TerrainData terrain_data;
    [SerializeField] protected float heightScale = 50f;
    [SerializeField] private GameObject draw_mode_input, scale_input, seed_input, offset_x_input, offset_y_input, roughness_input, octaves_input, persistance_input, 
                                    lacunarity_input, point_count_input, max_height_input;
    [SerializeField] private TMP_Dropdown algorithm_dropdown, draw_mode_dropdown, size_dropdown;
    [SerializeField] private Slider scale_slider, seed_slider, offset_x_slider, offset_y_slider, roughness_slider, octaves_slider, persistance_slider, 
                                    lacunarity_slider, point_count_slider, max_height_slider;
    [SerializeField] private CinemachineVirtualCamera camera_on_plane, camera_on_terrain;
    private bool generate_on_input_change=false;

    private HeightMapAlgorithm algorithm;

    private void Start() {
        terrain_data = terrain.terrainData;
    }

    public void GenerateOnInputChange(bool yes_no) {
        generate_on_input_change = yes_no;
    }

    public void InputChanged() {
        if(generate_on_input_change) {
            Generate();
        }
    }

    public void LookAtPlane() {
        camera_on_terrain.gameObject.SetActive(false);
        camera_on_plane.gameObject.SetActive(true);
        draw_mode_input.SetActive(true);
        max_height_input.SetActive(false);
        draw_mode_dropdown.value = (int)DrawMode.ColorMap;
        draw_mode = DrawMode.ColorMap;
    }
    public void LookAtTerrain() {
        camera_on_plane.gameObject.SetActive(false);
        camera_on_terrain.gameObject.SetActive(true);
        draw_mode_input.SetActive(false);
        max_height_input.SetActive(true);
        draw_mode = DrawMode.Mesh;
    }
    private float[,] map = null;
    public void Generate() {
        int _size = (int)Mathf.Pow(2, size_dropdown.value+5);
        Debug.Log(algorithm_dropdown.value);
        algorithm = (HeightMapAlgorithm)algorithm_dropdown.value;
        switch(algorithm) {
            case HeightMapAlgorithm.PerlinNoise:
                map = PerlinNoiseTerrain.GenerateHeights(_size, scale_slider.value);
            break;
            case HeightMapAlgorithm.FractalPerlinNoise:
                Vector2 offset = new Vector2(offset_x_slider.value, offset_y_slider.value);
                map = FractalPerlinNoise.GenerateHeights(_size, 
                                                    (int)seed_slider.value, scale_slider.value, 
                                                    (int)octaves_slider.value, persistance_slider.value, lacunarity_slider.value, offset, FractalPerlinNoise.NormalizeMode.Local);
            break;
            case HeightMapAlgorithm.DiamondSquare:
                map = DiamondSquareTerrain.GenerateHeights(_size+1, roughness_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.Voronoi:
                map = VoronoiTerrain.GenerateHeights(_size, (int)point_count_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.DLA:
                DLA.RunDLA();
            return;
            default:
                Debug.LogWarning("Using undefined algorithm: " + algorithm);
                Debug.Break();
            break;
            
        }
        if(draw_mode == DrawMode.Mesh) {
            /*terrain_data.heightmapResolution = _size + 1;
            terrain_data.size = new Vector3(_size, heightScale, _size);
            terrain_data.SetHeights(0, 0, map);*/
            map_display.DrawMesh(map, max_height_slider.value, map_generator.mesh_height_curve);
        }else {
            if((DrawMode)draw_mode_dropdown.value == DrawMode.NoiseMap) {
                map_display.DrawNoiseMap(map);
            }else {
                map_display.DrawColorMap(map);
            }
        }
    }

    public void SetAlgorithm(int index) {
        algorithm = (HeightMapAlgorithm)index;
        seed_input.SetActive(false);
        scale_input.SetActive(false);
        roughness_input.SetActive(false);
        offset_x_input.SetActive(false);
        offset_y_input.SetActive(false);
        octaves_input.SetActive(false);
        persistance_input.SetActive(false);
        lacunarity_input.SetActive(false);
        point_count_input.SetActive(false);

        switch(algorithm) {
            case HeightMapAlgorithm.PerlinNoise:
                scale_input.SetActive(true);
            break;
            case HeightMapAlgorithm.FractalPerlinNoise:
                scale_input.SetActive(true);
                seed_input.SetActive(true);
                octaves_input.SetActive(true);
                persistance_input.SetActive(true);
                lacunarity_input.SetActive(true);
                offset_x_input.SetActive(true);
                offset_y_input.SetActive(true);
            break;
            case HeightMapAlgorithm.DiamondSquare:
                seed_input.SetActive(true);
                roughness_input.SetActive(true);
            break;
            case HeightMapAlgorithm.Voronoi:
                seed_input.SetActive(true);
                point_count_input.SetActive(true);
            break;
            case HeightMapAlgorithm.DLA:
                // ?
            break;
            default:
                Debug.LogWarning("Set undefined algorithm: " + algorithm);
                Debug.Break();
            break;
        }
        
    }
}
