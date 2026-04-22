using DG.Tweening;
using TMPro;
using UnityEngine;

public class SongInfoPanel : MonoBehaviour
{
	[SerializeField] private CanvasGroup _canvasGroup;
	[SerializeField] private TMP_Text _titleText;
	[SerializeField] private TMP_Text _artistText;
	[SerializeField] private TMP_Text _bpmText;
	[SerializeField] private TMP_Text _durationText;
	[SerializeField] private TMP_Text _difficultyText;

	[Header("Animation")]
	[SerializeField, Min(0f)] private float _fadeOutDuration = 0.1f;
	[SerializeField, Min(0f)] private float _fadeInDuration = 0.2f;

	private void Reset()
	{
		_canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Awake()
	{
		if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
	}

	public void SetSong(LevelData data)
	{
		if (_canvasGroup == null)
		{
			ApplyText(data);
			return;
		}

		_canvasGroup.DOKill();
		_canvasGroup.DOFade(0f, _fadeOutDuration).OnComplete(() =>
		{
			ApplyText(data);
			_canvasGroup.DOFade(1f, _fadeInDuration);
		});
	}

	private void ApplyText(LevelData data)
	{
		if (_titleText) _titleText.text = string.IsNullOrEmpty(data.Title) ? data.name : data.Title;
		if (_artistText) _artistText.text = string.IsNullOrEmpty(data.Artist) ? "Unknown Artist" : data.Artist;
		if (_bpmText) _bpmText.text = $"BPM  {data.BPM:0}";
		if (_durationText) _durationText.text = $"Length  {FormatDuration(data.Song != null ? data.Song.length : 0f)}";
		if (_difficultyText) _difficultyText.text = $"Difficulty  {StarString(data.Difficulty)}";
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
