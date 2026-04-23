using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	public class TransportControlsUI : MonoBehaviour
	{
		[SerializeField] private LevelEditor _editor;

		[Header("Buttons")]
		[SerializeField] private Button _playButton;
		[SerializeField] private Button _pauseButton;
		[SerializeField] private Button _restartButton;

		[Header("Scrub")]
		[SerializeField] private Slider _scrubSlider;
		[SerializeField] private TextMeshProUGUI _timeLabel;

		private bool _suppressScrubCallback;

		private void OnEnable()
		{
			if (_playButton != null) _playButton.onClick.AddListener(_editor.Play);
			if (_pauseButton != null) _pauseButton.onClick.AddListener(_editor.Pause);
			if (_restartButton != null) _restartButton.onClick.AddListener(_editor.Restart);
			if (_scrubSlider != null) _scrubSlider.onValueChanged.AddListener(OnScrubChanged);

			if (_editor != null)
			{
				_editor.OnLevelLoaded += SyncFromEditor;
				_editor.OnPlaybackStateChanged += SyncFromEditor;
			}
		}

		private void OnDisable()
		{
			if (_playButton != null) _playButton.onClick.RemoveListener(_editor.Play);
			if (_pauseButton != null) _pauseButton.onClick.RemoveListener(_editor.Pause);
			if (_restartButton != null) _restartButton.onClick.RemoveListener(_editor.Restart);
			if (_scrubSlider != null) _scrubSlider.onValueChanged.RemoveListener(OnScrubChanged);

			if (_editor != null)
			{
				_editor.OnLevelLoaded -= SyncFromEditor;
				_editor.OnPlaybackStateChanged -= SyncFromEditor;
			}
		}

		private void Update()
		{
			if (_editor == null) return;

			float dur = _editor.SongDuration;
			float head = _editor.PlayheadTime;

			if (_scrubSlider != null && dur > 0f)
			{
				_suppressScrubCallback = true;
				_scrubSlider.SetValueWithoutNotify(head / dur);
				_suppressScrubCallback = false;
			}

			if (_timeLabel != null)
				_timeLabel.text = $"{FormatTime(head)} / {FormatTime(dur)}";
		}

		private void OnScrubChanged(float value)
		{
			if (_suppressScrubCallback || _editor == null) return;
			_editor.SetPlayhead(value * _editor.SongDuration);
		}

		private void SyncFromEditor()
		{
			if (_playButton != null) _playButton.gameObject.SetActive(!_editor.IsPlaying);
			if (_pauseButton != null) _pauseButton.gameObject.SetActive(_editor.IsPlaying);
		}

		private static string FormatTime(float seconds)
		{
			int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
			int m = total / 60;
			int s = total % 60;
			return $"{m:00}:{s:00}";
		}
	}
}
