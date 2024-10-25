using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor {

	public override void OnInspectorGUI() {
		MapGenerator mapGen = (MapGenerator)target;

		if (DrawDefaultInspector ()) {
			if (mapGen.autoUpdate) {
				mapGen.DrawMapInEditor ();
			}
		}

		if (GUILayout.Button ("Generate")) {
			mapGen.DrawMapInEditor ();
		}
	}
}

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