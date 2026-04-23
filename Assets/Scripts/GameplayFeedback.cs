using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayFeedback : MonoBehaviour
{
	[Header("Platform")]
	[SerializeField] private Transform _platformTransform;
	[SerializeField] private SpriteRenderer _platformSprite;

	[Header("Hit Spark")]
	[SerializeField] private GameObject _hitSparkPrefab;
	[SerializeField] private float _sparkPerfectScale = 1.5f;
	[SerializeField] private float _sparkGoodScale = 1.1f;
	[SerializeField] private float _sparkBadScale = 0.8f;

	[Header("Platform Punch")]
	[SerializeField] private float _punchPerfect = 0.15f;
	[SerializeField] private float _punchGood = 0.1f;
	[SerializeField] private float _punchBad = 0.05f;
	[SerializeField, Min(0f)] private float _punchDuration = 0.15f;
	[SerializeField, Min(1)] private int _punchVibrato = 4;
	[SerializeField, Range(0f, 1f)] private float _punchElasticity = 0.5f;

	[Header("Miss Flash")]
	[SerializeField] private Color _missFlashColor = Color.red;
	[SerializeField, Min(0f)] private float _missFlashInDuration = 0.08f;
	[SerializeField, Min(0f)] private float _missFlashOutDuration = 0.17f;
	[SerializeField, Min(0f)] private float _missShakeDuration = 0.3f;
	[SerializeField] private float _missShakeZStrength = 15f;
	[SerializeField, Min(1)] private int _missShakeVibrato = 10;

	[Header("UI Texts")]
	[SerializeField] private RectTransform _scoreText;
	[SerializeField] private RectTransform _comboText;
	[SerializeField] private TextMeshProUGUI _comboTextTMP;
	[SerializeField] private Color _comboTextDefaultColor = Color.white;

	[Header("Score Punch")]
	[SerializeField] private float _scorePunch = 0.1f;
	[SerializeField, Min(0f)] private float _scorePunchDuration = 0.15f;

	[Header("Combo Punch")]
	[SerializeField] private float _comboPunch = 0.2f;
	[SerializeField, Min(0f)] private float _comboPunchDuration = 0.25f;
	[SerializeField, Min(0f)] private float _comboColorFlashDuration = 0.2f;

	[Header("Combo Reset")]
	[SerializeField] private Color _comboResetColor = Color.red;
	[SerializeField] private float _comboResetShrinkScale = 0.8f;
	[SerializeField, Min(0f)] private float _comboResetDuration = 0.2f;

	[Header("Grade Text")]
	[SerializeField, Tooltip("World-space TextMeshPro prefab spawned at the hit/miss location")]
	private TextMeshPro _gradeTextPrefab;
	[SerializeField] private string _perfectLabel = "Perfect";
	[SerializeField] private string _goodLabel = "Good";
	[SerializeField] private string _badLabel = "Bad";
	[SerializeField] private string _missLabel = "Miss";
	[SerializeField] private Color _perfectOutlineColor = new Color(0.2f, 0.5f, 1f, 1f);
	[SerializeField] private Color _goodOutlineColor = new Color(0.2f, 0.85f, 0.3f, 1f);
	[SerializeField] private Color _badOutlineColor = new Color(0.55f, 0.32f, 0.08f, 1f);
	[SerializeField] private Color _missOutlineColor = Color.red;
	[SerializeField, Min(0f)] private float _gradeTextDuration = 1f;
	[SerializeField] private float _gradeTextRiseDistance = 0.8f;
	[SerializeField, Range(0f, 1f)] private float _gradeTextOutlineWidth = 0.25f;

	[Header("Health Bar")]
	[SerializeField] private RectTransform _healthBarContainer;
	[SerializeField, Tooltip("Used to identify which health icon is a lost life so it can be flashed")]
	private Sprite _lostLifeSprite;
	[SerializeField, Min(0f)] private float _healthShakeDuration = 0.3f;
	[SerializeField] private float _healthShakeStrength = 12f;
	[SerializeField, Min(1)] private int _healthShakeVibrato = 10;
	[SerializeField, Min(0f)] private float _lostLifeFlashDuration = 0.25f;

	private Color _platformOriginalColor;
	private Vector3 _platformOriginalScale;
	private Vector3 _scoreOriginalScale;
	private Vector3 _comboOriginalScale;
	private Vector3 _healthOriginalPos;

	private Tween _platformColorTween;
	private Tween _platformShakeTween;
	private Tween _platformPunchTween;
	private Tween _scorePunchTween;
	private Tween _comboPunchTween;
	private Tween _comboColorTween;
	private Tween _healthShakeTween;

	private void Awake()
	{
		if (_platformSprite)
			_platformOriginalColor = _platformSprite.color;
		if (_platformTransform)
			_platformOriginalScale = _platformTransform.localScale;
		if (_scoreText)
			_scoreOriginalScale = _scoreText.localScale;
		if (_comboText)
			_comboOriginalScale = _comboText.localScale;
		if (_healthBarContainer)
			_healthOriginalPos = _healthBarContainer.anchoredPosition3D;
	}

	public void PlayHit(Vector3 worldPos, NoteGrade grade, Color comboColor)
	{
		SpawnSpark(worldPos, grade, comboColor);
		PunchPlatform(grade);
		ShowGradeText(worldPos, grade);
	}

	public void PlayMiss(Vector3 worldPos)
	{
		FlashPlatformRed();
		ShowGradeText(worldPos, NoteGrade.Miss);
	}

	public void OnScoreChanged()
	{
		if (!_scoreText) return;
		_scorePunchTween?.Kill(true);
		_scoreText.localScale = _scoreOriginalScale;
		_scorePunchTween = _scoreText.DOPunchScale(Vector3.one * _scorePunch, _scorePunchDuration)
			.SetLink(_scoreText.gameObject);
	}

	public void OnComboIncreased(Color comboColor)
	{
		if (_comboText)
		{
			_comboPunchTween?.Kill(true);
			_comboText.localScale = _comboOriginalScale;
			_comboPunchTween = _comboText.DOPunchScale(Vector3.one * _comboPunch, _comboPunchDuration)
				.SetLink(_comboText.gameObject);
		}

		if (_comboTextTMP)
		{
			_comboColorTween?.Kill();
			_comboTextTMP.color = comboColor;
			_comboColorTween = _comboTextTMP.DOColor(_comboTextDefaultColor, _comboColorFlashDuration)
				.SetLink(_comboTextTMP.gameObject);
		}
	}

	public void OnComboReset()
	{
		if (_comboText)
		{
			_comboPunchTween?.Kill();
			_comboText.localScale = _comboOriginalScale;

			Sequence shrink = DOTween.Sequence();
			shrink.Append(_comboText.DOScale(_comboOriginalScale * _comboResetShrinkScale, _comboResetDuration * 0.5f));
			shrink.Append(_comboText.DOScale(_comboOriginalScale, _comboResetDuration * 0.5f));
			shrink.SetLink(_comboText.gameObject);
			_comboPunchTween = shrink;
		}

		if (_comboTextTMP)
		{
			_comboColorTween?.Kill();
			_comboTextTMP.color = _comboResetColor;
			_comboColorTween = _comboTextTMP.DOColor(_comboTextDefaultColor, _comboResetDuration)
				.SetLink(_comboTextTMP.gameObject);
		}
	}

	public void OnLifeLost()
	{
		if (_healthBarContainer)
		{
			_healthShakeTween?.Kill(true);
			_healthBarContainer.anchoredPosition3D = _healthOriginalPos;
			_healthShakeTween = _healthBarContainer.DOShakeAnchorPos(
					_healthShakeDuration,
					new Vector2(_healthShakeStrength, 0f),
					_healthShakeVibrato)
				.SetLink(_healthBarContainer.gameObject);
		}

		FlashNewestLostLife();
	}

	private void SpawnSpark(Vector3 worldPos, NoteGrade grade, Color comboColor)
	{
		if (!_hitSparkPrefab) return;

		GameObject sparkObj = Instantiate(_hitSparkPrefab, worldPos, Quaternion.identity);
		HitSpark spark = sparkObj.GetComponent<HitSpark>();
		if (!spark) return;

		Color color = comboColor;
		float multiplier = _sparkGoodScale;
		switch (grade)
		{
			case NoteGrade.Perfect:
				multiplier = _sparkPerfectScale;
				break;
			case NoteGrade.Good:
				multiplier = _sparkGoodScale;
				break;
			case NoteGrade.Bad:
				multiplier = _sparkBadScale;
				color = DesaturateDim(comboColor, 0.5f, 0.75f);
				break;
		}

		spark.Play(color, multiplier);
	}

	private void PunchPlatform(NoteGrade grade)
	{
		if (!_platformTransform) return;

		float magnitude = grade switch
		{
			NoteGrade.Perfect => _punchPerfect,
			NoteGrade.Good => _punchGood,
			NoteGrade.Bad => _punchBad,
			_ => 0f
		};

		_platformPunchTween?.Kill(true);
		_platformTransform.localScale = _platformOriginalScale;
		_platformPunchTween = _platformTransform.DOPunchScale(Vector3.one * magnitude, _punchDuration, _punchVibrato, _punchElasticity)
			.SetLink(_platformTransform.gameObject);
	}

	private void FlashPlatformRed()
	{
		if (!_platformSprite) return;

		_platformColorTween?.Kill();
		_platformSprite.color = _platformOriginalColor;

		Sequence flash = DOTween.Sequence();
		flash.Append(_platformSprite.DOColor(_missFlashColor, _missFlashInDuration));
		flash.Append(_platformSprite.DOColor(_platformOriginalColor, _missFlashOutDuration));
		flash.SetLink(_platformSprite.gameObject);
		_platformColorTween = flash;
	}

private void FlashNewestLostLife()
	{
		if (!_healthBarContainer || _healthBarContainer.childCount == 0) return;

		for (int i = 0; i < _healthBarContainer.childCount; i++)
		{
			Transform child = _healthBarContainer.GetChild(i);
			Image img = child.GetComponent<Image>();
			if (!img) continue;

			if (IsLostLife(img))
			{
				Color start = img.color;
				Sequence fade = DOTween.Sequence();
				fade.Append(img.DOColor(_missFlashColor, _lostLifeFlashDuration * 0.4f));
				fade.Append(img.DOColor(start, _lostLifeFlashDuration * 0.6f));
				fade.SetLink(img.gameObject, LinkBehaviour.KillOnDestroy);
				return;
			}
		}
	}

	private void ShowGradeText(Vector3 worldPos, NoteGrade grade)
	{
		if (!_gradeTextPrefab) return;

		TextMeshPro text = Instantiate(_gradeTextPrefab, worldPos, Quaternion.identity);
		text.text = GradeLabel(grade);

		text.fontMaterial = new Material(text.fontSharedMaterial);
		text.outlineColor = GradeOutlineColor(grade);
		text.outlineWidth = _gradeTextOutlineWidth;

		text.alpha = 0f;

		Vector3 endPos = worldPos + Vector3.up * _gradeTextRiseDistance;

		Sequence seq = DOTween.Sequence();
		seq.Insert(0f, text.transform.DOMove(endPos, _gradeTextDuration).SetEase(Ease.OutCubic));
		seq.Insert(0f, DOTween.To(() => text.alpha, v => text.alpha = v, 1f, _gradeTextDuration * 0.15f));
		seq.Insert(_gradeTextDuration * 0.7f, DOTween.To(() => text.alpha, v => text.alpha = v, 0f, _gradeTextDuration * 0.3f));
		seq.OnComplete(() => Destroy(text.gameObject));
		seq.SetLink(text.gameObject);
	}

	private string GradeLabel(NoteGrade grade)
	{
		return grade switch
		{
			NoteGrade.Perfect => _perfectLabel,
			NoteGrade.Good => _goodLabel,
			NoteGrade.Bad => _badLabel,
			_ => _missLabel
		};
	}

	private Color GradeOutlineColor(NoteGrade grade)
	{
		return grade switch
		{
			NoteGrade.Perfect => _perfectOutlineColor,
			NoteGrade.Good => _goodOutlineColor,
			NoteGrade.Bad => _badOutlineColor,
			_ => _missOutlineColor
		};
	}

	private void OnDestroy()
	{
		_platformColorTween?.Kill();
		_platformShakeTween?.Kill();
		_platformPunchTween?.Kill();
		_scorePunchTween?.Kill();
		_comboPunchTween?.Kill();
		_comboColorTween?.Kill();
		_healthShakeTween?.Kill();
	}

	private bool IsLostLife(Image img)
	{
		if (!img || !img.sprite) return false;
		if (_lostLifeSprite) return img.sprite == _lostLifeSprite;
		return img.sprite.name.ToLower().Contains("lost");
	}

	private static Color DesaturateDim(Color c, float saturationFactor, float valueFactor)
	{
		Color.RGBToHSV(c, out float h, out float s, out float v);
		return Color.HSVToRGB(h, s * saturationFactor, v * valueFactor);
	}
}
