using System;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, ColourMap, Mesh, FallofMap};
	public DrawMode drawMode;

	[SerializeField] MapDisplay display;

	public NoiseData noise_data;
	public MyTerrainData terrain_data;

	[Range(0,6)]
	public int editorPreviewLevelOfDetail;
	
	public bool autoUpdate;

	public TerrainType[] regions;
	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	private float[,] falloff_map;


	public static MapGenerator Instance;

	private void Awake() {
		//falloff_map = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}

	private void OnValuesUpdated() {
		if(Application.isPlaying == false) {
			DrawMapInEditor();
		}
	}


	public static int mapChunkSize {
		get{
			if(Instance == null) {
				Instance = FindFirstObjectByType<MapGenerator>();
			}
			if(Instance.terrain_data.useFlatShading) {
				return 95;
			}else {
				return 239;
			}
		}
	}

	private MapData GenerateMapData(Vector2 center) {
		
        float[,] noiseMap = FractalPerlinNoise.GenerateHeights(mapChunkSize+2, // 239+2 = 241
			(int)noise_data.seed, noise_data.noiseScale, (int)noise_data.octaves, noise_data.persistance, noise_data.lacunarity,
			center+noise_data.offset, noise_data.normalizeMode, FractalPerlinNoise.Noise.UnityPerlin);

		if(terrain_data.useFalloff) {
			falloff_map = FalloffGenerator.GenerateFalloffMap(mapChunkSize+2);
			for (int y = 0; y < mapChunkSize+2; y++) {
				for (int x = 0; x < mapChunkSize+2; x++) {
					if(terrain_data.useFalloff) {
						noiseMap[x,y] = Mathf.Clamp(noiseMap[x,y]-falloff_map[x,y], 0, 1);
					}
					
				}
		}
		}

		// creating color map from regions
		Color[] colorMap = new Color[(mapChunkSize+2) * (mapChunkSize+2)];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < regions.Length; i++) {

					if (currentHeight >= regions[i].max_height) {
						colorMap [y * mapChunkSize + x] = regions[i].color;
					}else {
						break;
					}
				}
			}
		}

		return new MapData(noiseMap, colorMap);

	}

	public void RequestMapData(Vector2 center, Action<MapData> callback) {
		ThreadStart threadStart = ()=> {
			MapDataThread(center, callback);
		};
		new Thread(threadStart).Start();
	}	
	private void MapDataThread(Vector2 center, Action<MapData> callback) {
		MapData data = GenerateMapData(center);
		lock(mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, data));
		}
	}


	public void RequestMeshData(MapData mapData,  int lod, Action<MeshData> callback) {
		ThreadStart threadStart = ()=> {
			MeshDataThread(mapData, lod, callback);
		};
		new Thread(threadStart).Start();
	}
	private void MeshDataThread(MapData mapData, int lod,  Action<MeshData> callback) {
		MeshData data = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrain_data.heightMultiplier, terrain_data.mesh_height_curve, lod, terrain_data.useFlatShading);
		lock(meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, data));
		}
	}




	private void Update() {
		while(mapDataThreadInfoQueue.Count > 0) {
			MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
			threadInfo.callback.Invoke(threadInfo.parameter);
		}
		while(meshDataThreadInfoQueue.Count > 0) {
			MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
			threadInfo.callback.Invoke(threadInfo.parameter);
		}
	}


	public void DrawMapInEditor() {
		
		MapData data = GenerateMapData(Vector2.zero);

		if (drawMode == DrawMode.NoiseMap) {
			//display.DrawTexture (TextureGenerator.TextureFromHeightMap (noiseMap));
            display.DrawNoiseMap(data.heightMap);
		} else if (drawMode == DrawMode.ColourMap) {
			//display.DrawTexture (TextureGenerator.TextureFromColourMap (colourMap, mapWidth, mapHeight));
            display.DrawColorMap(data.heightMap, data.colorMap);
		} else if (drawMode == DrawMode.Mesh) {
			//display.DrawMesh (MeshGenerator.GenerateTerrainMesh (noiseMap), TextureGenerator.TextureFromColourMap (colourMap, mapWidth, mapHeight));
            display.DrawMesh(data.heightMap, data.colorMap, terrain_data.heightMultiplier, terrain_data.mesh_height_curve, editorPreviewLevelOfDetail, terrain_data.useFlatShading, true);
		} else if(drawMode == DrawMode.FallofMap) {
			display.DrawFalloffMap(mapChunkSize);
		}
	}

	void OnValidate() {
		if(terrain_data != null) {
			terrain_data.on_value_updated -= OnValuesUpdated; // so it will not duplicate. when we do -=, it does not require it to actually exist
			terrain_data.on_value_updated += OnValuesUpdated;
		}
		if(noise_data != null) {
			noise_data.on_value_updated -= OnValuesUpdated;
			noise_data.on_value_updated += OnValuesUpdated;
		}
		
	}

	struct MapThreadInfo<T>{
		public readonly Action<T> callback;
		public readonly T parameter;
		public MapThreadInfo(Action<T> callback, T parameter) {
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}


[Serializable] public struct TerrainType{
    public string name;
    public float max_height;
    public Color color;
}


public struct MapData{
	public readonly float[,] heightMap;
	public readonly Color[] colorMap;
	public MapData(float[,] height_map, Color[] color_map) {
		heightMap = height_map;
		colorMap = color_map;
	}
}
