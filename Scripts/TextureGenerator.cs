using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] colors, int width, int height) {
        Texture2D tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    public static Texture2D TextureFromHeightMap(float[,] noise_map) {
        int width = noise_map.GetLength(0);
        int height = noise_map.GetLength(1);

        Color[] color_map = new Color[width * height];

        for(int i = 0; i < height; i++) {
            for(int j = 0; j < width; j++) {
                color_map[i*width + j] = Color.Lerp(Color.black, Color.white, noise_map[i,j]);
            }
        }
        return TextureFromColorMap(color_map, width, height);
    }

}
