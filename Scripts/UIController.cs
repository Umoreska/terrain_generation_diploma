
using TMPro;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;

public class UIController : MonoBehaviour
{
    [SerializeField] private DLA dla;
    [SerializeField] private Erosion erosion;
    [SerializeField] private DrawMode draw_mode;
    [SerializeField] private MapDisplay map_display;
    [SerializeField] private MapGenerator map_generator;
    [SerializeField] private Terrain terrain;
    [SerializeField] protected float heightScale = 50f;
    [SerializeField] private GameObject size_input, draw_mode_input, scale_input, seed_input, offset_x_input, offset_y_input, roughness_input, octaves_input, persistance_input, 
                                    lacunarity_input, point_count_input, max_height_input, dla_initial_input, dla_steps_input, erosion_panel;
    [SerializeField] private TMP_Dropdown algorithm_dropdown, draw_mode_dropdown, size_dropdown;
    [SerializeField] private Slider scale_slider, seed_slider, offset_x_slider, offset_y_slider, roughness_slider, octaves_slider, persistance_slider, 
                                    lacunarity_slider, point_count_slider, max_height_slider, dla_initial_slider, dla_steps_slider, erosion_iterations_slider, delta_time_slider;
    [SerializeField] private CinemachineVirtualCamera camera_on_plane, camera_on_terrain;
    private bool generate_on_input_change=false, use_fallof=false;

    private HeightMapAlgorithm algorithm;

    private void Start() {
        //terrain_data = terrain.terrainData;
    }

    public void GenerateOnInputChange(bool generate_on_input_change) {
        this.generate_on_input_change = generate_on_input_change;
    }

    public void UseFalloff(bool use_fallof) {
        this.use_fallof = use_fallof;
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

 

    private Coroutine eroding_coroutine = null;
    public void StartEroding() {
        if(eroding_coroutine != null) {
            Debug.LogWarning($"StartEroding: eroding_coroutine != null");
            return;
        }
        int erosion_iteration = (int)erosion_iterations_slider.value;
        float delta_time = delta_time_slider.value;
        eroding_coroutine = StartCoroutine(ErodingCoroutine(erosion_iteration, delta_time));
    }

    public void StopEroding() {
        if(eroding_coroutine == null) {
            Debug.LogWarning($"StopEroding: eroding_coroutine == null");
            return;
        }
        StopCoroutine(eroding_coroutine);
        eroding_coroutine = null;
    }

    private IEnumerator ErodingCoroutine(int iteration_per_erode, float delta_time=0.1f) {
        while(true) {
            yield return new WaitForSeconds(delta_time);
            ErodeMap(iteration_per_erode);
        }
    }

       public void ErodeMap(int iterations=1) {
        if(map.GetLength(0) != map.GetLength(1)) {
            Debug.LogWarning($"map width({map.GetLength(0)}) != map height({map.GetLength(1)})");
            Debug.Break();
            return;
        }
        int size = map.GetLength(0);

        float[] map_array = new float[size*size];
        for(int i = 0; i<size; i++) {
            for(int j = 0; j<size; j++) {
                map_array[i*size + j] = map[i,j];
            }
        }
        
        bool reset_seed = false;
        erosion.Erode(map_array, size, iterations, reset_seed);

        for(int i = 0; i<size; i++) {
            for(int j = 0; j<size; j++) {
                map[i,j] = map_array[i*size + j];
            }
        }

        ShowResult();
    }


    private float[,] map = null;
    public void Generate() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        int _size = (int)Mathf.Pow(2, size_dropdown.value+5);
        Debug.Log(algorithm_dropdown.value);
        algorithm = (HeightMapAlgorithm)algorithm_dropdown.value;
        switch(algorithm) {
            case HeightMapAlgorithm.PerlinNoise:
                map = PerlinNoiseTerrain.GenerateHeights(_size, scale_slider.value);
            break;
            case HeightMapAlgorithm.FractalPerlinNoise:
                FractalPerlinNoise.Noise perlin_noise_type = FractalPerlinNoise.Noise.UnityPerlin; // should i give user access to choose?
                Vector2 offset = new Vector2(offset_x_slider.value, offset_y_slider.value);
                map = FractalPerlinNoise.GenerateHeights(_size, 
                                                    (int)seed_slider.value, scale_slider.value, 
                                                    (int)octaves_slider.value, persistance_slider.value, lacunarity_slider.value, offset, 
                                                    FractalPerlinNoise.NormalizeMode.Local, perlin_noise_type);
            break;
            case HeightMapAlgorithm.DiamondSquare:
                map = DiamondSquareTerrain.GenerateHeights(_size+1, roughness_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.Voronoi:
                map = VoronoiTerrain.GenerateHeights(_size, (int)point_count_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.DLA:
                int initialGridSize = (int)dla_initial_slider.value;
                int stepAmount = (int)dla_steps_slider.value;
                map = DLA.RunDLA(initialGridSize, stepAmount);
                //int size = initialGridSize * (int)Mathf.Pow(DLA.UPSCALE_FACTOR, stepAmount); // res_size = initial * scale_factor^steps// just in case lol
                break;
            default:
                Debug.LogWarning("Using undefined algorithm: " + algorithm);
                Debug.Break();
            break;
            
        }
        sw.Stop();
        Debug.Log($"time for noise generation: {sw.ElapsedMilliseconds} ms");


        if(use_fallof) {
            float[,] falloff_map = FalloffGenerator.GenerateFalloffMap(map.GetLength(0));
            for(int i = 0; i < map.GetLength(0); i++) {
                for(int j = 0; j < map.GetLength(1); j++) {
                    map[i,j] -= falloff_map[i,j];
                }
            }
        }

        sw.Restart();
        ShowResult();
        sw.Stop();
        Debug.Log($"time for texture/mesh generation: {sw.ElapsedMilliseconds} ms");
    }

    private void ShowResult() {
        if(draw_mode == DrawMode.Mesh) {
            map_display.DrawMesh(map, max_height_slider.value, map_generator.terrain_data.mesh_height_curve, false);
        }else {
            if((DrawMode)draw_mode_dropdown.value == DrawMode.NoiseMap) {
                map_display.DrawNoiseMap(map);
            }else {
                map_display.DrawColorMap(map);
            }
        }
    }

    public void SetAlgorithm(int index) {
        size_input.SetActive(true);
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
        dla_initial_input.SetActive(false);
        dla_steps_input.SetActive(false);

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
                size_input.SetActive(false);
                dla_initial_input.SetActive(true);
                dla_steps_input.SetActive(true);
            break;
            default:
                Debug.LogWarning("Set undefined algorithm: " + algorithm);
                Debug.Break();
            break;
        }
        
    }
}
