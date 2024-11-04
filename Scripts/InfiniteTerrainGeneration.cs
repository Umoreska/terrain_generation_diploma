using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public enum ChunkUpdateMode{
    Square, ViewPreload, OnlyView
}
public class InfiniteTerrainGeneration : MonoBehaviour
{    
    private const float viewer_move_distance_threshold_for_update = 25f;
    private const float squared_viewer_move_distance_threshold_for_update = viewer_move_distance_threshold_for_update*viewer_move_distance_threshold_for_update;
    private const float viewer_rotate_angle_threshold_for_update = 5f;
    [SerializeField] private ChunkUpdateMode chunkUpdateMode = 0;
    [SerializeField] private Camera main_camera;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LODInfo[] detailLevels;
    [SerializeField] private static float max_view_dist; // how far viewer can see
    [SerializeField] private Transform viewer;
    private static MapGenerator mapGenerator;
    public static Vector2 viewer_position, old_viewer_postition;
    public static Vector3 old_viewer_direction = Vector3.forward;
    [SerializeField] int chunk_size; 
    int chunkVisibleAmount; // in view distance

    Dictionary<Vector2, TerrainChunk> terrain_chunk_dictionary = new Dictionary<Vector2, TerrainChunk>();
    static private  List<TerrainChunk> visible_chunk_last_update = new List<TerrainChunk>();
    [SerializeField] private Material mapMaterial;

    private void Start() {
        mapGenerator = FindFirstObjectByType<MapGenerator>();

        max_view_dist = detailLevels[detailLevels.Length-1].visible_distance_threshold;

        chunk_size = mapGenerator.mapChunkSize - 1;
        chunkVisibleAmount = Mathf.RoundToInt(max_view_dist / chunk_size);

        UpdateVisibleChunks();
        //ground_layer = LayerMask.NameToLayer("Ground") ;
    }

    private void Update() {
        // when to update chunks
        viewer_position = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrain_data.uniform_scale;
        if( (old_viewer_postition - viewer_position).sqrMagnitude > squared_viewer_move_distance_threshold_for_update) {

            UpdateVisibleChunks();
            old_viewer_postition = viewer_position;
            
        }else if(chunkUpdateMode != ChunkUpdateMode.Square){

            Vector3 currentForwardDirection = viewer.transform.forward;
            float angleDifference = Vector3.Angle(old_viewer_direction, currentForwardDirection);

            if (angleDifference >= viewer_rotate_angle_threshold_for_update) {
                UpdateVisibleChunks();
                old_viewer_direction = currentForwardDirection;
            }
        }
        
    }

