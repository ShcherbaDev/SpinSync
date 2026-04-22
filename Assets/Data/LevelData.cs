using UnityEngine;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level Data")]
public class LevelData : ScriptableObject
{
	[Header("Song Metadata")]
	public string Title;
	public string Artist;

	[Min(1f)] public float BPM = 120f;

	[Range(1, 5), Tooltip("Star rating (1-5) displayed on the song card")]
	public int Difficulty = 1;

	[Tooltip("Subtle tint applied to the card and background when this song is selected")]
	public Color AccentColor = new Color(0.4f, 0.7f, 1f, 1f);

	[Header("Audio")]
	public AudioClip Song;

	[Min(0f), Tooltip("Start offset (seconds) of the looped preview snippet")]
	public float PreviewStartTime;

	[Header("Gameplay")]
	[Tooltip("Timeline containing the note markers + audio track for this song. Bound to the Gameplay scene's PlayableDirector at runtime.")]
	public TimelineAsset Timeline;

	public float NoteTravelDuration = 0.5f;
}
