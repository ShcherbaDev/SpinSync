using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class SongCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[Header("Layout")]
	[SerializeField] private RectTransform _rect;
	[SerializeField] private CanvasGroup _canvasGroup;

	[Header("Visuals")]
	[SerializeField] private Image _background;
	[SerializeField] private Image _accentBar;

	[Header("Text")]
	[SerializeField] private TMP_Text _titleText;
	[SerializeField] private TMP_Text _artistText;
	[SerializeField] private TMP_Text _difficultyText;
	[SerializeField] private TMP_Text _durationText;

	[Header("Animation")]
	[SerializeField, Min(0f)] private float _selectDuration = 0.3f;
	[SerializeField, Min(0f)] private float _hoverDuration = 0.15f;
	[SerializeField] private float _selectedScale = 1.15f;
	[SerializeField] private float _hoverScale = 1.05f;
	[SerializeField] private float _selectedOffsetX = -80f;
	[SerializeField, Range(0f, 1f)] private float _unselectedAlpha = 0.6f;
	[SerializeField, Range(0f, 1f)] private float _unselectedBgAlpha = 0.18f;
	[SerializeField, Range(0f, 1f)] private float _selectedBgAlpha = 0.4f;

	private LevelData _data;
	private int _index;
	private System.Action<int> _onClicked;
	private bool _isSelected;
	private bool _isHovered;

	public RectTransform Rect => _rect;
	public LevelData Data => _data;

	private void Reset()
	{
		_rect = (RectTransform)transform;
		_canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Awake()
	{
		if (_rect == null) _rect = (RectTransform)transform;
		if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
	}

	public void Bind(LevelData data, int index, System.Action<int> onClicked)
	{
		_data = data;
		_index = index;
		_onClicked = onClicked;

		if (_titleText) _titleText.text = string.IsNullOrEmpty(data.Title) ? data.name : data.Title;
		if (_artistText) _artistText.text = string.IsNullOrEmpty(data.Artist) ? "Unknown Artist" : data.Artist;
		if (_difficultyText) _difficultyText.text = StarString(data.Difficulty);
		if (_durationText) _durationText.text = FormatDuration(data.Song != null ? data.Song.length : 0f);

		if (_background)
		{
			Color normalTint = data.AccentColor;
			normalTint.a = _unselectedBgAlpha;
			_background.color = normalTint;
		}

		if (_accentBar)
		{
			Color bar = data.AccentColor;
			bar.a = 1f;
			_accentBar.color = bar;
		}
	}

	public void SetSelected(bool selected, bool animate)
	{
		_isSelected = selected;
		ApplyVisuals(animate);
	}

	private string ScaleId => "cardScale_" + GetInstanceID();
	private string PosXId => "cardPosX_" + GetInstanceID();
	private string AlphaId => "cardAlpha_" + GetInstanceID();
	private string BgColorId => "cardBgColor_" + GetInstanceID();

	private void ApplyVisuals(bool animate)
	{
		DOTween.Kill(ScaleId);
		DOTween.Kill(PosXId);
		DOTween.Kill(AlphaId);
		DOTween.Kill(BgColorId);

		float targetScale = _isSelected ? _selectedScale : (_isHovered ? _hoverScale : 1f);
		float targetX = _isSelected ? _selectedOffsetX : 0f;
		float targetAlpha = _isSelected ? 1f : _unselectedAlpha;
		Ease ease = _isSelected ? Ease.OutBack : Ease.OutQuad;

		Color bgColor = _background != null ? _background.color : Color.white;
		bgColor.a = _isSelected ? _selectedBgAlpha : _unselectedBgAlpha;

		if (animate)
		{
			_rect.DOScale(targetScale, _selectDuration).SetEase(ease).SetId(ScaleId).SetLink(gameObject);
			_rect.DOAnchorPosX(targetX, _selectDuration).SetEase(ease).SetId(PosXId).SetLink(gameObject);
			_canvasGroup.DOFade(targetAlpha, _selectDuration).SetId(AlphaId).SetLink(gameObject);
			if (_background) _background.DOColor(bgColor, _selectDuration).SetId(BgColorId).SetLink(gameObject);
		}
		else
		{
			_rect.localScale = Vector3.one * targetScale;
			Vector2 pos = _rect.anchoredPosition;
			pos.x = targetX;
			_rect.anchoredPosition = pos;
			_canvasGroup.alpha = targetAlpha;
			if (_background) _background.color = bgColor;
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		_onClicked?.Invoke(_index);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (_isSelected) return;
		_isHovered = true;
		DOTween.Kill(ScaleId);
		_rect.DOScale(_hoverScale, _hoverDuration).SetId(ScaleId).SetLink(gameObject);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (_isSelected) return;
		_isHovered = false;
		DOTween.Kill(ScaleId);
		_rect.DOScale(1f, _hoverDuration).SetId(ScaleId).SetLink(gameObject);
	}

	private static string StarString(int difficulty)
	{
		int filled = Mathf.Clamp(difficulty, 0, 5);
		return new string('\u2605', filled) + new string('\u2606', 5 - filled);
	}

	private static string FormatDuration(float seconds)
	{
		int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
		return $"{total / 60}:{total % 60:00}";
	}
}
