using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gameplay : MonoBehaviour
{
	[SerializeField] private Player _player;
	[SerializeField] private DebugNoteSpawner _spawner;
	[SerializeField] private AudioSource _audioSource;

	[Header("UI")]
	[SerializeField] private TextMeshProUGUI _scoreText;
	[SerializeField] private TextMeshProUGUI _comboText;

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
	[SerializeField] private int _combo;

	private void Start()
	{
		_currentLives = _numberOfLives;
		UpdateHealthBar();
	}

	private void OnEnable()
	{
		_player.OnHitDetected += ProcessNoteResult;
		_spawner.OnNoteSpawned += SubscribeToNote;
	}

	private void OnDisable()
	{
		_player.OnHitDetected -= ProcessNoteResult;
		_spawner.OnNoteSpawned -= SubscribeToNote;
	}

	private void SubscribeToNote(Note note)
	{
		note.OnNoteFinished += ProcessNoteResult;
	}

	private void ProcessNoteResult(Note note, NoteGrade grade)
	{
		note.OnNoteFinished -= ProcessNoteResult;

		if (grade == NoteGrade.Miss)
		{
			_currentLives--;
			_combo = 0;
			_audioSource.PlayOneShot(_missSound);
			UpdateHealthBar();
		}
		else
		{
			_combo++;
			_audioSource.PlayOneShot(_hitSound);
		}
		
		UpdateComboText();

		if (_currentLives <= 0)
		{
			Debug.Log("Game Over");
			Application.Quit();
		}
	}

	private void UpdateComboText()
	{
		_comboText.text = $"{_combo}x";
	}

	private void UpdateHealthBar()
	{
		for (int i = _healthBarContainer.childCount - 1; i >= 0; i--)
		{
			Destroy(_healthBarContainer.GetChild(i).gameObject);
		}

		for (int i = 0; i < _numberOfLives; i++)
		{
			GameObject lifeItem = new GameObject();
			Image lifeImage = lifeItem.AddComponent<Image>();
			lifeImage.sprite = _currentLives > i ? _activeLifeSprite : _lostLifeSprite;
			Instantiate(lifeItem, _healthBarContainer);
		}
	}
}
