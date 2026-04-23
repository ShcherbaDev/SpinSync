using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[System.Serializable]
public class NoteGradeToScoreMapping
{
	public NoteGrade Grade;
	public int Score;
}

public class Gameplay : MonoBehaviour
{
	[SerializeField] private PlayableDirector _director;

	[SerializeField] private Player _player;
	[SerializeField] private NoteSpawner _noteSpawner;
	[SerializeField] private AudioSource _sfxAudioSource;
	[SerializeField] private GameplayFeedback _feedback;
	[SerializeField] private GameOverScreen _gameOverScreen;

	[Header("UI Texts")]
	[SerializeField] private TextMeshProUGUI _scoreText;
	[SerializeField, Min(1)] private int _maxAmountOfScoreDigits = 7;
	[SerializeField] private TextMeshProUGUI _comboText;

	[Header("Progress")]
	[SerializeField, Tooltip("Image with type=Filled, Fill Method=Radial 360")]
	private Image _progressFillImage;
	[SerializeField] private TextMeshProUGUI _progressText;

	[Header("Health (UI)")]
	[SerializeField] private RectTransform _healthBarContainer;
	[SerializeField] private Color _healthBarItemsColor;
	[SerializeField] private Sprite _activeLifeSprite;
	[SerializeField] private Sprite _lostLifeSprite;

	[Header("Health")]
	[SerializeField, Min(0)] private int _numberOfLives = 3;
	[SerializeField, Min(0)] private int _currentLives;

	[Header("Sounds")]
	[SerializeField] private AudioClip _hitSound;
	[SerializeField] private AudioClip _missSound;

	[Header("Combo")]
	[SerializeField] private int _currentCombo;
	[SerializeField] private Color _currentComboColor;

	[Header("Scoring")]
	[SerializeField] private List<NoteGradeToScoreMapping> _gradeToScoreMapping;
	[SerializeField] private int _currentScore;

	private void Awake()
	{
		// Must run before NoteSpawner.Start, which caches markers from _director.playableAsset.
		if (SongSelection.Current != null && SongSelection.Current.Timeline != null)
			_director.playableAsset = SongSelection.Current.Timeline;
	}

	private void Start()
	{
		_currentLives = _numberOfLives;
		UpdateHealthBar();

		_currentComboColor = RandomComboColor();

		if (SongSelection.Current != null)
			Debug.Log($"Gameplay starting with selected song: {SongSelection.Current.Title} by {SongSelection.Current.Artist}");

		_director.Play();
	}

	private void OnEnable()
	{
		_player.OnHitDetected += ProcessNoteResult;
		_noteSpawner.OnNoteSpawned += SubscribeToNote;
	}

	private void OnDisable()
	{
		_player.OnHitDetected -= ProcessNoteResult;
		_noteSpawner.OnNoteSpawned -= SubscribeToNote;
	}

	private void SubscribeToNote(Note note)
	{
		note.OnNoteFinished += ProcessNoteResult;
		note.SetComboVisuals(_currentComboColor);
	}

	private void ProcessNoteResult(Note note, NoteGrade grade)
	{
		note.OnNoteFinished -= ProcessNoteResult;

		// Captured before any logic that may destroy the note later in the frame.
		Vector3 notePosition = note.transform.position;

		if (grade == NoteGrade.Miss)
		{
			_currentLives--;
			_currentCombo = 0;
			_currentComboColor = RandomComboColor();
			_sfxAudioSource.PlayOneShot(_missSound);
			UpdateHealthBar();

			if (_feedback)
			{
				_feedback.PlayMiss(notePosition);
				_feedback.OnComboReset();
				_feedback.OnLifeLost();
			}
		}
		else
		{
			_currentCombo++;
			_sfxAudioSource.PlayOneShot(_hitSound);

			if (_feedback)
			{
				_feedback.PlayHit(notePosition, grade, _currentComboColor);
				_feedback.OnComboIncreased(_currentComboColor);
			}
		}

		_currentScore += _gradeToScoreMapping.Find(item => item.Grade == grade).Score;
		UpdateScoreText();
		UpdateComboText();

		if (_feedback)
			_feedback.OnScoreChanged();

		if (_currentLives <= 0)
		{
			Debug.Log("Game Over");
			TriggerGameOver();
		}
	}

	private void Update()
	{
		UpdateProgress();
	}

	private void UpdateProgress()
	{
		if (!_director || _director.duration <= 0) return;

		float progress = Mathf.Clamp01((float)(_director.time / _director.duration));

		if (_progressFillImage)
			_progressFillImage.fillAmount = progress;

		if (_progressText)
			_progressText.text = $"{Mathf.FloorToInt(progress * 100f)}%";
	}

	private void TriggerGameOver()
	{
		if (_director)
			_director.Pause();

		if (_player)
			_player.enabled = false;

		foreach (Note note in FindObjectsByType<Note>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			Destroy(note.gameObject);

		if (_gameOverScreen)
			_gameOverScreen.Show();
	}

	private void UpdateScoreText()
	{
		_scoreText.text = _currentScore.ToString().PadLeft(_maxAmountOfScoreDigits, '0');
	}

	private void UpdateComboText()
	{
		_comboText.text = $"{_currentCombo}x";
	}

	private static Color RandomComboColor()
	{
		return Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
	}

	private void UpdateHealthBar()
	{
		for (int i = _healthBarContainer.childCount - 1; i >= 0; i--)
		{
			Destroy(_healthBarContainer.GetChild(i).gameObject);
		}

		for (int i = 0; i < _numberOfLives; i++)
		{
			GameObject lifeItem = new GameObject("LifeItem");
			lifeItem.transform.SetParent(_healthBarContainer, false);

			Image lifeImage = lifeItem.AddComponent<Image>();
			lifeImage.sprite = _currentLives > i ? _activeLifeSprite : _lostLifeSprite;
			lifeImage.color = _healthBarItemsColor;
		}
	}
}
