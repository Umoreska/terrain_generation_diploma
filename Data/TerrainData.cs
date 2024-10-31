using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainData", menuName = "ScriptableObjects/TerrainData", order = 1)]
public class MyTerrainData : UpdateableData
{
    public float uniform_scale = 2f; // scales x,z
    public bool useFlatShading;
	public bool useFalloff=false;
	public float heightMultiplier; // scales y axis
	public AnimationCurve mesh_height_curve;
}
