
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class DLA : MonoBehaviour
{
    static public int initialGridSize = 5; // Початковий розмір сітки
    public const int upscaleFactor = 2;    // Фактор збільшення розміру сітки
    static public int upscaleSteps = 3;     // Кількість етапів збільшення
    static int pixelsPerStep = 10;   // Кількість пікселів на кожному етапі

    static DLA_Connection[,] connections_grid;
    static int[,] grid;
    static float[,] height_map;

    public static void RunDLA() {
        // Початкова генерація
        grid = new int[initialGridSize, initialGridSize];
        height_map = new float[initialGridSize, initialGridSize];
        connections_grid = new DLA_Connection[initialGridSize,initialGridSize];
        
        // Початковий піксель в центрі
        Vector2Int start_pos = new Vector2Int(initialGridSize / 2, initialGridSize / 2);
        grid[start_pos.x, start_pos.y] = 1;
        connections_grid[start_pos.x, start_pos.y] = new DLA_Connection(start_pos, null, DLA_Connection.PixelType.New);
        
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
            AddGridValuesToHeightMap(1f / Mathf.Pow(2, step+1));
            PrintPixelsOnScene(step+1);
        }

        //JiggleGrid();
        AddGridValuesToHeightMap(1f / Mathf.Pow(2, upscaleSteps+1));
        BlurHeightMap();
        
        
    }

    static void AddGridValuesToHeightMap(float strength) {
        for(int i = 0; i < grid.GetLength(0); i++) {
            for(int j = 0; j < grid.GetLength(1); j++) {
                height_map[i,j] += grid[i,j]*strength;
            }
        }
    }

    static void PrintPixelsOnScene(int map_offset=0) {

        float max_value = float.MinValue;
        map_offset = connections_grid.GetLength(0);

        // getting max-value
        for(int i = 0; i < connections_grid.GetLength(0); i++) {
            for(int j = 0; j < connections_grid.GetLength(1); j++) {
                if(connections_grid[i,j]!=null) {
                    if(Mathf.Abs(connections_grid[i,j].value) > max_value) {
                        max_value = Mathf.Abs(connections_grid[i,j].value);
                    }
                }
            }
        }
        max_value += 1; // so the max value will not be 0;
        Debug.Log($"max_value: {max_value}");


        // creating cubes in scene. for better look
        for(int i = 0; i < grid.GetLength(0); i++) {
            for(int j = 0; j < grid.GetLength(1); j++) {

                if(grid[i,j] == 1 || connections_grid[i,j] != null) {
                    if(grid[i,j] != 1 || connections_grid[i,j] == null) { // checking if one of them is not assigned.
                        Debug.LogWarning($"grid and connection are not sync. grid[{i},{j}]: {grid[i,j]}; connection_grid[{i},{j}]: {connections_grid[i,j]}");Debug.Break();
                        return;
                    }

                    float cube_height = max_value - Mathf.Abs(connections_grid[i,j].value);
                    CreateCube(new Vector2Int(i + map_offset, j), cube_height, connections_grid[i,j].type);
                }else {
                    CreateCube(new Vector2Int(i + map_offset,j), 0.001f, DLA_Connection.PixelType.None);
                }

                //CreateCube(new Vector2Int(i + map_offset, j), height_map[i,j] + 0.001f);

            }
        }
    }

    private static void CreateCube(Vector2Int position, float cube_height, DLA_Connection.PixelType pixel_type) {
        float position_y = cube_height / 2;
        GameObject pixel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pixel.transform.localScale = new Vector3(0.8f, cube_height, 0.8f);
        pixel.transform.position = new Vector3(position.x,position_y,position.y);

        Renderer renderer = pixel.GetComponent<Renderer>();
        switch(pixel_type) {
            case DLA_Connection.PixelType.None:
            break;
            case DLA_Connection.PixelType.New:
                renderer.material.color = Color.blue;
            break;
            case DLA_Connection.PixelType.Mid:
                renderer.material.color = Color.magenta;
            break;
            case DLA_Connection.PixelType.Old:
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
    static void PrintValues(DLA_Connection[,] grid) {
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
                grid[newPos.x, newPos.y] = 1;

                // Збереження зв'язку
                DLA_Connection adjacent = connections_grid[adjacent_pos.x,adjacent_pos.y];
                connections_grid[newPos.x, newPos.y] = new DLA_Connection(newPos, adjacent, DLA_Connection.PixelType.New);
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
        if(IsInBounds(pos.x + 1, pos.y) && grid[pos.x + 1, pos.y] == 1) { // x+1, y
            adjacent = new Vector2Int(pos.x + 1, pos.y);
            return true;
        }
        if(IsInBounds(pos.x - 1, pos.y) && grid[pos.x - 1, pos.y] == 1) { // x-1, y
            adjacent = new Vector2Int(pos.x - 1, pos.y);
            return true;
        }
        if(IsInBounds(pos.x, pos.y+1) && grid[pos.x, pos.y+1] == 1) { // x, y+1
            adjacent = new Vector2Int(pos.x, pos.y+1);
            return true;
        }
        if(IsInBounds(pos.x, pos.y-1) && grid[pos.x, pos.y-1] == 1) { // x, y-1
            adjacent = new Vector2Int(pos.x, pos.y-1);
            return true;
        }
        return false;
    }

    static Vector2Int RandomDirection() => new Vector2Int(Random.Range(-1, 2), Random.Range(-1, 2));

    static private List<DLA_Connection> position_changed = new List<DLA_Connection>();
    static void UpscaleGrid() {
        // Зберігаємо старий розмір та зв'язки
        int oldSize = grid.GetLength(0);
        int newSize = oldSize * upscaleFactor;
        int[,] newGrid = new int[newSize, newSize];
        DLA_Connection[,] newConnectionGrid = new DLA_Connection[newSize, newSize];

        for(int i = 0; i < oldSize; i++) {
            for(int j = 0; j < oldSize; j++) {

                if(connections_grid[i,j] != null) {

                    DLA_Connection start = connections_grid[i,j]; // its x in x -> y
                    Vector2Int new_start_pos = new Vector2Int(i*upscaleFactor, j*upscaleFactor);
                    newConnectionGrid[new_start_pos.x,new_start_pos.y] = start;
                    newConnectionGrid[new_start_pos.x,new_start_pos.y].position = new Vector2Int(new_start_pos.x, new_start_pos.y); // updating position of start
                    start.type = DLA_Connection.PixelType.Old;
                    position_changed.Add(start);
                    
                    //position_changed.Add(start);
                    Debug.Log($"start before: {i};{j}, after: {new_start_pos}"); // for debugging n shi

                    newGrid[new_start_pos.x,new_start_pos.y] = 1;


                    if(connections_grid[i,j].connected_to == null) { // central pixel is not connected to anyone
                        Debug.Log("null");
                        continue;
                    }


                    DLA_Connection end = connections_grid[i,j].connected_to; // its y in x -> y

                    Vector2Int new_end_pos = end.position;
                    if(position_changed.Contains(end) == false) {
                        new_end_pos = new Vector2Int(end.position.x*upscaleFactor, end.position.y*upscaleFactor);
                    }
                    //newConnectionGrid[new_end_pos.x,new_end_pos.y] = end; <------------
                    //newGrid[new_end_pos.x,new_end_pos.y] = 1; <-----------
                    end.type = DLA_Connection.PixelType.Old;


                    //Vector2Int mid_pos = new Vector2Int(Mathf.FloorToInt((new_start_pos.x + new_end_pos.x) / 2.0f), 
                    //                              Mathf.FloorToInt((new_start_pos.y + new_end_pos.y) / 2.0f));

                    Vector2Int mid_pos = new Vector2Int((new_start_pos.x + new_end_pos.x) / 2, (new_start_pos.y + new_end_pos.y) / 2);
                                                    
                    if(newConnectionGrid[mid_pos.x, mid_pos.y] != null) {//somehow, middle position between two pixels on NEW FUCKING GRID is already occupied by some fucker
                        Debug.LogWarning($"WTF!!!. type of that sucker: {newConnectionGrid[mid_pos.x, mid_pos.y].type}");
                        //continue;
                    }

                    DLA_Connection mid_dla = new DLA_Connection(mid_pos, end, DLA_Connection.PixelType.Mid, false);
                    mid_dla.is_middle = true;
                    newConnectionGrid[mid_pos.x, mid_pos.y] = mid_dla;
                    newGrid[mid_pos.x,mid_pos.y] = 1;

                    mid_dla.value = (start.value + end.value) / 2; // fuck it, let it be half idk U_U
                    //newConnectionGrid[new_start_pos.x, new_start_pos.y].connected_to = newConnectionGrid[mid_pos.x, mid_pos.y];
                    //newConnectionGrid[mid_pos.x, mid_pos.y].connected_to = newConnectionGrid[new_end_pos.x, new_end_pos.y];

                    start.connected_to = mid_dla;
                    mid_dla.connected_to = end;

                    Debug.Log($"mid: {mid_pos.x};{mid_pos.y}: {newGrid[mid_pos.x,mid_pos.y]}, value: {newConnectionGrid[mid_pos.x, mid_pos.y].value}");
                    Debug.Log($"end before: {end.position}, after: {new_end_pos}");
                }
            }
        }
        // Оновлюємо grid
        grid = newGrid;
        connections_grid = newConnectionGrid;
        UpdateConnectionPositions();
        position_changed.Clear();
    }

    private static void UpdateConnectionPositions() {
        for(int i = 0; i < connections_grid.GetLength(0); i++) {
            for(int j = 0; j < connections_grid.GetLength(1); j++) {
                if(connections_grid[i,j] == null) continue;
                
                connections_grid[i,j].position = new Vector2Int(i,j);
            }
        }
    }

    static List<DLA_Connection> jiggled = new List<DLA_Connection>();
    private static void JiggleGrid() {
        for(int i = 0; i < connections_grid.GetLength(0); i++) {
            for(int j = 0; j < connections_grid.GetLength(1); j++) {
                if(connections_grid[i,j] == null || connections_grid[i,j].connected_to == null) {
                    continue;
                }

                DLA_Connection target = connections_grid[i,j].connected_to;
                if(jiggled.Contains(target)) {
                    continue;
                }
                
                Vector2Int connection_direction = target.position - connections_grid[i,j].position;
                Vector2Int jiggle_direction;
                if(Random.Range(0, 2) == 0) {
                    jiggle_direction = new Vector2Int(-connection_direction.y, connection_direction.x);
                }else {
                    jiggle_direction = new Vector2Int(connection_direction.y, -connection_direction.x);
                }

                Vector2Int jiggle_position = new Vector2Int(target.position.x + jiggle_direction.x, target.position.y + jiggle_direction.y);
                if(IsInBounds(jiggle_position.x, jiggle_position.y) == false) {
                    jiggle_direction = -jiggle_direction;
                    jiggle_position = new Vector2Int(target.position.x + jiggle_direction.x, target.position.y + jiggle_direction.y);
                    if(IsInBounds(jiggle_position.x, jiggle_position.y) == false) {
                        // well, there is nothing we can do then
                        return;
                    }
                }
                // jiggle to side if it only is not occupied
                if(connections_grid[jiggle_position.x, jiggle_position.y] == null) { // if its empty
                    connections_grid[jiggle_position.x, jiggle_position.y] = target;
                                grid[jiggle_position.x, jiggle_position.y] = 1;
                    connections_grid[target.position.x, target.position.y] = null;
                                grid[target.position.x, target.position.y] = 0;

                    target.position = jiggle_position;
                    jiggled.Add(target);
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





    class DLA_Connection{  
        public enum PixelType{
            None ,New, Mid, Old
        }   
        public PixelType type;   
        public Vector2Int position;
        public Vector2Int connected_position;
        public DLA_Connection connected_to;
        public float value;
        public bool is_middle = false;
        public DLA_Connection(Vector2Int positiion, DLA_Connection connected_to, PixelType type,bool assign_value=true){
            this.position = positiion;
            this.type = type;
            if(connected_to != null) {
                this.connected_to = connected_to;
                connected_position = connected_to.position;
                if(assign_value) {
                    value = connected_to.value-1;
                }
            }
        }
    }

}

