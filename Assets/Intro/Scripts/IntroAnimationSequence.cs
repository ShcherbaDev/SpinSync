using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroAnimationSequence : MonoBehaviour
{
	[Header("Scene References")]
	[SerializeField] private RectTransform _canvasRoot;
	[SerializeField] private RectTransform _logo;

	[Header("Logo Intro")]
	[SerializeField, Min(0f)] private float _logoDuration = 5f;
	[SerializeField] private float _logoStartScale = 0.3f;
	[SerializeField] private float _logoEndScale = 1f;

	[Header("Background Music")]
	[SerializeField] private AudioClip _musicClip;
	[SerializeField, Range(0f, 1f)] private float _musicVolume = 0.7f;
	[SerializeField, Min(30f)] private float _musicBPM = 120f;

	[Header("Logo Pulse (after intro)")]
	[SerializeField, Range(0f, 1f)] private float _pulseAmplitude = 0.08f;
	[SerializeField] private AnimationCurve _pulseCurve = new AnimationCurve(
		new Keyframe(0f, 0f),
		new Keyframe(0.15f, 1f),
		new Keyframe(1f, 0f)
	);

	[Header("Platform Rotation")]
	[SerializeField] private Transform _platform;
	[SerializeField] private float _platformBaseRotationSpeed = 30f;
	[SerializeField] private float _platformImpulseRotationSpeed = 360f;
	[SerializeField] private AnimationCurve _platformImpulseCurve = new AnimationCurve(
		new Keyframe(0f, 0f),
		new Keyframe(0.15f, 1f),
		new Keyframe(1f, 0f)
	);
	[SerializeField, Range(0f, 1f)] private float _platformDirectionFlipChance = 0.5f;

	[Header("Buttons")]
	[SerializeField] private Button _buttonPrefab;
	[SerializeField] private float _buttonsTopOffsetY = -200f;
	[SerializeField] private float _buttonSpacing = 24f;

	[Header("Buttons Appear Animation")]
	[SerializeField, Min(0f)] private float _buttonAppearDuration = 0.45f;
	[SerializeField, Min(0f)] private float _buttonAppearStagger = 0.12f;
	[SerializeField] private float _buttonStartScale = 0.6f;

	[Header("Scene Loading")]
	[SerializeField] private string _gameplaySceneName = "Gameplay";

	[Header("Scene Transition")]
	[SerializeField] private ScreenFader _faderPrefab;
	[SerializeField, Min(0.1f)] private float _quitFadeDuration = 1f;

	private CanvasGroup _logoCanvasGroup;
	private AudioSource _musicAudioSource;
	private ScreenFader _screenFader;
	private float _pulseBeatTimer;
	private bool _pulseActive;
	private float _platformDirection = 1f;

	private void Awake()
	{
		_logoCanvasGroup = _logo.GetComponent<CanvasGroup>();
		if (!_logoCanvasGroup)
			_logoCanvasGroup = _logo.gameObject.AddComponent<CanvasGroup>();

		_logoCanvasGroup.alpha = 0f;
		_logo.localScale = Vector3.one * _logoStartScale;

		if (_faderPrefab)
			_screenFader = Instantiate(_faderPrefab);
	}

	private void Start()
	{
		StartBackgroundMusic();

		Sequence sequence = DOTween.Sequence();
		sequence.Append(_logoCanvasGroup.DOFade(1f, _logoDuration));
		sequence.Join(_logo.DOScale(_logoEndScale, _logoDuration).SetEase(Ease.OutCubic));
		sequence.AppendCallback(StartLogoPulse);
		sequence.AppendCallback(SpawnMenuButtons);
	}

	private void StartBackgroundMusic()
	{
		if (!_musicClip) return;

		_musicAudioSource = GetComponent<AudioSource>();
		if (!_musicAudioSource)
			_musicAudioSource = gameObject.AddComponent<AudioSource>();

		_musicAudioSource.clip = _musicClip;
		_musicAudioSource.volume = _musicVolume;
		_musicAudioSource.loop = true;
		_musicAudioSource.playOnAwake = false;
		_musicAudioSource.Play();
	}

	private void StartLogoPulse()
	{
		_pulseBeatTimer = 0f;
		_pulseActive = true;
	}

	private void Update()
	{
		float platformImpulseShape = 0f;

		if (_pulseActive)
		{
			float beatDuration = 60f / _musicBPM;
			_pulseBeatTimer += Time.deltaTime;
			if (_pulseBeatTimer >= beatDuration)
			{
				_pulseBeatTimer -= beatDuration;
				if (Random.value < _platformDirectionFlipChance)
					_platformDirection = -_platformDirection;
			}

			float beatT = _pulseBeatTimer / beatDuration;
			float logoShape = _pulseCurve.Evaluate(beatT);
			platformImpulseShape = _platformImpulseCurve.Evaluate(beatT);

			float peakScale = _logoEndScale * (1f + _pulseAmplitude);
			_logo.localScale = Vector3.one * Mathf.Lerp(_logoEndScale, peakScale, logoShape);
		}

		if (_platform)
		{
			float speed = _platformBaseRotationSpeed + _platformImpulseRotationSpeed * platformImpulseShape;
			_platform.Rotate(0f, 0f, _platformDirection * speed * Time.deltaTime);
		}
	}

	private void SpawnMenuButtons()
	{
		float buttonHeight = ((RectTransform)_buttonPrefab.transform).sizeDelta.y;
		float step = buttonHeight + _buttonSpacing;

		Button play = SpawnButton("Play", _buttonsTopOffsetY, LoadGameplay);
		Button quit = SpawnButton("Quit", _buttonsTopOffsetY - step, QuitGame);

		AnimateButtonAppear((RectTransform)play.transform, 0f);
		AnimateButtonAppear((RectTransform)quit.transform, _buttonAppearStagger);
	}

	private Button SpawnButton(string label, float yPosition, UnityEngine.Events.UnityAction onClick)
	{
		Button button = Instantiate(_buttonPrefab, _canvasRoot);
		button.name = label + "Button";

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

		cg.DOFade(1f, _buttonAppearDuration).SetDelay(delay);
		rect.DOScale(1f, _buttonAppearDuration).SetDelay(delay).SetEase(Ease.OutBack);
	}

	private void LoadGameplay()
	{
		SceneManager.LoadScene(_gameplaySceneName);
	}

	private void QuitGame()
	{
		if (_screenFader)
			_screenFader.FadeToBlack(_quitFadeDuration);

		if (_musicAudioSource && _musicAudioSource.isPlaying)
			_musicAudioSource.DOFade(0f, _quitFadeDuration);

		DOVirtual.DelayedCall(_quitFadeDuration, QuitApplication);
	}

	private static void QuitApplication()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
