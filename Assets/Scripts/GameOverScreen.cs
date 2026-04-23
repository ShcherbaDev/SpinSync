using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
	[Header("Scene References")]
	[SerializeField] private CanvasGroup _canvasGroup;
	[SerializeField] private RectTransform _canvasRoot;
	[SerializeField] private TextMeshProUGUI _titleText;
	[SerializeField] private Button _buttonPrefab;

	[Header("Layout")]
	[SerializeField] private float _buttonsTopOffsetY = -60f;
	[SerializeField] private float _buttonSpacing = 24f;

	[Header("Animation")]
	[SerializeField, Min(0f)] private float _fadeInDuration = 0.5f;
	[SerializeField, Min(0f)] private float _buttonAppearDuration = 0.45f;
	[SerializeField, Min(0f)] private float _buttonAppearStagger = 0.12f;
	[SerializeField] private float _buttonStartScale = 0.6f;

	[Header("Scene Transition")]
	[SerializeField] private ScreenFader _faderPrefab;
	[SerializeField, Min(0.1f)] private float _fadeDuration = 0.8f;
	[SerializeField] private string _gameplaySceneName = "Gameplay";
	[SerializeField] private string _menuSceneName = "Intro";

	[Header("Text")]
	[SerializeField] private string _titleLabel = "Game Over";
	[SerializeField] private string _tryAgainLabel = "Try Again";
	[SerializeField] private string _goToMenuLabel = "Go to Menu";

	[Header("Audio")]
	[SerializeField] private AudioSource _audioSource;
	[SerializeField] private AudioClip _gameOverMusic;
	[SerializeField, Range(0f, 1f)] private float _gameOverMusicVolume = 0.8f;
	[SerializeField] private bool _gameOverMusicLoops = true;

	private ScreenFader _screenFader;
	private bool _shown;

	private void Awake()
	{
		if (_canvasGroup)
		{
			_canvasGroup.alpha = 0f;
			_canvasGroup.blocksRaycasts = false;
			_canvasGroup.interactable = false;
		}

		if (_titleText)
			_titleText.text = _titleLabel;

		if (_faderPrefab)
			_screenFader = Instantiate(_faderPrefab);
	}

	public void Show()
	{
		if (_shown) return;
		_shown = true;

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		Time.timeScale = 0f;

		if (_canvasGroup)
		{
			_canvasGroup.blocksRaycasts = true;
			_canvasGroup.interactable = true;
			_canvasGroup.DOFade(1f, _fadeInDuration).SetUpdate(true);
		}

		PlayGameOverMusic();
		SpawnMenuButtons();
	}

	private void PlayGameOverMusic()
	{
		if (!_gameOverMusic) return;

		if (!_audioSource)
			_audioSource = gameObject.AddComponent<AudioSource>();

		_audioSource.clip = _gameOverMusic;
		_audioSource.volume = _gameOverMusicVolume;
		_audioSource.loop = _gameOverMusicLoops;
		_audioSource.playOnAwake = false;
		_audioSource.ignoreListenerPause = true;
		_audioSource.Play();
	}

	private void SpawnMenuButtons()
	{
		if (!_buttonPrefab || !_canvasRoot) return;

		float buttonHeight = ((RectTransform)_buttonPrefab.transform).sizeDelta.y;
		float step = buttonHeight + _buttonSpacing;

		Button tryAgain = SpawnButton(_tryAgainLabel, _buttonsTopOffsetY, ReloadGameplay);
		Button goToMenu = SpawnButton(_goToMenuLabel, _buttonsTopOffsetY - step, GoToMenu);

		AnimateButtonAppear((RectTransform)tryAgain.transform, 0f);
		AnimateButtonAppear((RectTransform)goToMenu.transform, _buttonAppearStagger);
	}

	private Button SpawnButton(string label, float yPosition, UnityEngine.Events.UnityAction onClick)
	{
		Button button = Instantiate(_buttonPrefab, _canvasRoot);
		button.name = label.Replace(" ", "") + "Button";

		RectTransform rect = (RectTransform)button.transform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = new Vector2(0f, yPosition);

		TMP_Text text = button.GetComponentInChildren<TMP_Text>(includeInactive: true);
		if (text) text.text = label;

		button.onClick.AddListener(onClick);
		return button;
	}

	private void AnimateButtonAppear(RectTransform rect, float delay)
	{
		CanvasGroup cg = rect.GetComponent<CanvasGroup>();
		if (!cg) cg = rect.gameObject.AddComponent<CanvasGroup>();

		cg.alpha = 0f;
		rect.localScale = Vector3.one * _buttonStartScale;

		cg.DOFade(1f, _buttonAppearDuration).SetDelay(delay).SetUpdate(true);
		rect.DOScale(1f, _buttonAppearDuration).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
	}

	private void ReloadGameplay()
	{
		LoadScene(_gameplaySceneName);
	}

	private void GoToMenu()
	{
		LoadScene(_menuSceneName);
	}

	private void LoadScene(string sceneName)
	{
		if (_screenFader)
			_screenFader.FadeToBlack(_fadeDuration).SetUpdate(true);

		if (_audioSource && _audioSource.isPlaying)
			_audioSource.DOFade(0f, _fadeDuration).SetUpdate(true);

		DOVirtual.DelayedCall(_fadeDuration, () =>
		{
			Time.timeScale = 1f;
			SceneManager.LoadScene(sceneName);
		}, ignoreTimeScale: true);
	}
}
