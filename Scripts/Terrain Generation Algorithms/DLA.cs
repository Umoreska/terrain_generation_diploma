
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Timeline;

public class DLA : MonoBehaviour
{
    static GameObject cubes_parent = null; 
    static public int initialGridSize = 3; // Початковий розмір сітки
    public const int upscaleFactor = 2;    // Фактор збільшення розміру сітки
    static public int upscaleSteps = 3;     // Кількість етапів збільшення
    static int pixelsPerStep = 10;   // Кількість пікселів на кожному етапі

    static List<Pixel> pixels;
    static private Pixel mainPixel;
    static Pixel[,] grid;
    static float[,] height_map;

    public static void RunDLA() {
        if(cubes_parent == null) {
            cubes_parent = GameObject.Find("cubes_parent");
        }else {
            foreach(Transform child in cubes_parent.transform) {
                Destroy(child.gameObject);
            }
        }

        // Початкова генерація
        grid = new Pixel[initialGridSize, initialGridSize];
        height_map = new float[initialGridSize, initialGridSize];
        pixels = new List<Pixel>();
        
        // Початковий піксель в центрі
        Vector2Int start_pos = new Vector2Int(initialGridSize / 2, initialGridSize / 2);

        mainPixel = new Pixel(start_pos, null, Pixel.PixelType.New);
        grid[start_pos.x, start_pos.y] = mainPixel;
        pixels.Add(mainPixel);
        
        pixelsPerStep = grid.GetLength(0)*2;
        for (int i = 0; i < pixelsPerStep; i++) {
            AddRandomPixel();
        }
        AddGridValuesToHeightMap(1f);
        PrintPixelsOnScene(0);

        // Генерація пікселів і зв'язків на кожному етапі
        for (int step = 0; step < upscaleSteps; step++) {
            UpscaleGrid();
            UpscaleHeightMap();
            // Після масштабування сітки додаємо пікселі
            pixelsPerStep = grid.GetLength(0)*2;
            for (int i = 0; i < pixelsPerStep; i++) {
                //AddRandomPixel();
            }
            PrintPixelsOnScene(step+1);
            AddGridValuesToHeightMap(1f / Mathf.Pow(2, step+1));
        }

        Debug.Log($"pixels {pixels.Count}"); {
            foreach (var pixel in pixels) {
                Debug.Log($"{pixel.position}");
            }
        }

        AddGridValuesToHeightMap(1f / Mathf.Pow(2, upscaleSteps+1));
        BlurHeightMap();
        
        
    }

    static void AddGridValuesToHeightMap(float strength) {
        for(int i = 0; i < grid.GetLength(0); i++) {
            for(int j = 0; j < grid.GetLength(1); j++) {
                if(grid[i,j] == null) continue;
                height_map[i,j] += grid[i,j].value*strength;
            }
        }
    }

    static void PrintPixelsOnScene(int map_offset=0) {

        float max_value = float.MinValue;
        map_offset = grid.GetLength(0);

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
                    float cube_height = max_value - Mathf.Abs(grid[i,j].value);
                    CreateCube(new Vector2Int(i + map_offset, j), cube_height, grid[i,j].type);
                }else {
                    CreateCube(new Vector2Int(i + map_offset,j), 0.001f, Pixel.PixelType.None);
                }

