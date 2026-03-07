using UnityEngine;
using UnityEngine.InputSystem;

public class DebugNoteSpawner : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private Note _notePrefab;

	[SerializeField, Tooltip("A point where spawning the Notes")]
	private Transform _centerPoint;

	[Header("Spawn Settings")]
	[SerializeField, Min(0.1f)]
	private float _targetRadius = 5f; // Must be the same as Player's platform radius

	[SerializeField, Min(0.1f), Tooltip("Duration in seconds to move from start to Player platform")]
	private float _travelDuration = 2f;

	[Header("Angle Settings")]
	[SerializeField]
	private bool _randomAngle = true;

	[SerializeField, Range(0f, 360f)]
	private float _specificAngle;

	public System.Action<Note> OnNoteSpawned;

	private void SpawnNote()
	{
		if (!_notePrefab)
		{
			Debug.LogError("Note prefab is not assigned in NoteSpawner");
			return;
		}

		float angle = _randomAngle ? Random.Range(0f, 360f) : _specificAngle;
		Vector3 spawnPos = _centerPoint != null ? _centerPoint.position : transform.position;

		Note spawnedNote = Instantiate(_notePrefab, spawnPos, Quaternion.identity);
		spawnedNote.Init(_travelDuration, _targetRadius, angle);

		OnNoteSpawned?.Invoke(spawnedNote);
	}

	private void Start()
	{
		// Set the same radius as Player's platform radius
		_targetRadius = FindAnyObjectByType<Player>().Radius;
	}

	private void Update()
	{
		if (Keyboard.current.digit1Key.wasPressedThisFrame)
			SpawnNote();
	}
}
