

using UnityEngine;
using Unity;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Timeline;
using System.Security.Claims;


public class DLA : MonoBehaviour {
    [SerializeField] private MapDisplay map_display;
    static GameObject cubes_parent = null; 
    static public int initialGridSize = 3; // Початковий розмір сітки
    public const int UPSCALE_FACTOR = 2;    // Фактор збільшення розміру сітки
    static public int upscaleSteps = 6;     // Кількість етапів збільшення
    static int pixelsPerStep = 10;   // Кількість пікселів на кожному етапі

    static int step = 1; // how often this dla was upscaled already 

    static readonly Vector2Int[] offsets = { new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(-1,0) };

    static List<Pixel> pixels;
    static public Pixel mainPixel;
    static Pixel[,] grid;
    static float[] image;

    static int size;

    public void RunDLA(int start_size, int upscale_count) {

        // parent for cubes that are used for pixels visualisation
        if(cubes_parent == null) {
            cubes_parent = GameObject.Find("cubes_parent");
            if(cubes_parent == null) {
                cubes_parent = new GameObject();
            }
        }else {
            foreach(Transform child in cubes_parent.transform) {
                Destroy(child.gameObject);
            }
        }

        initialGridSize = start_size;
        // Початкова генерація
        grid = new Pixel[initialGridSize, initialGridSize];
        image = new float[initialGridSize*initialGridSize];
        pixels = new List<Pixel>();
        
        // Початковий піксель в центрі
        Vector2Int start_pos = new Vector2Int(initialGridSize / 2, initialGridSize / 2);

        mainPixel = new Pixel(start_pos, null, Pixel.PixelType.MAIN);
        grid[start_pos.x, start_pos.y] = mainPixel;
        pixels.Add(mainPixel);
        
        pixelsPerStep = grid.GetLength(0)*2;


        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();        
        sw.Start();

        AddPixels(pixelsPerStep);
        UpscaleTexture();

        sw.Stop();
        Debug.Log($"initialized dla. added: {pixelsPerStep}. spent: {sw.ElapsedMilliseconds} ms");

        //PrintPixelsOnScene(0);

        upscaleSteps = upscale_count;
        // Генерація пікселів і зв'язків на кожному етапі
        for (int step = 0; step < upscaleSteps; step++) {
            sw.Restart();

            UpscaleGrid();
            AddPixels(grid.GetLength(0)/2 * grid.GetLength(0)/2);
            UpscaleTexture();
            
            sw.Stop();
            Debug.Log($"step: {step}. added: {grid.GetLength(0)/2 * grid.GetLength(0)/2}. spent: {sw.ElapsedMilliseconds} ms");
        }
        //PrintPixelsOnScene(0);
        map_display.DrawNoiseMap(image, grid.GetLength(0));
        
        /* Example of a generation:
        DLA dla(12);
        dla.AddPixels(12);
        dla.UpscaleTexture();
        
        dla.Upscale();
        dla.AddPixels(24);
        dla.UpscaleTexture();
        
        dla.Upscale();
        dla.AddPixels(9 * 24);
        dla.UpscaleTexture();

        dla.Upscale();
        dla.AddPixels(9 * 9 * 24);
        dla.UpscaleTexture();

        dla.Upscale();
        dla.AddPixels(3 * 9 * 9 * 24);
        dla.UpscaleTexture();
        
        dla.GenTexture();

        Note that the steps UpscaleTexture(); and Upscale(); have to be used consecutively.
        */
    }

    static void AddGridValuesToHeightMap(float strength) {
        int width = grid.GetLength(0);
        for(int i = 0; i < grid.GetLength(0); i++) {
            for(int j = 0; j < grid.GetLength(1); j++) {
                if(grid[i,j] == null) continue;
                image[i*width + j] += grid[i,j].value*strength;
            }
        }
    }

    static void PrintPixelsOnScene(int map_offset=0) {
        mainPixel.type = Pixel.PixelType.MAIN;

        float max_value = float.MinValue;
        map_offset = grid.GetLength(0);
        int width = grid.GetLength(0);

        // getting max-value
        foreach(var pixel in pixels) {
            if(Mathf.Abs(pixel.value) > max_value) {
                max_value = Mathf.Abs(pixel.value);
            }
        }
        max_value += 1; // so the max value will not be 0;
        Debug.Log($"max_value: {max_value}");


        // creating cubes in scene. for better look
        for(int i = 0; i < grid.GetLength(0); i++) {
            for(int j = 0; j < grid.GetLength(1); j++) {

                if(grid[i,j] != null) {
                    if(grid[i,j] == mainPixel) {
                        Debug.Log($"printing mainPixel. its pos: {grid[i,j].position}");
                    }
                    //float cube_height = max_value - Mathf.Abs(grid[i,j].value);
                    CreateCube(new Vector2Int(i + map_offset, j), grid[i,j].value, grid[i,j]);
                }else {
                    CreateCube(new Vector2Int(i + map_offset,j), 0.001f, null);
                }

                //CreateCube(new Vector2Int(i + map_offset, j + map_offset), image[i*width + j] + 0.001f, null);

            }
        }
    }

