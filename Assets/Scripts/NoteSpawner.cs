using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[ExecuteAlways]
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

	private List<Note> _previewNotes = new List<Note>();

	private void Start()
	{
		_pendingMarkers.Clear();
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
	}

	private void Update()
	{
		if (Application.isPlaying)
			UpdatePlayMode();
		else
			UpdateEditModePreview();
	}

	private void SpawnNote(float angle)
	{
		Note newNote = Instantiate(_notePrefab, Vector3.zero, Quaternion.identity);
		newNote.Init(_noteTravelDuration, _player.Radius, angle);
		OnNoteSpawned?.Invoke(newNote);
	}

	// =====================
	//  Play mode-only code
	// =====================

	private void UpdatePlayMode()
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

	// ==================
	//  Editor-only code
	// ==================
	private void UpdateEditModePreview()
	{
		if (_director == null || _player == null || _pendingMarkers.Count == 0)
			return;

		RefreshPreviewNotes();
	}

	private void RefreshPreviewNotes()
	{
		ClearPreviewNotes();

		float directorTime = (float)_director.time;

		foreach (NoteMarker marker in _pendingMarkers)
		{
			float spawnTime = (float)marker.time - _noteTravelDuration;
			float destroyTime = (float)marker.time + _noteTravelDuration * 0.2f;

			if (directorTime >= spawnTime && directorTime <= destroyTime)
			{
				float progress = (directorTime - spawnTime) / _noteTravelDuration;
				SpawnPreviewNote(marker.Angle, progress);
			}
		}
	}

	private void SpawnPreviewNote(float angle, float progress)
	{
		Note previewNote = Instantiate(_notePrefab, Vector3.zero, Quaternion.identity);

		float angleRadian = angle * Mathf.Deg2Rad;
		Vector2 direction = new Vector2(Mathf.Sin(angleRadian), Mathf.Cos(angleRadian)).normalized;

		previewNote.transform.up = direction;
		previewNote.transform.position = direction * (progress * _player.Radius);

		previewNote.enabled = false;

		_previewNotes.Add(previewNote);
	}

	private void ClearPreviewNotes()
	{
		foreach (Note note in _previewNotes)
		{
			if (note != null)
				DestroyImmediate(note.gameObject);
		}

		_previewNotes.Clear();
	}

	private void OnDisable()
	{
		if (!Application.isPlaying)
			ClearPreviewNotes();
	}
}
