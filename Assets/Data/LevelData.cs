using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoteData
{
	[Tooltip("Time in seconds when the Note should be pressed")]
	public float HitTimeSeconds;

	[Tooltip("Angle to set a direction of movement"), Range(0f, 360f)]
	public float Angle;
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Level Data")]
public class LevelData : ScriptableObject
{
	public AudioClip Song;
	public float NoteTravelDuration = 0.5f;
	public List<NoteData> Notes;
}