    private static void CreateCube(Vector2Int position, float cube_height, Pixel pixel) {
        float position_y = cube_height / 2;
        GameObject cube_pixel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube_pixel.transform.parent = cubes_parent.transform;
        cube_pixel.transform.localScale = new Vector3(0.8f, cube_height, 0.8f);
        cube_pixel.transform.position = new Vector3(position.x,position_y,position.y);

        Renderer renderer = cube_pixel.GetComponent<Renderer>();
        if(pixel == null) {
            return;
        }else {
            cube_pixel.AddComponent<CubePixel>().pixel = pixel;
        }
        switch(pixel.type) {
            case Pixel.PixelType.None:
            break;
            case Pixel.PixelType.New:
                renderer.material.color = Color.blue;
            break;
            case Pixel.PixelType.Mid:
                renderer.material.color = Color.magenta;
                break;
            case Pixel.PixelType.Last:
                renderer.material.color = Color.white;
            break;
            case Pixel.PixelType.Old:
                renderer.material.color = Color.red;
            break;
            case Pixel.PixelType.MAIN:
                renderer.material.color = Color.yellow;
            break;
        }
    }

    static void PrintPixels(int[,] grid) {
        string line = string.Empty;
        for(int i = 0; i < grid.GetLength(0); i++) {
            for(int j = 0; j < grid.GetLength(1); j++) {
                line += $"{grid[i,j]}";
            }
            line += "\n";
        }
        Debug.Log(line);
    }
    static void PrintValues(Pixel[,] grid) {
        string line = string.Empty;
        for(int i = 0; i < grid.GetLength(0); i++) {
            for(int j = 0; j < grid.GetLength(1); j++) {
                if(grid[i,j] == null) {
                    line += " ";
                }else {
                    line += $"{i}{j}: {grid[i,j].value} ";
                }
            }
            line += "\n";
        }
        Debug.Log(line);
    }

    static void AddPixels(int amount=1) {
        if(amount < 1) {
            return;
        }

        Vector2Int randomPos;
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for(int i = 0; i < amount; i++) {


            randomPos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            while(grid[randomPos.x, randomPos.y] != null) { // change position untill grid element is null 
                randomPos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            }

            //Debug.Log($"born in {randomPos}");
            MovePixelToConnection(randomPos);
        }

        

        // Randomly choose which edge to pick from: top, bottom, left, or right
        /*int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: // Top edge
                randomPos = new Vector2Int(Random.Range(0, width), 0);
                break;
            case 1: // Bottom edge
                randomPos = new Vector2Int(Random.Range(0, width), height - 1);
                break;
            case 2: // Left edge
                randomPos = new Vector2Int(0, Random.Range(0, height));
                break;
            case 3: // Right edge
                randomPos = new Vector2Int(width - 1, Random.Range(0, height));
                break;
            default:
                randomPos = Vector2Int.zero; // Fallback
                break;
        }*/
    }



