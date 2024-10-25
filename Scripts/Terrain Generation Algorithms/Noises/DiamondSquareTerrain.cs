using UnityEngine;

public class DiamondSquareTerrain : TerrainGenerationParent
{
    public int size = 513; // Повинно бути 2^n + 1    
    public int seed = 42;
    public static int static_seed;
    public float roughness = 2f;

    void Start()
    {        
        if(!generate) return;

        StartCoroutine(GenerateTerrainCoroutine());
    }

    protected override TerrainData GenerateTerrain(TerrainData terrainData) {
        static_seed = seed;
        terrainData.heightmapResolution = size;
        terrainData.size = new Vector3(size, heightScale, size);
        terrainData.SetHeights(0, 0, GenerateHeights(size, roughness, seed, false));
        return terrainData;
    }

    public static  float[,] GenerateHeights(int _size, float _roughness, int seed, bool random = true) {
        float[,] heights = new float[_size, _size];
        int max = _size - 1;

        if(random == false) {
            //Random.InitState(static_seed);
            Random.InitState(seed);
        }

        // Ініціалізуємо кути
        heights[0, 0] = Random.Range(0f, 1f);
        heights[0, max] = Random.Range(0f, 1f);
        heights[max, 0] = Random.Range(0f, 1f);
        heights[max, max] = Random.Range(0f, 1f);

        int stepSize = max;
        float scale = _roughness;

        while (stepSize > 1)
        {
            // Diamond step
            for (int x = 0; x < max; x += stepSize)
            {
                for (int y = 0; y < max; y += stepSize)
                {
                    float avg = (heights[x, y] +
                                heights[x + stepSize, y] +
                                heights[x, y + stepSize] +
                                heights[x + stepSize, y + stepSize]) * 0.25f;
                    heights[x + stepSize / 2, y + stepSize / 2] = avg + Random.Range(-scale, scale);
                }
            }

            // Square step
            for (int x = 0; x <= max; x += stepSize / 2)
            {
                for (int y = (x + stepSize / 2) % stepSize; y <= max; y += stepSize)
                {
                    float avg = (heights[(x - stepSize / 2 + _size) % _size, y] +
                                heights[(x + stepSize / 2) % _size, y] +
                                heights[x, (y + stepSize / 2) % _size] +
                                heights[x, (y - stepSize / 2 + _size) % _size]) * 0.25f;
                    heights[x, y] = avg + Random.Range(-scale, scale);

                    // Handle borders
                    if (x == 0) heights[max, y] = heights[x, y];
                    if (y == 0) heights[x, max] = heights[x, y];
                }
            }

            stepSize /= 2;
            scale /= 2f;
        }

        //SmoothHeights(heights);

        NormalizeHeights(heights);

        return heights;
    }

    public static  void NormalizeHeights(float[,] heights)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (heights[x, y] < min)
                {
                    min = heights[x, y];
                }

                if (heights[x, y] > max)
                {
                    max = heights[x, y];
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = (heights[x, y])/max; // в новій мапі мін стає 0, а мах - 1, тому від значень віднімається мін
            }
        }
    }

    private void SmoothHeights(float[,] heights)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                float sum = 0f;
                int count = 0;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        sum += heights[x + i, y + j];
                        count++;
                    }
                }

                heights[x, y] = sum / count;
            }
        }
    }

    
}
