using Unity.Burst.Intrinsics;
using UnityEditor.Rendering;
using UnityEngine;

public class FractalPerlinNoise {
    public int size = 256;
    public float scale = 20f;
    public int seed = 42;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    public enum NormalizeMode{
        Local, Global
    }


    public static float[,] GenerateHeights(int _size, int seed, float _scale, int _octaves, float _persistence, float _lacunarity, Vector2 offset, NormalizeMode normalize_mode) {
        float[,] noise_heights = new float[_size, _size];
        float max_possible_height = 0;

        float frequency = 1;
        float amplitude = 1;

        System.Random prng = new System.Random(seed);
        Vector2[] octave_offsets = new Vector2[_octaves];
        for(int i = 0; i < _octaves; i++) {
            float offset_x = prng.Next(-100000, 100000) + offset.x;
            float offset_y = prng.Next(-100000, 100000) - offset.y;
            octave_offsets[i] = new Vector2(offset_x, offset_y);

            max_possible_height += amplitude; // max noise height is 1, so 1*amplitude is still just amplitude
            amplitude *= _persistence;
        }


        float local_max_height = float.MinValue;
        float local_min_height = float.MaxValue;

        float halfSize = _size / 2f;

        if(_scale <= 0) {
            _scale = 0.00001f;
        }

        for (int x = 0; x < _size; x++) {
            for (int y = 0; y < _size; y++) {


                //float noise_height = CalculateFractalHeight(_size, x, y, _scale, _octaves, _persistence, _lacunarity);

                float noise_height = 0;
                frequency = 1;
                amplitude = 1;
                
                for (int i = 0; i < _octaves; i++)
                {
                    float xCoord = (x-halfSize + octave_offsets[i].x) / _scale * frequency ;
                    float yCoord = (y-halfSize+ octave_offsets[i].y) / _scale * frequency ;

                    noise_height += (Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1) * amplitude; // L

                    amplitude *= _persistence;
                    frequency *= _lacunarity;
                }

                if(noise_height > local_max_height) {
                    local_max_height = noise_height;
                }
                if(noise_height < local_min_height) {
                    local_min_height = noise_height;
                }
                noise_heights[x,y] = noise_height;
            }
        }       

        // normalising heights
        for (int x = 0; x < noise_heights.GetLength(0); x++) {
            for (int y = 0; y < noise_heights.GetLength(1); y++) {

                if(normalize_mode == NormalizeMode.Local) {
                    
                    noise_heights[x,y] = Mathf.InverseLerp(local_min_height, local_max_height, noise_heights[x,y]);

                }else if(normalize_mode == NormalizeMode.Global) {

                    float normalized_height = (noise_heights[x,y] + 1) / (2f * max_possible_height / 1.75f) ; // L
                    noise_heights[x,y] = Mathf.Clamp(normalized_height, 0, int.MaxValue);

                }
            }
        }


        return noise_heights;
    }


}
