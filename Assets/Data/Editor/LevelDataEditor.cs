using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		LevelData data = (LevelData)target;

		EditorGUILayout.Space();

		using (new EditorGUI.DisabledScope(data.Song == null))
		{
			if (GUILayout.Button("Estimate BPM from Audio"))
			{
				if (BPMEstimator.TryEstimate(data.Song, out float bpm, out string error))
				{
					Undo.RecordObject(data, "Estimate BPM");
					data.BPM = Mathf.Round(bpm * 10f) / 10f;
					EditorUtility.SetDirty(data);
					Debug.Log($"[LevelData] Estimated BPM: {data.BPM}");
				}
				else
				{
					EditorUtility.DisplayDialog("BPM Estimation Failed", error, "OK");
				}
			}
		}

		if (data.Song == null)
			EditorGUILayout.HelpBox("Assign an Audio Clip above to estimate BPM.", MessageType.Info);
	}
}
