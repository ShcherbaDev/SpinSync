// Editor-only code that implements the Notes preview in Unity Timeline
// for easier level creation

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public partial class NoteSpawner
{
	private List<Note> _previewNotes = new List<Note>();

	partial void UpdateEditModePreview()
	{
		if (_director == null || _player == null || _pendingMarkers.Count == 0)
			return;

		RefreshPreviewNotes();
	}

	partial void OnEditorDisable()
	{
		if (!Application.isPlaying)
			ClearPreviewNotes();
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
}
#endif
