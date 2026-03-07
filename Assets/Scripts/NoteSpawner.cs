using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private Note _notePrefab;
	[SerializeField] private Player _player;

	public System.Action<Note> OnNoteSpawned;

	private LevelData _currentLevel;
	private AudioSource _audioSource;

	private int _nextNoteIndex;
	private bool _isLevelRunning;

	public void StartLevel(LevelData levelData, AudioSource audioSource)
	{
		_currentLevel = levelData;
		_audioSource = audioSource;

		if (!_currentLevel || !_currentLevel.Song)
		{
			Debug.LogError("No song set");
			return;
		}

		_nextNoteIndex = 0;
		_isLevelRunning = true;
	}

	private void Update()
	{
		if (!_isLevelRunning)
			return;

		float currentTime = _audioSource.time;

		// Check whether it's time to spawn a next Note
		while (_nextNoteIndex < _currentLevel.Notes.Count)
		{
			NoteData data = _currentLevel.Notes[_nextNoteIndex];

			float spawnTriggerTime = data.HitTimeSeconds - _currentLevel.NoteTravelDuration;

			if (currentTime >= spawnTriggerTime)
			{
				SpawnNote(data);
				_nextNoteIndex++;
			}
			else
			{
				break;
			}
		}
	}

	private void SpawnNote(NoteData note)
	{
		Note newNote = Instantiate(_notePrefab, Vector3.zero, Quaternion.identity);
		newNote.Init(_currentLevel.NoteTravelDuration, _player.Radius, note.Angle);
		OnNoteSpawned?.Invoke(newNote);
	}
}
