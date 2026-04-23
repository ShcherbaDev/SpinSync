using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
	[Header("Game Refs")]
	[SerializeField] private Gameplay _gameplay;

	[Header("Scene References")]
	[SerializeField] private CanvasGroup _canvasGroup;
	[SerializeField] private RectTransform _canvasRoot;
	[SerializeField] private TextMeshProUGUI _titleText;
	[SerializeField] private Button _buttonPrefab;

	[Header("Layout")]
	[SerializeField] private float _buttonsTopOffsetY = -60f;
	[SerializeField] private float _buttonSpacing = 24f;

	[Header("Animation")]
	[SerializeField, Min(0f)] private float _fadeInDuration = 0.3f;
	[SerializeField, Min(0f)] private float _fadeOutDuration = 0.2f;
	[SerializeField, Min(0f)] private float _buttonAppearDuration = 0.35f;
	[SerializeField, Min(0f)] private float _buttonAppearStagger = 0.08f;
	[SerializeField] private float _buttonStartScale = 0.6f;

	[Header("Scene Transition")]
	[SerializeField] private ScreenFader _faderPrefab;
	[SerializeField, Min(0.1f)] private float _fadeDuration = 0.8f;
	[SerializeField] private string _gameplaySceneName = "Gameplay";
	[SerializeField] private string _menuSceneName = "Intro";

	[Header("Text")]
	[SerializeField] private string _titleLabel = "Paused";
	[SerializeField] private string _resumeLabel = "Resume";
	[SerializeField] private string _restartLabel = "Restart";
	[SerializeField] private string _goToMenuLabel = "Go to Menu";

	private ScreenFader _screenFader;
	private bool _isPaused;
	private bool _transitioning;
	private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

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

	private void Update()
	{
		if (_transitioning) return;
		if (Keyboard.current == null) return;

		if (Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			if (_isPaused) Resume();
			else Pause();
		}
	}

	public void Pause()
	{
		if (_isPaused || _transitioning) return;
		_isPaused = true;

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		Time.timeScale = 0f;

		if (_gameplay)
			_gameplay.PauseGameplay();

		if (_canvasGroup)
		{
			_canvasGroup.blocksRaycasts = true;
			_canvasGroup.interactable = true;
			_canvasGroup.DOFade(1f, _fadeInDuration).SetUpdate(true);
		}

		SpawnMenuButtons();
	}

	public void Resume()
	{
		if (!_isPaused || _transitioning) return;
		_isPaused = false;

		if (_canvasGroup)
		{
			_canvasGroup.blocksRaycasts = false;
			_canvasGroup.interactable = false;
			_canvasGroup.DOFade(0f, _fadeOutDuration).SetUpdate(true);
		}

		DestroyButtons();

		Time.timeScale = 1f;

		if (_gameplay)
			_gameplay.ResumeGameplay();

		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	private void SpawnMenuButtons()
	{
		if (!_buttonPrefab || !_canvasRoot) return;

		DestroyButtons();

		float buttonHeight = ((RectTransform)_buttonPrefab.transform).sizeDelta.y;
		float step = buttonHeight + _buttonSpacing;

		Button resume = SpawnButton(_resumeLabel, _buttonsTopOffsetY, Resume);
		Button restart = SpawnButton(_restartLabel, _buttonsTopOffsetY - step, Restart);
		Button goToMenu = SpawnButton(_goToMenuLabel, _buttonsTopOffsetY - step * 2f, GoToMenu);

		AnimateButtonAppear((RectTransform)resume.transform, 0f);
		AnimateButtonAppear((RectTransform)restart.transform, _buttonAppearStagger);
		AnimateButtonAppear((RectTransform)goToMenu.transform, _buttonAppearStagger * 2f);
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
		_spawnedButtons.Add(button.gameObject);
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

	private void DestroyButtons()
	{
		for (int i = _spawnedButtons.Count - 1; i >= 0; i--)
		{
			if (_spawnedButtons[i]) Destroy(_spawnedButtons[i]);
		}
		_spawnedButtons.Clear();
	}

	private void Restart()
	{
		LoadScene(_gameplaySceneName);
	}

	private void GoToMenu()
	{
		LoadScene(_menuSceneName);
	}

	private void LoadScene(string sceneName)
	{
		if (_transitioning) return;
		_transitioning = true;

		if (_screenFader)
			_screenFader.FadeToBlack(_fadeDuration).SetUpdate(true);

		DOVirtual.DelayedCall(_fadeDuration, () =>
		{
			Time.timeScale = 1f;
			SceneManager.LoadScene(sceneName);
		}, ignoreTimeScale: true);
	}

	private void OnDisable()
	{
		if (_isPaused)
		{
			Time.timeScale = 1f;
			_isPaused = false;
		}
	}
}
