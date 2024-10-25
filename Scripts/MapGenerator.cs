using System;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, ColourMap, Mesh, FallofMap};
	public DrawMode drawMode;
	public FractalPerlinNoise.NormalizeMode normalizeMode;
	[SerializeField] MapDisplay display;

	public bool useFlatShading;

	[Range(0,6)]
	public int editorPreviewLevelOfDetail;
	public float noiseScale;
    public float heightMultiplier;

	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public bool useFalloff=false;

	public bool autoUpdate;
	public AnimationCurve mesh_height_curve;

	public TerrainType[] regions;
	private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	private float[,] falloff_map;


	public static MapGenerator Instance;

	private void Awake() {
		falloff_map = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}


	public static int mapChunkSize {
		get{
			if(Instance == null) {
				Instance = FindFirstObjectByType<MapGenerator>();
			}
			if(Instance.useFlatShading) {
				return 95;
			}else {
				return 239;
			}
		}
	}

	private MapData GenerateMapData(Vector2 center) {
		
        float[,] noiseMap = FractalPerlinNoise.GenerateHeights(mapChunkSize+2, // 239+2 = 241
																(int)seed, noiseScale, (int)octaves, persistance, lacunarity, center+offset, normalizeMode, FractalPerlinNoise.Noise.UnityPerlin);


		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				if(useFalloff) {
					noiseMap[x,y] = Mathf.Clamp (noiseMap[x,y]-falloff_map[x,y], 0, 1);
				}
				float currentHeight = noiseMap[x, y];
				for (int i = 0; i < regions.Length; i++) {

					if (currentHeight >= regions[i].max_height) {
						colourMap [y * mapChunkSize + x] = regions[i].color;
					}else {
						break;
					}
				}
			}
		}

		return new MapData(noiseMap, colourMap);

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
		MeshData data = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, mesh_height_curve, lod, useFlatShading);
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
            display.DrawMesh(data.heightMap, data.colorMap, heightMultiplier, mesh_height_curve, editorPreviewLevelOfDetail, useFlatShading);
		} else if(drawMode == DrawMode.FallofMap) {
			display.DrawFalloffMap(mapChunkSize);
		}
	}

	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
		falloff_map = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
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

public struct MapData{
	public readonly float[,] heightMap;
	public readonly Color[] colorMap;
	public MapData(float[,] height_map, Color[] color_map) {
		heightMap = height_map;
		colorMap = color_map;
	}
}