    public void UpdateVisibleChunks() {

        for(int i = 0; i < visible_chunk_last_update.Count; i++) {
            visible_chunk_last_update[i].SetVisible(false);
        }
        visible_chunk_last_update.Clear();

        // chunk on the left from viewer will be [-1:0], on the right bottom - [1:-1]
        // in game world it will be [-240:0] and [240:-240]
        int current_chunk_coord_x = Mathf.RoundToInt(viewer_position.x / chunk_size);
        int current_chunk_coord_y = Mathf.RoundToInt(viewer_position.y / chunk_size);
        // wiever stands on this chunk
        
        // loop through arounding viewer chunks
        for(int offset_y = -chunkVisibleAmount; offset_y <= chunkVisibleAmount; offset_y++) {
            for(int offset_x = -chunkVisibleAmount; offset_x <= chunkVisibleAmount; offset_x++) {
                Vector2 chunk_coord = new Vector2(current_chunk_coord_x + offset_x, current_chunk_coord_y + offset_y);
                Vector3 chunk_position_on_scene = new Vector3(chunk_coord.x*chunk_size, 0, chunk_coord.y*chunk_size) * mapGenerator.terrain_data.uniform_scale;

                if(offset_x == 0 && offset_y == 0) { // chunk viewer standing on
                    if(terrain_chunk_dictionary.ContainsKey(chunk_coord)) {
                        terrain_chunk_dictionary[chunk_coord].UpdateChunk();
                    }else {
                        terrain_chunk_dictionary.Add(chunk_coord, new TerrainChunk(chunk_coord, chunk_size, detailLevels, this.transform, mapMaterial));
                    }
                }else if(MathF.Abs(offset_x) == 1 || MathF.Abs(offset_y) == 1) {
                    if(IsObjectInView(main_camera.transform.position, main_camera.transform.forward, chunk_position_on_scene, viewAngle) || true) {
                        if(terrain_chunk_dictionary.ContainsKey(chunk_coord)) {
                            terrain_chunk_dictionary[chunk_coord].UpdateChunk();
                        }else {
                            terrain_chunk_dictionary.Add(chunk_coord, new TerrainChunk(chunk_coord, chunk_size, detailLevels, this.transform, mapMaterial));
                        }
                    }
                }
                

                switch (chunkUpdateMode) {
                    case ChunkUpdateMode.Square: // load and show chunk around
                        if(terrain_chunk_dictionary.ContainsKey(chunk_coord)) {
                            terrain_chunk_dictionary[chunk_coord].UpdateChunk();
                        }else {
                            terrain_chunk_dictionary.Add(chunk_coord, new TerrainChunk(chunk_coord, chunk_size, detailLevels, this.transform, mapMaterial));
                        }
                    break;
                    case ChunkUpdateMode.OnlyView: // load and show only chunks in view
                    
                        if(IsObjectInView(main_camera.transform.position, main_camera.transform.forward, chunk_position_on_scene, viewAngle)) {
                            if(terrain_chunk_dictionary.ContainsKey(chunk_coord)) {
                                terrain_chunk_dictionary[chunk_coord].UpdateChunk();
                            }else {
                                terrain_chunk_dictionary.Add(chunk_coord, new TerrainChunk(chunk_coord, chunk_size, detailLevels, this.transform, mapMaterial));
                            }
                        }
                    break;
                    case ChunkUpdateMode.ViewPreload: // load chunk around and show only in view
                        if(terrain_chunk_dictionary.ContainsKey(chunk_coord)) {
                            //terrain_chunk_dictionary[chunk_coord].UpdateChunk(false);
                        }else {
                            terrain_chunk_dictionary.Add(chunk_coord, new TerrainChunk(chunk_coord, chunk_size, detailLevels, this.transform, mapMaterial));
                        }
                        var chunk = terrain_chunk_dictionary[chunk_coord];
                    
                        if(IsObjectInView(main_camera.transform.position, main_camera.transform.forward, chunk_position_on_scene, viewAngle)) {
                            //chunk.SetVisible(true);
                            chunk.UpdateChunk();
                        }
                    break;
                }

               



                /*if(terrain_chunk_dictionary.ContainsKey(chunk_coord)) {
                    terrain_chunk_dictionary[chunk_coord].UpdateChunk();
                }else {
                    terrain_chunk_dictionary.Add(chunk_coord, new TerrainChunk(chunk_coord, chunk_size, detailLevels, this.transform, mapMaterial));
                }*/
            }
        }
    }

    private static float CalculateAngle(Vector3 playerPosition, Vector3 forwardDirection, Vector3 objectPosition) {
        // Ігноруємо координату Y, працюємо лише з XZ-площиною
        forwardDirection.y = 0;
        playerPosition.y = 0;
        objectPosition.y = 0;

        // Вектор від гравця до об'єкта
        Vector3 directionToObject = (objectPosition - playerPosition).normalized;
        directionToObject.y = 0;

        // Обчислюємо кут між напрямком гравця і напрямком на об'єкт
        float angleToTarget = MathF.Abs(Vector3.Angle(forwardDirection, directionToObject));
        return angleToTarget;
    }


     private static bool IsObjectInView(Vector3 playerPosition, Vector3 forwardDirection, Vector3 objectPosition, float viewAngle) {
        return CalculateAngle(playerPosition, forwardDirection, objectPosition) <= (viewAngle / 2);
    }

    
    public bool IsChunkOnRightOrLeft(Vector3 player_pos, Vector3 looking_dir, Vector3 chunk_pos, float angle_threshold) {
        Vector3 direction_to_chunk = (chunk_pos - player_pos).normalized;

        float angle_to_chunk = Vector3.Angle(looking_dir, direction_to_chunk);

        bool is_left_side = Mathf.Abs(angle_to_chunk - 90f) <= angle_threshold;
        bool is_right_side = Mathf.Abs(angle_to_chunk + 90f) <= angle_threshold;

        return is_left_side || is_right_side;
    }

