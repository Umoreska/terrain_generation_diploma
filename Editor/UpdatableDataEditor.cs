using UnityEngine;
using System.Collections;
using UnityEditor;
[CustomEditor(typeof(UpdateableData), true)]
public class UpdatableDataEditor : Editor
{
  	public override void OnInspectorGUI() {

        base.OnInspectorGUI();

		UpdateableData data = (UpdateableData)target;

		if (GUILayout.Button("Update")) {
			data.NotifyOfUpdatedValues();
			EditorUtility.SetDirty(target);
		}
	}
}
