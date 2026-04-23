using System.Collections.Generic;
using SpinSync.EditorRuntime;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private Note _notePrefab;
	[SerializeField] private Player _player;
	[SerializeField] private float _noteTravelDuration = 0.5f;

	public System.Action<Note> OnNoteSpawned;

	private readonly List<float> _triggerTimes = new List<float>();
	private readonly List<float> _triggerAngles = new List<float>();
	private int _nextIndex;

	private AudioSource _audioSource;

	/// <summary>
	/// Configure the spawner to play a Level (JSON). Playhead comes from <paramref name="audioSource"/>.time.
	/// Call before Start (e.g. from Gameplay.Awake).
	/// </summary>
	public void LoadLevel(Level level, AudioSource audioSource)
	{
		_triggerTimes.Clear();
		_triggerAngles.Clear();
		_nextIndex = 0;
		_audioSource = audioSource;

		if (level == null || level.Notes == null) return;

		List<CustomNote> sorted = new List<CustomNote>(level.Notes);
		sorted.Sort((a, b) => a.Time.CompareTo(b.Time));

		_triggerTimes.Capacity = sorted.Count;
		_triggerAngles.Capacity = sorted.Count;
		for (int i = 0; i < sorted.Count; i++)
		{
			_triggerTimes.Add(sorted[i].Time);
			_triggerAngles.Add(sorted[i].Angle);
		}
	}

	public void SetNoteTravelDuration(float seconds)
	{
		_noteTravelDuration = Mathf.Max(0.01f, seconds);
	}

	private void Update()
	{
		if (!Application.isPlaying || _audioSource == null) return;

		_nextIndex = NoteScheduler.AdvancePending(
			_triggerTimes,
			_nextIndex,
			_audioSource.time,
			_noteTravelDuration,
			i => SpawnNote(_triggerAngles[i]));
	}

	private void SpawnNote(float angle)
	{
		Note newNote = Instantiate(_notePrefab, Vector3.zero, Quaternion.identity);
		newNote.Init(_noteTravelDuration, _player.Radius, angle);
		OnNoteSpawned?.Invoke(newNote);
	}
}