    static void MovePixelToConnection(Vector2Int pos)
    {
        int size = grid.GetLength(0);
        while (grid[pos.x, pos.y] == null) 
        {
            foreach(var offset in offsets) {
                if(pos.x + offset.x > 0 && pos.x + offset.x < size-1 && pos.y + offset.y > 0 && pos.y + offset.y < size-1) {
                    if(grid[pos.x + offset.x, pos.y + offset.y] != null)
                    {
                        //pixels.emplace_back(new Pixel());
                        //pixels.back()->position = pos;
                        //pixels.back()->parent = grid[pos.x + offset.x][pos.y + offset.y];
                        //grid[pos.x + offset.x][pos.y + offset.y]->children.emplace_back(pixels.back());
                        Pixel parent = grid[pos.x + offset.x, pos.y + offset.y];
                        Pixel new_pixel = new Pixel(pos, parent, Pixel.PixelType.New);
                        pixels.Add(new_pixel);

                        //grid[pos.x][pos.y] = pixels.back();
                        grid[pos.x, pos.y] = new_pixel;
                        break;
                    }
                }
            }

            if(grid[pos.x, pos.y] == null) {
                pos += offsets[Random.Range(0, 4)];
            }

            // Correct possible overshoots over the grid.
            pos.x = Mathf.Clamp(pos.x, 0, size-1);
            pos.y = Mathf.Clamp(pos.y, 0, size-1);

        }
    }
    static bool IsInBounds(int x, int y) {
        return x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1);
    }



    static void UpscaleGrid() {
        // Зберігаємо старий розмір та зв'язки
        int oldSize = grid.GetLength(0);
        int newSize = oldSize * UPSCALE_FACTOR;
        grid = new Pixel[newSize, newSize];

        CreateConnections(mainPixel);

        CalculateValues();

        foreach(var pixel in pixels) {
            grid[pixel.position.x, pixel.position.y] = pixel;
        }
    }

    static void UpscaleTexture() {

        CalculateValues();

        int image_size = grid.GetLength(0);

        // place current grid on the image
        for(int x=0; x<image_size; x++) {
            for(int y=0; y<image_size; y++) {
            
                float v = grid[x,y] != null ? 1f - 1f / (1f + 0.5f * grid[x,y].value) : 0f;

                int index = x * image_size + y;
                float fv = 1f - 1f / (1f + (1f / step) * v + 1.25f * image[index]);

                image[index + 0] = fv; // why +0 tho??

            }
        }
        ++step;

        // upscale the current image
        List<float> new_image_list = new List<float>();
        float multiplier = 1f / UPSCALE_FACTOR;
        //Debug.Log($"multiplier: {multiplier}");
        for(int x = 0; x < image_size * UPSCALE_FACTOR; x++) {
            for(int y = 0; y < image_size * UPSCALE_FACTOR; y++) {

                int mx = (int)Mathf.Floor(x * multiplier) * image_size; // mx - middle pixel.x
                int bx = x > 0 ? (int)Mathf.Floor((x - 1) * multiplier) * image_size : mx; // bx - pixel.x on the left from middle.x one

                int my = (int)Mathf.Floor(y * multiplier); // same but with y coordinate
                int by = y > 0 ? (int)Mathf.Floor((y - 1) * multiplier) : my;
                
                //Debug.Log($"image length: {image.Length}; mx: {mx}; bx: {bx}; my: {my}; by: {by};\nbx+by: {bx+by}; bx+my: {bx+my}; mx+by: {mx+by}; mx+my: {mx+my}");
                float v = 0.25f * image[bx + by] + 0.25f * image[bx + my] + 0.25f * image[mx + by] + 0.25f * image[mx + my];

                new_image_list.Add(v);
            }
        }

        image = new_image_list.ToArray();
        new_image_list.Clear();

        image_size *= UPSCALE_FACTOR;
        // bluring image using a convolution aproximation of gaussian blur
         for(int x = 0; x < image_size; x++) {
            for(int y = 0; y < image_size; y++) {

                int mx = x * image_size;
                int bx = x > 0 ? (x-1) * image_size : mx;
                int ax = x < image_size-1 ? (x+1) * image_size : mx;

                int my = y;
                int by = y > 0 ? y-1 : my;
                int ay = y < image_size-1 ? y+1 : my;
                
                //     min                      middle                         max
                float bxby = image[bx + by]; float bxmy = image[bx + my]; float bxay = image[bx + ay];
                float mxby = image[mx + by]; float mxmy = image[mx + my]; float mxay = image[mx + ay];
                float axby = image[ax + by]; float axmy = image[ax + my]; float axay = image[ax + ay];             

                float minWeight = 4f * (Random.Range(1, 11) / 10f);
                float midWeight = 8f * (Random.Range(1, 11) / 10f);
                float maxWeight = 16f * (Random.Range(1, 11) / 10f);
                
                float v = 1f / (4f*minWeight + 4f*midWeight + maxWeight) * (
                        minWeight*bxby + midWeight*bxmy + minWeight*bxay +
                        midWeight*mxby + maxWeight*mxmy + midWeight*mxay +
                        minWeight*axby + midWeight*axmy + minWeight*axay
                );

                new_image_list.Add(v);
            }
        }



    }

    static private void CreateConnections(Pixel pixel) {
 
        Pixel[] children = pixel.children.ToArray();
        pixel.children.Clear();

        foreach(Pixel child in children) {
            Vector2Int connecter_pos = pixel.position * UPSCALE_FACTOR + (child.position - pixel.position);
            Pixel connecter = new Pixel(connecter_pos, pixel, Pixel.PixelType.Mid);
            pixels.Add(connecter);


            //connecter.parent = pixel;
            child.parent = connecter;
            //pixel.children.Add(connector)
            connecter.children.Add(child);            

            CreateConnections(child);
        }
        pixel.type = Pixel.PixelType.Old;
        pixel.position *= UPSCALE_FACTOR;
    }

    static private void CalculateValues() {
        foreach(var pixel in pixels) {
            if(pixel.children.Count == 0) { // then it the last one
                int value = 1;
                pixel.type = Pixel.PixelType.Last;
                pixel.value = value;
                Pixel parent = pixel.parent;
                while(parent != null) {
                    value++;
                    parent.value = value;
                    parent = parent.parent;
                }
            }
        }
    }

    







    public class Pixel{  
        public enum PixelType{
            None ,New, Mid, Old, Last, MAIN
        }   
        public PixelType type; // for visualisation   
        public Vector2Int position;
        public Pixel parent; // this pixel is connected to parent
        public List<Pixel> children; // pixels connected to this one
        public float value;
        public Pixel(Vector2Int position, Pixel parent, PixelType type) {
            this.position = position;
            this.type = type;

            this.parent = parent;

            children = new List<Pixel>();
            parent?.children.Add(this);
            
        }

    }

}

