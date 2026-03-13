using UnityEngine;
using UnityEngine.Timeline;

[System.Serializable]
public class NoteMarker : Marker
{
	[Range(0f, 360f)]
	public float Angle;
}
