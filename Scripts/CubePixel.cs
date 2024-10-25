using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePixel : MonoBehaviour
{
    public DLA.Pixel pixel;

    private void OnMouseDown() {
        string parent = pixel.parent != null ? pixel.parent.position+"": false+"";
        Debug.Log($"{(pixel == DLA.mainPixel?"main.":"")} pixtl pos: {pixel.position}; value: {pixel.value}; parent: {parent}; children: {pixel.children.Count}");
    }

}
