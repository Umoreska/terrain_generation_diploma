using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor (typeof (Tester))]
public class TesterEditor : Editor {

	public override void OnInspectorGUI() {
		
		Tester tester = (Tester)target;

		if (DrawDefaultInspector ()) {
			/*if (mapGen.autoUpdate) {
				mapGen.DrawMapInEditor ();
			}*/
		}

		if (GUILayout.Button ("Test Mesh")) {
			tester.TestMeshTerrainSpeedGeneration();
		}

		if (GUILayout.Button ("Test Noise")) {
			tester.TestBuiltInNoiseAndImprovedNoiseSpeed();
		}
	}
}
