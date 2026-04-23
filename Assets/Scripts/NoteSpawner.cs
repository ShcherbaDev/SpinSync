using System.Collections.Generic;
using SpinSync.EditorRuntime;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[ExecuteAlways]
public partial class NoteSpawner : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private Note _notePrefab;
	[SerializeField] private Player _player;
	[SerializeField] private PlayableDirector _director;
	[SerializeField] private float _noteTravelDuration = 0.5f;

	public System.Action<Note> OnNoteSpawned;

	private List<NoteMarker> _pendingMarkers = new List<NoteMarker>();
	private List<float> _pendingTriggerTimes = new List<float>();
	private int _nextMarkerIndex;

	partial void OnEditorDisable();
	partial void UpdateEditModePreview();

	private void Start()
	{
		_pendingMarkers.Clear();
		_pendingTriggerTimes.Clear();
		_nextMarkerIndex = 0;

		TimelineAsset timeline = _director?.playableAsset as TimelineAsset;
		if (timeline == null)
			return;

		foreach (IMarker marker in timeline.markerTrack.GetMarkers())
		{
			if (marker is NoteMarker noteMarker)
				_pendingMarkers.Add(noteMarker);
		}

		_pendingMarkers.Sort((a, b) => a.time.CompareTo(b.time));

		_pendingTriggerTimes.Capacity = _pendingMarkers.Count;
		foreach (NoteMarker m in _pendingMarkers)
			_pendingTriggerTimes.Add((float)m.time);
	}

	private void Update()
	{
		if (Application.isPlaying)
			UpdatePlayMode();
		else
			UpdateEditModePreview();
	}

	private void OnDisable()
	{
		OnEditorDisable();
	}

	private void SpawnNote(float angle)
	{
		Note newNote = Instantiate(_notePrefab, Vector3.zero, Quaternion.identity);
		newNote.Init(_noteTravelDuration, _player.Radius, angle);
		OnNoteSpawned?.Invoke(newNote);
	}

	private void UpdatePlayMode()
	{
		_nextMarkerIndex = NoteScheduler.AdvancePending(
			_pendingTriggerTimes,
			_nextMarkerIndex,
			(float)_director.time,
			_noteTravelDuration,
			i => SpawnNote(_pendingMarkers[i].Angle));
	}
}