                //CreateCube(new Vector2Int(i + map_offset, j), height_map[i,j] + 0.001f);

            }
        }
    }

    private static void CreateCube(Vector2Int position, float cube_height, Pixel.PixelType pixel_type) {
        float position_y = cube_height / 2;
        GameObject pixel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pixel.transform.parent = cubes_parent.transform;
        pixel.transform.localScale = new Vector3(0.8f, cube_height, 0.8f);
        pixel.transform.position = new Vector3(position.x,position_y,position.y);

        Renderer renderer = pixel.GetComponent<Renderer>();
        switch(pixel_type) {
            case Pixel.PixelType.None:
            break;
            case Pixel.PixelType.New:
                renderer.material.color = Color.blue;
            break;
            case Pixel.PixelType.Mid:
                renderer.material.color = Color.magenta;
            break;
            case Pixel.PixelType.Old:
                renderer.material.color = Color.red;
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

    static void AddRandomPixel() {
        Vector2Int randomPos;// = new Vector2Int(Random.Range(0, grid.GetLength(0)), Random.Range(0, grid.GetLength(1)));
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        /*randomPos = new Vector2Int(Random.Range(0, grid.GetLength(0)), Random.Range(0, grid.GetLength(1)));
        while(grid[randomPos.x, randomPos.y] != null) {
            randomPos = new Vector2Int(Random.Range(0, grid.GetLength(0)), Random.Range(0, grid.GetLength(1)));
        }*/

        // Randomly choose which edge to pick from: top, bottom, left, or right
        int edge = Random.Range(0, 4);
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
        }
        Debug.Log($"born in {randomPos}");
        MovePixelToConnection(randomPos);
    }



    static void MovePixelToConnection(Vector2Int pos)
    {
        while (true)
        {
            Vector2Int newPos = pos + RandomDirection();

            // making sure it is in bounds
            while(IsInBounds(newPos.x, newPos.y) == false) {
                newPos = pos + RandomDirection();
            }
            
            // Якщо піксель стикається з існуючим
            if (IsAdjacentToFrozenPixel(newPos, out var adjacent_pos)) {
                Debug.Log($"now live in: {newPos}, connected to: {adjacent_pos}");

                Pixel adjacent_pixel = grid[adjacent_pos.x, adjacent_pos.y];
                Pixel new_pixel = new Pixel(newPos, adjacent_pixel, Pixel.PixelType.New);
                if(adjacent_pixel == mainPixel) {
                    Debug.LogWarning("blyat i dont understand bro wtf");
                    Debug.Log($"children count of mainPixel: {mainPixel.children.Count}");
                }
                grid[newPos.x, newPos.y] = new_pixel;
                pixels.Add(new_pixel);
                return;
            }
            pos = newPos;
        }
    }
    static bool IsInBounds(int x, int y) {
        return x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1);
    }

    static bool IsAdjacentToFrozenPixel(Vector2Int pos, out Vector2Int adjacent) {        
        adjacent = Vector2Int.zero;
        if(IsInBounds(pos.x + 1, pos.y) && grid[pos.x + 1, pos.y] != null) { // x+1, y
            adjacent = new Vector2Int(pos.x + 1, pos.y);
            return true;
        }
        if(IsInBounds(pos.x - 1, pos.y) && grid[pos.x - 1, pos.y] != null) { // x-1, y
            adjacent = new Vector2Int(pos.x - 1, pos.y);
            return true;
        }
        if(IsInBounds(pos.x, pos.y+1) && grid[pos.x, pos.y+1] != null) { // x, y+1
            adjacent = new Vector2Int(pos.x, pos.y+1);
            return true;
        }
        if(IsInBounds(pos.x, pos.y-1) && grid[pos.x, pos.y-1] != null) { // x, y-1
            adjacent = new Vector2Int(pos.x, pos.y-1);
            return true;
        }
        return false;
    }

    static Vector2Int RandomDirection() => new Vector2Int(Random.Range(-1, 2), Random.Range(-1, 2));

    static void UpscaleGrid() {
        // Зберігаємо старий розмір та зв'язки
        int oldSize = grid.GetLength(0);
        int newSize = oldSize * upscaleFactor;
        grid = new Pixel[newSize, newSize];

        CreateConnections(mainPixel);

        CalculateValues();

        foreach(var pixel in pixels) {
            grid[pixel.position.x, pixel.position.y] = pixel;
        }
    }

    static private void CreateConnections(Pixel pixel) {
 
        Pixel[] children = pixel.children.ToArray();
        pixel.children.Clear();
        Debug.Log($"Connection. children count: {children.Length}");

        foreach(Pixel child in children) {
            Vector2Int connecter_pos = pixel.position * upscaleFactor + (child.position - pixel.position);
            Pixel connecter = new Pixel(connecter_pos, pixel, Pixel.PixelType.Mid);
            pixels.Add(connecter);


            connecter.parent = pixel;
            child.parent = connecter;
            connecter.children.Add(child);

            CreateConnections(child);
        }
        pixel.type = Pixel.PixelType.Old;
        pixel.position *= upscaleFactor;
    }

    static private void CalculateValues() {
        foreach(var pixel in pixels) {
            if(pixel.children.Count == 0) { // then it the last one
                int value = 1;
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

    static void UpscaleHeightMap() {
        int oldSize = height_map.GetLength(0);
        int newSize = oldSize * upscaleFactor;
        float[,] newHeightMap = new float[newSize, newSize];

        // Масштабування з використанням лінійної інтерполяції
        for (int x = 0; x < newSize; x++)
        {
            for (int y = 0; y < newSize; y++)
            {
                float oldX = (float)x / (newSize - 1) * (oldSize - 1);
                float oldY = (float)y / (newSize - 1) * (oldSize - 1);

                int xFloor = Mathf.FloorToInt(oldX);
                int yFloor = Mathf.FloorToInt(oldY);
                int xCeil = Mathf.Min(xFloor + 1, oldSize - 1);
                int yCeil = Mathf.Min(yFloor + 1, oldSize - 1);

                float xLerp = oldX - xFloor;
                float yLerp = oldY - yFloor;

                // Лінійна інтерполяція між сусідніми пікселями
                float top = Mathf.Lerp(height_map[xFloor, yFloor], height_map[xCeil, yFloor], xLerp);
                float bottom = Mathf.Lerp(height_map[xFloor, yCeil], height_map[xCeil, yCeil], xLerp);
                float heightValue = Mathf.Lerp(top, bottom, yLerp);

                //newHeightMap[x, y] = Mathf.RoundToInt(heightValue);
                newHeightMap[x, y] = heightValue;
            }
        }

        height_map = newHeightMap; // Оновлюємо карту висот
    }


    static void BlurHeightMap() {
        float[,] blurredMap = new float[height_map.GetLength(0), height_map.GetLength(1)];

        // Просте ядро для згортки (усереднення сусідніх пікселів)
        int[,] kernel = { { 1, 1, 1 }, { 1, 8, 1 }, { 1, 1, 1 } };
        int kernelSum = 9; // Сума всіх значень ядра

        // Проходимо по кожному пікселю карти висот, крім країв
        for (int x = 1; x < height_map.GetLength(0) - 1; x++)
        {
            for (int y = 1; y < height_map.GetLength(1) - 1; y++)
            {
                float sum = 0;

                // Згортка з сусідами (3x3 сусідні елементи)
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        sum += height_map[x + i, y + j] * kernel[i + 1, j + 1];
                    }
                }

                // Усереднене значення для пікселя
                //blurredMap[x, y] = Mathf.RoundToInt(sum / (float)kernelSum);
                blurredMap[x, y] = sum / kernelSum;
            }
        }

        height_map = blurredMap; // Оновлюємо масив висот після згладжування
    }





    class Pixel{  
        public enum PixelType{
            None ,New, Mid, Old
        }   
        public PixelType type; // for visualisation   
        public Vector2Int position;
        public Pixel parent; // this pixel is connected to parent
        public List<Pixel> children; // pixels connected to this one
        public float value;
        public bool is_middle = false;
        public Pixel(Vector2Int positiion, Pixel parent, PixelType type){
            this.position = positiion;
            this.type = type;

            this.parent = parent;
            children = new List<Pixel>();
            if(parent != null){
                parent.children.Add(this);
            }
            
        }
    }

}

