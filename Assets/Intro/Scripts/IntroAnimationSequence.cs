using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SpinSync.EditorRuntime;
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
	[SerializeField, Tooltip("Fallback clip played only when no levels are available in StreamingAssets/Levels/ or persistentDataPath/Levels/.")]
	private AudioClip _musicClip;
	[SerializeField, Range(0f, 1f)] private float _musicVolume = 0.7f;
	[SerializeField, Min(30f), Tooltip("BPM used for the logo pulse if no level is picked. When a random level is picked, its BPM is used instead.")]
	private float _musicBPM = 120f;

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
	[SerializeField] private string _levelEditorSceneName = "LevelEditor";

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
		_musicAudioSource = GetComponent<AudioSource>();
		if (!_musicAudioSource)
			_musicAudioSource = gameObject.AddComponent<AudioSource>();

		_musicAudioSource.volume = _musicVolume;
		_musicAudioSource.loop = true;
		_musicAudioSource.playOnAwake = false;

		StartCoroutine(PickAndPlayRandomLevelMusic());
	}

	private IEnumerator PickAndPlayRandomLevelMusic()
	{
		IReadOnlyList<string> ids = LevelStorage.ListAll();
		if (ids != null && ids.Count > 0)
		{
			// Try a few random picks; fall back to iterating if any fail to load audio.
			HashSet<string> tried = new HashSet<string>();
			for (int attempt = 0; attempt < ids.Count + 3; attempt++)
			{
				string id = (attempt < 3)
					? ids[Random.Range(0, ids.Count)]
					: NextUntried(ids, tried);
				if (string.IsNullOrEmpty(id)) break;
				if (!tried.Add(id)) continue;

				Level level = LevelStorage.Load(id);
				if (level == null) continue;

				yield return LevelAudioLoader.LoadInto(level);
				if (level.AudioClip == null) continue;

				_musicBPM = level.BPM > 0f ? level.BPM : _musicBPM;
				_musicAudioSource.clip = level.AudioClip;
				_musicAudioSource.time = Mathf.Clamp(level.PreviewStartTime, 0f, Mathf.Max(0f, level.AudioClip.length - 0.1f));
				_musicAudioSource.Play();
				yield break;
			}
		}

		// Fallback: the legacy inspector-assigned clip.
		if (_musicClip != null)
		{
			_musicAudioSource.clip = _musicClip;
			_musicAudioSource.time = 0f;
			_musicAudioSource.Play();
		}
	}

	private static string NextUntried(IReadOnlyList<string> ids, HashSet<string> tried)
	{
		foreach (string id in ids)
			if (!tried.Contains(id)) return id;
		return null;
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
		Button editor = SpawnButton("Level Editor", _buttonsTopOffsetY - step, LoadLevelEditor);
		Button quit = SpawnButton("Quit", _buttonsTopOffsetY - step * 2f, QuitGame);

		AnimateButtonAppear((RectTransform)play.transform, 0f);
		AnimateButtonAppear((RectTransform)editor.transform, _buttonAppearStagger);
		AnimateButtonAppear((RectTransform)quit.transform, _buttonAppearStagger * 2f);
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

	private void LoadLevelEditor()
	{
		SceneManager.LoadScene(_levelEditorSceneName);
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
