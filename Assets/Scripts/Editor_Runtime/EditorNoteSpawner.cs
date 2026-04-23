using System.Collections.Generic;
using UnityEngine;

namespace SpinSync.EditorRuntime
{
	/// <summary>
	/// Editor-scene equivalent of NoteSpawner.
	/// In Edit mode renders lightweight ghost dots (no Note.cs) so scrubbing works without breaking Note.Progress.
	/// In Test mode instantiates real Note prefabs, mirroring NoteSpawner.SpawnNote.
	/// </summary>
	public class EditorNoteSpawner : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private Note _notePrefab;
		[SerializeField] private Player _player;
		[SerializeField, Tooltip("Prefab used for Edit-mode ghost dots. A SpriteRenderer GameObject is enough.")]
		private SpriteRenderer _ghostPrefab;
		[SerializeField] private Transform _ghostsContainer;

		[Header("Settings")]
		[SerializeField] private float _noteTravelDuration = 0.5f;
		[SerializeField, Min(0f), Tooltip("How far before a note's time the ghost starts appearing (seconds).")]
		private float _ghostLookahead = 1f;
		[SerializeField, Min(0f), Tooltip("How long after a note's time the ghost lingers (seconds).")]
		private float _ghostLinger = 0.2f;

		public System.Action<Note> OnNoteSpawned;

		public float NoteTravelDuration
		{
			get { return _noteTravelDuration; }
			set { _noteTravelDuration = Mathf.Max(0.01f, value); }
		}

		// Test-mode trigger state
		private readonly List<float> _triggerTimes = new List<float>();
		private readonly List<float> _triggerAngles = new List<float>();
		private int _nextIndex;

		// Pool of ghost sprites for Edit mode
		private readonly List<SpriteRenderer> _ghostPool = new List<SpriteRenderer>();
		private int _ghostPoolUsed;

		public void ConfigureFromLevel(LevelData level)
		{
			if (level != null)
				_noteTravelDuration = level.NoteTravelDuration;
		}

		// ---- Edit mode ----

		public void RenderEditPreview(IReadOnlyList<CustomNote> notes, float playhead)
		{
			BeginGhostFrame();

			float lookEarly = _noteTravelDuration + _ghostLookahead;
			float lookLate = _ghostLinger;

			for (int i = 0; i < notes.Count; i++)
			{
				CustomNote note = notes[i];
				float spawnTime = note.Time - _noteTravelDuration;
				float endTime = note.Time + lookLate;

				if (playhead < note.Time - lookEarly || playhead > endTime)
					continue;

				float progress = (playhead - spawnTime) / _noteTravelDuration;
				progress = Mathf.Clamp(progress, 0f, 1f + lookLate / _noteTravelDuration);

				DrawGhost(note.Angle, progress);
			}

			EndGhostFrame();
		}

		public void ClearEditPreview()
		{
			BeginGhostFrame();
			EndGhostFrame();
		}

		private void BeginGhostFrame()
		{
			_ghostPoolUsed = 0;
		}

		private void EndGhostFrame()
		{
			for (int i = _ghostPoolUsed; i < _ghostPool.Count; i++)
			{
				if (_ghostPool[i] != null)
					_ghostPool[i].gameObject.SetActive(false);
			}
		}

		private void DrawGhost(float angleDegrees, float progress)
		{
			if (_ghostPrefab == null || _player == null)
				return;

			SpriteRenderer ghost = AcquireGhost();
			if (ghost == null)
				return;

			float angleRad = angleDegrees * Mathf.Deg2Rad;
			Vector2 direction = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)).normalized;

			ghost.transform.position = direction * (Mathf.Clamp01(progress) * _player.Radius);
			ghost.transform.up = direction;
			ghost.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(progress));

			Color c = ghost.color;
			c.a = Mathf.Clamp01(progress) * 0.7f;
			ghost.color = c;

			ghost.gameObject.SetActive(true);
		}

		private SpriteRenderer AcquireGhost()
		{
			if (_ghostPoolUsed < _ghostPool.Count)
			{
				SpriteRenderer existing = _ghostPool[_ghostPoolUsed++];
				if (existing == null)
					return null;
				return existing;
			}

			Transform parent = _ghostsContainer != null ? _ghostsContainer : transform;
			SpriteRenderer fresh = Instantiate(_ghostPrefab, Vector3.zero, Quaternion.identity, parent);
			_ghostPool.Add(fresh);
			_ghostPoolUsed++;
			return fresh;
		}

		// ---- Test mode ----

		public void BeginTest(IReadOnlyList<CustomNote> sortedNotes)
		{
			_triggerTimes.Clear();
			_triggerAngles.Clear();
			_triggerTimes.Capacity = sortedNotes.Count;
			_triggerAngles.Capacity = sortedNotes.Count;

			for (int i = 0; i < sortedNotes.Count; i++)
			{
				_triggerTimes.Add(sortedNotes[i].Time);
				_triggerAngles.Add(sortedNotes[i].Angle);
			}

			_nextIndex = 0;
			DestroyAllRealNotes();
		}

		public void EndTest()
		{
			DestroyAllRealNotes();
			_triggerTimes.Clear();
			_triggerAngles.Clear();
			_nextIndex = 0;
		}

		public void TickTest(float playhead)
		{
			_nextIndex = NoteScheduler.AdvancePending(
				_triggerTimes,
				_nextIndex,
				playhead,
				_noteTravelDuration,
				i => SpawnRealNote(_triggerAngles[i]));
		}

		public void RebaseTest(float playhead)
		{
			_nextIndex = NoteScheduler.RebaseIndex(_triggerTimes, playhead, _noteTravelDuration);
			DestroyAllRealNotes();
		}

		private void SpawnRealNote(float angle)
		{
			if (_notePrefab == null || _player == null)
				return;

			Note newNote = Instantiate(_notePrefab, Vector3.zero, Quaternion.identity);
			newNote.Init(_noteTravelDuration, _player.Radius, angle);
			OnNoteSpawned?.Invoke(newNote);
		}

		private static void DestroyAllRealNotes()
		{
			foreach (Note n in FindObjectsByType<Note>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
				Destroy(n.gameObject);
		}
	}
}