    public class TerrainChunk{
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detail_level;
        LODMesh[] lod_meshes; // saves mesh for every LevelOfDetail
        LODMesh collision_lod;

        private MapData map_data;
        private bool map_data_received;
        private int prev_lod_index = -1; // it has to be updated first time

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detail_level, Transform parent, Material material) {
            this.detail_level = detail_level;

            position = coord * size;
            Vector3 position_on_scene = new Vector3(position.x,0,position.y);
            bounds = new Bounds(position,Vector2.one*size);
            
            meshObject = new GameObject("Terrain Chunk");
            meshObject.layer = LayerMask.NameToLayer("Ground");//ground_layer;

            meshObject.transform.position = position_on_scene * mapGenerator.terrain_data.uniform_scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrain_data.uniform_scale;

            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            //meshObject.transform.localScale = Vector3.one * size /10f; // /10f because plane is already 10 units in wide and length with scale 1
            
            SetVisible(false);
            lod_meshes = new LODMesh[detail_level.Length];

            for(int i = 0; i <lod_meshes.Length; i++) {
                lod_meshes[i] = new LODMesh(detail_level[i].lod, ()=> {UpdateChunk();} ); // preloadView chunkUpdateMode problem here <-------------------------!
                if(detail_level[i].use_for_collider) {
                    collision_lod = lod_meshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived); // getting height map in async thread
        }

        private void OnMapDataReceived(MapData map_data) {      
            this.map_data = map_data;
            map_data_received = true;

            Texture2D texture2D = TextureGenerator.TextureFromColorMap(map_data.colorMap, mapGenerator.mapChunkSize, mapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture2D;
            
            UpdateChunk(); // preloadView chunkUpdateMode problem here <-------------------------!
        }

        public void UpdateChunk(bool show_after_loaded=true) {
            if(map_data_received == false) {
                return;
            }


            float distance_from_viewer_to_nearest_edge = Mathf.Sqrt( bounds.SqrDistance(viewer_position) );
            bool visible = distance_from_viewer_to_nearest_edge <= max_view_dist;

            if(visible) {
                int lod_index = 0;

                // getting correct level of detail
                for(int i = 0; i < detail_level.Length-1; i++) {
                    if(distance_from_viewer_to_nearest_edge > detail_level[i].visible_distance_threshold) {
                        lod_index = i + 1;
                    }else {
                        break;
                    }
                }

                // creating mesh with gotten level of detail
                if(lod_index != prev_lod_index) {
                    LODMesh lod_mesh = lod_meshes[lod_index];
                    if(lod_mesh.has_mesh) {
                        meshFilter.mesh = lod_mesh.mesh;
                        prev_lod_index = lod_index;
                        //meshCollider.sharedMesh = lod_mesh.mesh;

                    }else if(lod_mesh.has_requested_mesh == false) {
                        lod_mesh.RequestMesh(map_data);
                    }
                }
                if(lod_index == 0) { // if terrain is close enough
                    if(collision_lod.has_mesh) {
                        meshCollider.sharedMesh = collision_lod.mesh;
                    }else if(collision_lod.has_requested_mesh == false) {
                        collision_lod.RequestMesh(map_data);
                    }
                }
                visible_chunk_last_update.Add(this);
            }
            if(show_after_loaded) {
                SetVisible(visible);
            }
        }


        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() => meshObject.activeSelf;
    }

    class LODMesh { // responsible for fetching its own mesh from mesh generator

        public Mesh mesh;
        public bool has_requested_mesh;
        public bool has_mesh;

        private int lod;

        private System.Action update_callback;

        public LODMesh(int lod, System.Action update_callback) {
            this.lod = lod;
            this.update_callback = update_callback;
        }

        public void RequestMesh(MapData map_data) {
            has_requested_mesh = true;
            mapGenerator.RequestMeshData(map_data, lod, OnMeshDataRecieved);
        }  

        private void OnMeshDataRecieved(MeshData data) {
            mesh = data.CreateMesh();
            has_mesh = true;
            update_callback();
        }
    }

[Serializable] public struct LODInfo{
        public int lod;
        public float visible_distance_threshold; // when wiever is outside of distance it will switch over next level of detail 

        public bool use_for_collider;

    }

}
