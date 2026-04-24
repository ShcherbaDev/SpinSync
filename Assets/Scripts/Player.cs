using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
	[Header("References")]
	[SerializeField, Tooltip("An actual object that rotates (located in a center of the screen)")]
	private Transform _pivotTransform;

	[SerializeField, Tooltip("The platform that catches notes (must be a child of the pivot)")]
	private Transform _platformTransform;

	[SerializeField]
	private SpriteRenderer _spriteRenderer;

	[Header("Platform Settings")]
	[SerializeField, Min(0.5f), Tooltip("Distance from the pivot center")]
	private float _radius = 3.5f;

	[SerializeField, Tooltip("Size of the platform (X - width, Y - height)")]
	private Vector2 _scale = new Vector2(1f, 1f);

	[Header("Input Settings")]
	[SerializeField, Range(0.01f, 0.5f)]
	private float _sensitivity = 0.2f;

	[SerializeField]
	private bool _isInverted = true;

	[Header("Behavior")]
	[SerializeField, Tooltip("Disable to use this Player for rotation-only (e.g. Level Editor). When false, key presses do not trigger note hit detection.")]
	private bool _enableHitDetection = true;

	[SerializeField, Tooltip("Disable in scenes that need a visible cursor for UI interaction (e.g. Level Editor).")]
	private bool _lockCursor = true;

	private InputSystem_Actions _inputActions;

	/// <summary>
	/// Used in IsPlatformAlignedWithNote method
	/// </summary>
	private float _cosThreshold;

	private int _pendingPresses;

	public System.Action<Note, NoteGrade> OnHitDetected;

	public float Radius
	{
		get { return _radius; }
	}

	public Transform PivotTransform
	{
		get { return _pivotTransform; }
	}

	public bool EnableHitDetection
	{
		get { return _enableHitDetection; }
		set { _enableHitDetection = value; }
	}

	private void CalculateHalfAngularWidth()
	{
		float horizontalSize = _spriteRenderer.bounds.size.x * _scale.x;
		float halfAngle = (horizontalSize / _radius) * 0.5f;
		_cosThreshold = Mathf.Cos(halfAngle);
	}

	private void RotateByUserInput()
	{
		int rotationInvertedMultiplier = _isInverted ? -1 : 1;
		float finalRotationDelta = _inputActions.Player.Move.ReadValue<Vector2>().x * _sensitivity * rotationInvertedMultiplier;
		_pivotTransform.Rotate(0f, 0f, finalRotationDelta);
	}

	private void OnPressPerformed(InputAction.CallbackContext context)
	{
		_pendingPresses++;
	}

	private void HandleNotePress()
	{
		Note[] activeNotes = FindObjectsByType<Note>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
		Note closestNote = null;
		float closestProgressDiff = float.MaxValue;

		// Find a note that is the closest to the Player
		foreach (Note note in activeNotes)
		{
			if (!IsPlatformAlignedWithNote(note))
				continue;

			float diff = Mathf.Abs(1f - note.Progress);
			if (diff < closestProgressDiff)
			{
				closestProgressDiff = diff;
				closestNote = note;
			}
		}

		if (!closestNote)
			return;

		NoteGrade grade = closestNote.RateHit();
		Debug.Log(grade);
		if (grade != NoteGrade.Miss)
		{
			OnHitDetected?.Invoke(closestNote, grade);
			Destroy(closestNote.gameObject);
		}
	}

	private bool IsPlatformAlignedWithNote(Note note)
	{
		Vector2 playerDirection = (_platformTransform.position - _pivotTransform.position).normalized;
		Vector2 noteDirection = (note.transform.position - _pivotTransform.position).normalized;

		float dot = Vector2.Dot(noteDirection, playerDirection);
		return dot >= _cosThreshold;
	}

	private void OnValidate()
	{
		if (_platformTransform != null)
		{
			// Move away from center (pivot)
			_platformTransform.localPosition = new Vector3(0f, _radius, 0f);
			_platformTransform.localScale = new Vector3(_scale.x, _scale.y, 1f);
		}
	}

	private void Awake()
	{
		if (!_pivotTransform)
		{
			Debug.LogWarning("_pivotTransform is null. Falling back to current GameObject's Transform");
			_pivotTransform = transform;
		}

		_inputActions = new InputSystem_Actions();
	}

	private void OnEnable()
	{
		_inputActions?.Enable();
		if (_inputActions != null)
			_inputActions.Player.Press.performed += OnPressPerformed;
	}

	private void OnDisable()
	{
		if (_inputActions != null)
			_inputActions.Player.Press.performed -= OnPressPerformed;
		_inputActions?.Disable();
		_pendingPresses = 0;
	}

	private void Start()
	{
		if (_lockCursor)
		{
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
		}

		CalculateHalfAngularWidth();
	}

	private void Update()
	{
		RotateByUserInput();

		if (_enableHitDetection)
		{
			while (_pendingPresses > 0)
			{
				_pendingPresses--;
				HandleNotePress();
			}
		}
		else
		{
			_pendingPresses = 0;
		}
	}
}
