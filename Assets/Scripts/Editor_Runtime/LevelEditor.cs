using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SpinSync.EditorRuntime
{
	public enum EditorMode { Edit, Test }

	public enum BeatSubdivision { Quarter = 4, Eighth = 8, Sixteenth = 16 }

	/// <summary>
	/// Central brain of the LevelEditor scene. Single source of truth for playhead, mode, and the editing list.
	/// All UI components read state from here and call mutation methods that go through the command stack.
	/// </summary>
	public class LevelEditor : MonoBehaviour
	{
		private const string LatencyPrefsKey = "SpinSync.EditorLatencyMs";
		private const string IntroSceneName = "Intro";
		private const string GameplaySceneName = "Gameplay";

		[Header("References")]
		[SerializeField] private AudioSource _audioSource;
		[SerializeField] private Player _player;
		[SerializeField] private EditorNoteSpawner _spawner;
		[SerializeField] private EditorTestSession _testSession;

		[Header("Defaults")]
		[SerializeField, Tooltip("Default input-latency offset (ms) used the first time the editor is opened. PlayerPrefs override after that.")]
		private float _defaultLatencyMs = 50f;

		// ---- State ----

		private Level _editingLevel;
		private EditorMode _mode = EditorMode.Edit;
		private bool _isPlaying;
		private bool _isDirty;
		private float _playheadTime;
		private int? _selectedNoteIndex;
		private float _inputLatencySeconds;

		private bool _bpmSnapEnabled;
		private BeatSubdivision _snapSubdivision = BeatSubdivision.Eighth;

		private InputSystem_Actions _inputActions;
		private readonly EditorCommandStack _commands = new EditorCommandStack();

		// ---- Events for UI ----

		public event Action OnLevelLoaded;
		public event Action OnNotesChanged;
		public event Action OnSelectionChanged;
		public event Action OnPlaybackStateChanged;
		public event Action OnDirtyChanged;
		public event Action OnModeChanged;

		// ---- Public read-only state ----

		public Level SelectedSong { get { return _editingLevel; } }
		public Level EditingLevel { get { return _editingLevel; } }
		public IReadOnlyList<CustomNote> Notes
		{
			get { return _editingLevel != null ? (IReadOnlyList<CustomNote>)_editingLevel.Notes : Array.Empty<CustomNote>(); }
		}
		public EditorMode Mode { get { return _mode; } }
		public bool IsPlaying { get { return _isPlaying; } }
		public bool IsDirty { get { return _isDirty; } }
		public float PlayheadTime { get { return _playheadTime; } }
		public int? SelectedNoteIndex { get { return _selectedNoteIndex; } }
		public float SongDuration
		{
			get { return (_audioSource != null && _audioSource.clip != null) ? _audioSource.clip.length : 0f; }
		}
		public float InputLatencySeconds
		{
			get { return _inputLatencySeconds; }
			set
			{
				_inputLatencySeconds = Mathf.Max(0f, value);
				PlayerPrefs.SetFloat(LatencyPrefsKey, _inputLatencySeconds * 1000f);
			}
		}
		public bool BpmSnapEnabled
		{
			get { return _bpmSnapEnabled; }
			set { _bpmSnapEnabled = value; }
		}
		public BeatSubdivision SnapSubdivision
		{
			get { return _snapSubdivision; }
			set { _snapSubdivision = value; }
		}
		public bool CanUndo { get { return _commands.CanUndo; } }
		public bool CanRedo { get { return _commands.CanRedo; } }

		// ---- Lifecycle ----

		private void Awake()
		{
			_inputActions = new InputSystem_Actions();

			float prefMs = PlayerPrefs.GetFloat(LatencyPrefsKey, _defaultLatencyMs);
			_inputLatencySeconds = Mathf.Max(0f, prefMs / 1000f);
		}

		private void OnEnable()
		{
			_inputActions?.Enable();
		}

		private void OnDisable()
		{
			_inputActions?.Disable();
		}

		private void Update()
		{
			if (_mode == EditorMode.Edit)
			{
				UpdateEditMode();
			}
			else
			{
				UpdateTestMode();
			}

			HandleKeyboardShortcuts();
		}

		private void UpdateEditMode()
		{
			if (_isPlaying && _audioSource != null && _audioSource.isPlaying)
				_playheadTime = _audioSource.time;

			if (_isPlaying && _inputActions.Player.Press.WasPressedThisFrame())
				PlaceNoteAtCurrentState();

			if (_spawner != null && _editingLevel != null)
				_spawner.RenderEditPreview(_editingLevel.Notes, _playheadTime);
		}

		private void UpdateTestMode()
		{
			if (_isPlaying && _audioSource != null && _audioSource.isPlaying)
				_playheadTime = _audioSource.time;

			if (_spawner != null)
				_spawner.TickTest(_playheadTime);
		}

		private void HandleKeyboardShortcuts()
		{
			Keyboard kb = Keyboard.current;
			if (kb == null) return;

			if (_mode == EditorMode.Test && kb.escapeKey.wasPressedThisFrame)
			{
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				return;
			}

			bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;

			if (ctrl && kb.zKey.wasPressedThisFrame)
				Undo();
			else if (ctrl && kb.yKey.wasPressedThisFrame)
				Redo();
			else if (ctrl && kb.sKey.wasPressedThisFrame)
				SaveCurrent();
			else if (kb.deleteKey.wasPressedThisFrame && _selectedNoteIndex.HasValue && _mode == EditorMode.Edit)
				DeleteSelectedNote();
		}

		// ---- Song loading ----

		/// <summary>
		/// Load a level by SongId. If the level exists in storage it's loaded; otherwise a new empty
		/// level is created with that SongId. Audio is loaded asynchronously.
		/// </summary>
		public void LoadSong(string songId)
		{
			Level level = LevelStorage.Load(songId);
			if (level == null)
			{
				Debug.LogWarning($"[LevelEditor] No level '{songId}' in storage; creating empty draft.");
				level = new Level { SongId = songId, Title = songId };
			}
			LoadLevel(level);
		}

		public void LoadLevel(Level level)
		{
			_editingLevel = level;
			_editingLevel.Notes.Sort((a, b) => a.Time.CompareTo(b.Time));

			if (_audioSource != null)
			{
				_audioSource.Stop();
				_audioSource.clip = null;
				_audioSource.time = 0f;
			}

			StartCoroutine(LoadClipAndConfigure());

			_isPlaying = false;
			_playheadTime = 0f;
			_selectedNoteIndex = null;
			_isDirty = false;
			_commands.Clear();

			OnLevelLoaded?.Invoke();
			OnNotesChanged?.Invoke();
			OnSelectionChanged?.Invoke();
			OnPlaybackStateChanged?.Invoke();
			OnDirtyChanged?.Invoke();
		}

		private IEnumerator LoadClipAndConfigure()
		{
			string id = _editingLevel.SongId;
			yield return LevelAudioLoader.LoadInto(_editingLevel);

			if (_editingLevel == null || _editingLevel.SongId != id) yield break; // level changed mid-load

			if (_audioSource != null)
				_audioSource.clip = _editingLevel.AudioClip;

			if (_spawner != null)
				_spawner.ConfigureFromLevel(_editingLevel);

			OnLevelLoaded?.Invoke();
		}

		// ---- Playback ----

		public void Play()
		{
			if (_audioSource == null || _audioSource.clip == null)
				return;

			_audioSource.time = Mathf.Clamp(_playheadTime, 0f, Mathf.Max(0f, SongDuration - 0.01f));
			_audioSource.Play();
			_isPlaying = true;
			OnPlaybackStateChanged?.Invoke();
		}

		public void Pause()
		{
			if (_audioSource != null && _audioSource.isPlaying)
				_audioSource.Pause();
			_isPlaying = false;
			OnPlaybackStateChanged?.Invoke();
		}

		public void Restart()
		{
			SetPlayhead(0f);
			if (_isPlaying)
				Play();
		}

		public void SetPlayhead(float time)
		{
			_playheadTime = Mathf.Clamp(time, 0f, SongDuration);

			if (_audioSource != null)
			{
				bool wasPlaying = _audioSource.isPlaying;
				_audioSource.time = Mathf.Min(_playheadTime, Mathf.Max(0f, SongDuration - 0.01f));
				if (!wasPlaying && _isPlaying)
					_audioSource.Play();
			}

			if (_mode == EditorMode.Test && _spawner != null)
				_spawner.RebaseTest(_playheadTime);
		}

		// ---- Mutation API (all go through command stack) ----

		public void PlaceNoteAtCurrentState()
		{
			if (_editingLevel == null || _mode != EditorMode.Edit)
				return;

			float time = _playheadTime - _inputLatencySeconds;
			time = Mathf.Max(0f, MaybeSnap(time));
			float angle = ReadPlatformAngleAsNoteAngle();

			CustomNote note = new CustomNote { Time = time, Angle = angle };
			PlaceNoteCommand cmd = new PlaceNoteCommand(_editingLevel.Notes, note);
			_commands.Execute(cmd);
			_selectedNoteIndex = cmd.InsertedIndex;
			MarkDirty();
			OnNotesChanged?.Invoke();
			OnSelectionChanged?.Invoke();
		}

		public void DeleteSelectedNote()
		{
			if (_editingLevel == null || !_selectedNoteIndex.HasValue)
				return;

			int idx = _selectedNoteIndex.Value;
			if (idx < 0 || idx >= _editingLevel.Notes.Count)
				return;

			_commands.Execute(new DeleteNoteCommand(_editingLevel.Notes, idx));
			_selectedNoteIndex = null;
			MarkDirty();
			OnNotesChanged?.Invoke();
			OnSelectionChanged?.Invoke();
		}

		public void MoveSelectedNote(float newTime, float newAngle)
		{
			if (_editingLevel == null || !_selectedNoteIndex.HasValue)
				return;

			int idx = _selectedNoteIndex.Value;
			if (idx < 0 || idx >= _editingLevel.Notes.Count)
				return;

			float time = Mathf.Clamp(MaybeSnap(newTime), 0f, SongDuration);
			float angle = WrapAngle(newAngle);

			MoveNoteCommand cmd = new MoveNoteCommand(_editingLevel.Notes, idx, time, angle);
			_commands.Execute(cmd);
			_selectedNoteIndex = cmd.NewIndex;
			MarkDirty();
			OnNotesChanged?.Invoke();
			OnSelectionChanged?.Invoke();
		}

		public void SetSelectedAngleFromPlatform()
		{
			if (_editingLevel == null || !_selectedNoteIndex.HasValue)
				return;
			CustomNote n = _editingLevel.Notes[_selectedNoteIndex.Value];
			MoveSelectedNote(n.Time, ReadPlatformAngleAsNoteAngle());
		}

		public void SelectNote(int? index)
		{
			_selectedNoteIndex = index;
			OnSelectionChanged?.Invoke();
		}

		public void Undo()
		{
			if (_commands.Undo() != null)
			{
				_selectedNoteIndex = null;
				MarkDirty();
				OnNotesChanged?.Invoke();
				OnSelectionChanged?.Invoke();
			}
		}

		public void Redo()
		{
			if (_commands.Redo() != null)
			{
				_selectedNoteIndex = null;
				MarkDirty();
				OnNotesChanged?.Invoke();
				OnSelectionChanged?.Invoke();
			}
		}

		// ---- Save/Load ----

		public void SaveCurrent()
		{
			if (_editingLevel == null)
				return;

			LevelStorage.Save(_editingLevel);
			_isDirty = false;
			OnDirtyChanged?.Invoke();
			Debug.Log($"[LevelEditor] Saved {_editingLevel.SongId} ({_editingLevel.Notes.Count} notes) to {_editingLevel.FolderPath}");
		}

		// ---- Mode switching ----

		public void EnterTestMode()
		{
			if (_mode == EditorMode.Test || _editingLevel == null)
				return;

			_mode = EditorMode.Test;

			Pause();
			SetPlayhead(0f);

			if (_player != null)
				_player.EnableHitDetection = true;

			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;

			if (_spawner != null)
			{
				_spawner.ClearEditPreview();
				_spawner.BeginTest(_editingLevel.Notes);
			}

			if (_testSession != null)
			{
				_testSession.gameObject.SetActive(true);
				_testSession.ResetSession();
			}

			_selectedNoteIndex = null;
			OnSelectionChanged?.Invoke();
			OnModeChanged?.Invoke();

			Play();
		}

		public void ExitTestMode()
		{
			if (_mode == EditorMode.Edit)
				return;

			Pause();
			SetPlayhead(0f);

			if (_player != null)
				_player.EnableHitDetection = false;

			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;

			if (_spawner != null)
				_spawner.EndTest();

			if (_testSession != null)
				_testSession.gameObject.SetActive(false);

			_mode = EditorMode.Edit;
			OnModeChanged?.Invoke();
		}

		public void BackToMenu()
		{
			SceneManager.LoadScene(IntroSceneName);
		}

		public void PlayCustomInGameplay()
		{
			if (_editingLevel == null)
			{
				Debug.LogWarning("[LevelEditor] Cannot Play: no level loaded.");
				return;
			}

			SaveCurrent();

			SongSelection.Current = _editingLevel;

			SceneManager.LoadScene(GameplaySceneName);
		}

		// ---- Helpers ----

		private void MarkDirty()
		{
			if (!_isDirty)
			{
				_isDirty = true;
				OnDirtyChanged?.Invoke();
			}
		}

		private float ReadPlatformAngleAsNoteAngle()
		{
			if (_player == null || _player.PivotTransform == null)
				return 0f;

			float pivotZ = Mathf.Repeat(_player.PivotTransform.eulerAngles.z, 360f);
			return Mathf.Repeat(360f - pivotZ, 360f);
		}

		private float MaybeSnap(float time)
		{
			if (!_bpmSnapEnabled || _editingLevel == null || _editingLevel.BPM <= 0f)
				return time;

			int subsPerBeat;
			switch (_snapSubdivision)
			{
				case BeatSubdivision.Sixteenth: subsPerBeat = 4; break;
				case BeatSubdivision.Eighth: subsPerBeat = 2; break;
				default: subsPerBeat = 1; break;
			}

			float secondsPerBeat = 60f / _editingLevel.BPM;
			float subInterval = secondsPerBeat / subsPerBeat;
			return Mathf.Round(time / subInterval) * subInterval;
		}

		private static float WrapAngle(float a)
		{
			return Mathf.Repeat(a, 360f);
		}
	}
}
