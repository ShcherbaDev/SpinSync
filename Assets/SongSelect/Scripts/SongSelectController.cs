using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SpinSync.EditorRuntime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SongSelectController : MonoBehaviour
{
	[Header("Scene Navigation")]
	[SerializeField] private string _gameplaySceneName = "Gameplay";
	[SerializeField] private string _introSceneName = "Intro";
	[SerializeField] private ScreenFader _faderPrefab;
	[SerializeField, Min(0.1f)] private float _fadeDuration = 0.4f;

	[Header("Scene UI References")]
	[SerializeField, Tooltip("Prefab of a single song card (with SongCard component)")]
	private SongCard _cardPrefab;

	[SerializeField, Tooltip("Container in which cards will be instantiated. Cards are positioned relative to this rect.")]
	private RectTransform _cardsContainer;

	[SerializeField] private SongInfoPanel _infoPanel;
	[SerializeField] private SongPreviewPlayer _previewPlayer;

	[SerializeField, Tooltip("Full-screen overlay image used for the per-track accent tint")]
	private Image _accentTintOverlay;

	[SerializeField] private Button _playButton;
	[SerializeField] private Button _backButton;

	[Header("Card Layout")]
	[SerializeField, Min(1f)] private float _cardSpacing = 130f;
	[SerializeField, Min(0f)] private float _layoutDuration = 0.4f;
	[SerializeField, Min(0f)] private float _layoutStaggerPerCard = 0.03f;

	[Header("Accent Tint")]
	[SerializeField, Range(0f, 1f)] private float _tintAlpha = 0.18f;
	[SerializeField, Min(0f)] private float _tintFadeDuration = 0.4f;

	[Header("Play Button Pulse")]
	[SerializeField] private bool _enablePlayButtonPulse = true;
	[SerializeField, Min(1f)] private float _playButtonPulseScale = 1.03f;
	[SerializeField, Min(0.05f)] private float _playButtonPulseDuration = 0.7f;

	[Header("Input")]
	[SerializeField] private float _scrollDeadzone = 50f;

	private readonly List<SongCard> _cards = new List<SongCard>();
	private readonly List<Level> _levels = new List<Level>();
	private int _selectedIndex;
	private ScreenFader _fader;
	private bool _interactive;
	private RectTransform _playButtonRect;

	private void Awake()
	{
		if (_playButton != null)
		{
			_playButton.onClick.AddListener(StartSelectedSong);
			_playButtonRect = (RectTransform)_playButton.transform;
		}

		if (_backButton != null)
			_backButton.onClick.AddListener(ReturnToIntro);

		if (_faderPrefab != null)
			_fader = Instantiate(_faderPrefab);
	}

	private void Start()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		if (!ValidateReferences()) return;

		IReadOnlyList<string> ids = LevelStorage.ListAll();
		if (ids.Count == 0)
		{
			Debug.LogWarning("SongSelectController: No levels found. Expected folders under StreamingAssets/Levels/ or persistentDataPath/Levels/.");
			return;
		}

		for (int i = 0; i < ids.Count; i++)
		{
			Level level = LevelStorage.Load(ids[i]);
			if (level == null) continue;

			_levels.Add(level);

			SongCard card = Instantiate(_cardPrefab, _cardsContainer);
			card.name = $"Card_{i}_{level.SongId}";
			int capturedIndex = _cards.Count;
			card.Bind(level, capturedIndex, SelectByIndex);
			_cards.Add(card);

			// Preload audio in the background so duration/preview/play are ready when needed.
			StartCoroutine(LoadClipForCard(level, card));
		}

		if (_cards.Count == 0) return;

		_selectedIndex = 0;
		LayoutCards(animate: false);
		ApplySelection(animate: false);
		StartPlayButtonPulse();

		if (_fader != null)
		{
			_fader.FadeToBlack(0f);
			_fader.FadeFromBlack(_fadeDuration);
			DOVirtual.DelayedCall(_fadeDuration, () => _interactive = true);
		}
		else
		{
			_interactive = true;
		}
	}

	private IEnumerator LoadClipForCard(Level level, SongCard card)
	{
		yield return LevelAudioLoader.LoadInto(level);
		if (card != null) card.RefreshDuration();
	}

	private bool ValidateReferences()
	{
		if (_cardPrefab == null)
		{
			Debug.LogError("SongSelectController: Card Prefab is not assigned.");
			return false;
		}
		if (_cardsContainer == null)
		{
			Debug.LogError("SongSelectController: Cards Container is not assigned.");
			return false;
		}
		if (_infoPanel == null)
		{
			Debug.LogError("SongSelectController: Info Panel is not assigned.");
			return false;
		}
		if (_previewPlayer == null)
		{
			Debug.LogError("SongSelectController: Preview Player is not assigned.");
			return false;
		}
		return true;
	}

	private void Update()
	{
		if (!_interactive) return;

		Keyboard keyboard = Keyboard.current;
		if (keyboard != null)
		{
			if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
				MoveSelection(-1);
			else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
				MoveSelection(1);

			if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
				StartSelectedSong();

			if (keyboard.escapeKey.wasPressedThisFrame)
				ReturnToIntro();
		}

		Mouse mouse = Mouse.current;
		if (mouse != null)
		{
			float scrollY = mouse.scroll.ReadValue().y;
			if (scrollY > _scrollDeadzone) MoveSelection(-1);
			else if (scrollY < -_scrollDeadzone) MoveSelection(1);
		}
	}

	private void MoveSelection(int delta)
	{
		if (_cards.Count == 0) return;

		int next = Mathf.Clamp(_selectedIndex + delta, 0, _cards.Count - 1);
		if (next == _selectedIndex) return;

		_selectedIndex = next;
		LayoutCards(animate: true);
		ApplySelection(animate: true);
	}

	private void SelectByIndex(int index)
	{
		if (!_interactive) return;

		if (index == _selectedIndex)
		{
			StartSelectedSong();
			return;
		}

		_selectedIndex = Mathf.Clamp(index, 0, _cards.Count - 1);
		LayoutCards(animate: true);
		ApplySelection(animate: true);
	}

	private void LayoutCards(bool animate)
	{
		for (int i = 0; i < _cards.Count; i++)
		{
			RectTransform rect = _cards[i].Rect;
			float targetY = (_selectedIndex - i) * _cardSpacing;
			string layoutId = "cardLayoutY_" + rect.GetInstanceID();

			DOTween.Kill(layoutId);

			if (animate)
			{
				float stagger = Mathf.Abs(i - _selectedIndex) * _layoutStaggerPerCard;
				rect.DOAnchorPosY(targetY, _layoutDuration)
					.SetEase(Ease.OutCubic)
					.SetDelay(stagger)
					.SetId(layoutId)
					.SetLink(rect.gameObject);
			}
			else
			{
				Vector2 pos = rect.anchoredPosition;
				pos.y = targetY;
				rect.anchoredPosition = pos;
			}
		}
	}

	private void ApplySelection(bool animate)
	{
		for (int i = 0; i < _cards.Count; i++)
			_cards[i].SetSelected(i == _selectedIndex, animate);

		Level current = _cards[_selectedIndex].Data;
		if (_infoPanel != null) _infoPanel.SetSong(current);
		if (_previewPlayer != null) _previewPlayer.PlayPreview(current);

		if (_accentTintOverlay != null)
		{
			Color tint = current.AccentColor;
			tint.a = _tintAlpha;
			const string tintId = "accentTint";
			DOTween.Kill(tintId);
			if (animate)
				_accentTintOverlay.DOColor(tint, _tintFadeDuration)
					.SetId(tintId)
					.SetLink(_accentTintOverlay.gameObject);
			else
				_accentTintOverlay.color = tint;
		}
	}

	private void StartSelectedSong()
	{
		if (!_interactive || _cards.Count == 0) return;
		_interactive = false;

		SongSelection.Current = _cards[_selectedIndex].Data;

		if (_previewPlayer != null) _previewPlayer.FadeOutAll(_fadeDuration);
		if (_fader != null) _fader.FadeToBlack(_fadeDuration);

		DOVirtual.DelayedCall(_fadeDuration, () => SceneManager.LoadScene(_gameplaySceneName));
	}

	private void ReturnToIntro()
	{
		if (!_interactive) return;
		_interactive = false;

		if (_previewPlayer != null) _previewPlayer.FadeOutAll(_fadeDuration);
		if (_fader != null) _fader.FadeToBlack(_fadeDuration);

		DOVirtual.DelayedCall(_fadeDuration, () => SceneManager.LoadScene(_introSceneName));
	}

	private void StartPlayButtonPulse()
	{
		if (!_enablePlayButtonPulse || _playButtonRect == null) return;
		_playButtonRect.DOScale(_playButtonPulseScale, _playButtonPulseDuration)
			.SetEase(Ease.InOutSine)
			.SetLoops(-1, LoopType.Yoyo)
			.SetLink(_playButtonRect.gameObject);
	}
}
