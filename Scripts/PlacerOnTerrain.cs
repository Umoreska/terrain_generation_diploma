//using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class PlacerOnTerrain : MonoBehaviour
{  
    public static GameObject[] PlaceOnTerrain(float[,] height_map, int count, float min_threshold_height, float max_threshold_height, int seed, GameObject[] prefabs) {
        // List to store the placed objects
        List<GameObject> placedObjects = new List<GameObject>();

        int mapWidth = height_map.GetLength(0);
        int mapHeight = height_map.GetLength(1);

        // Initialize random generator with seed
        Random.InitState(seed);


        List<Vector2> placeble_places = new List<Vector2>();

        for(int i = 0; i < mapWidth; i++) {
            for(int j = 0; j < mapHeight; j++) {
                if(height_map[i,j] >= min_threshold_height && height_map[i,j] <= max_threshold_height){
                    placeble_places.Add(new Vector2(i, j));
                }
            }
        }
        
        int placeble_places_count = placeble_places.Count;
        for (int i = 0; i < count; i++) {

            Vector2 place = placeble_places[Random.Range(0, placeble_places_count)];
            // Randomly select a position
            int x = Random.Range(0, mapWidth);
            int z = Random.Range(0, mapHeight);

            // Get the height at the selected position
            float height = height_map[x, z];
            // Randomly select a prefab from the array
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length-1)];

            // Instantiate and place the object
            Vector3 position = new Vector3(x, height, z);
            GameObject instance = GameObject.Instantiate(prefab, position, Quaternion.identity);
            // Randomize it !!!


            // Add the instance to the list
            placedObjects.Add(instance);
        }

        return placedObjects.ToArray();    
    }
}