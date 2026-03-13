using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class NoteSpawner : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private Note _notePrefab;
	[SerializeField] private Player _player;
	[SerializeField] private PlayableDirector _director;
	[SerializeField] private float _noteTravelDuration = 0.5f;

	public System.Action<Note> OnNoteSpawned;

	private List<NoteMarker> _pendingMarkers = new List<NoteMarker>();
	private int _nextMarkerIndex;

	private void Start()
	{
		TimelineAsset timeline = _director.playableAsset as TimelineAsset;
		if (timeline == null)
			return;

		foreach (IMarker marker in timeline.markerTrack.GetMarkers())
		{
			if (marker is NoteMarker noteMarker)
				_pendingMarkers.Add(noteMarker);
		}

		_pendingMarkers.Sort((a, b) => a.time.CompareTo(b.time));
	}

	private void Update()
	{
		while (_nextMarkerIndex < _pendingMarkers.Count)
		{
			NoteMarker marker = _pendingMarkers[_nextMarkerIndex];

			if ((float)_director.time >= (float)marker.time - _noteTravelDuration)
			{
				SpawnNote(marker.Angle);
				_nextMarkerIndex++;
			}
			else
				break;
		}
	}

	private void SpawnNote(float angle)
	{
		Note newNote = Instantiate(_notePrefab, Vector3.zero, Quaternion.identity);
		newNote.Init(_noteTravelDuration, _player.Radius, angle);
		OnNoteSpawned?.Invoke(newNote);
	}
}
