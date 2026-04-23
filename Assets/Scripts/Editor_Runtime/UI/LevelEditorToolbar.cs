using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	public class LevelEditorToolbar : MonoBehaviour
	{
		[SerializeField] private LevelEditor _editor;

		[Header("Buttons")]
		[SerializeField] private Button _saveButton;
		[SerializeField] private TextMeshProUGUI _saveButtonLabel;
		[SerializeField] private Button _testButton;
		[SerializeField] private TextMeshProUGUI _testButtonLabel;
		[SerializeField] private Button _backButton;

		[Header("Latency")]
		[SerializeField] private Slider _latencySlider;
		[SerializeField] private TextMeshProUGUI _latencyLabel;
		[SerializeField, Min(0f)] private float _latencyMinMs;
		[SerializeField, Min(0f)] private float _latencyMaxMs = 250f;

		[Header("BPM Snap")]
		[SerializeField] private Toggle _snapToggle;
		[SerializeField] private TMP_Dropdown _snapDropdown;

		[Header("Song Title")]
		[SerializeField] private TextMeshProUGUI _songTitleLabel;

		[Header("Confirm Dialog (Back when dirty)")]
		[SerializeField] private GameObject _confirmDialog;
		[SerializeField] private Button _confirmYes;
		[SerializeField] private Button _confirmNo;

		private void OnEnable()
		{
			if (_saveButton != null) _saveButton.onClick.AddListener(_editor.SaveCurrent);
			if (_testButton != null) _testButton.onClick.AddListener(ToggleTestMode);
			if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

			if (_latencySlider != null)
			{
				_latencySlider.minValue = _latencyMinMs;
				_latencySlider.maxValue = _latencyMaxMs;
				_latencySlider.SetValueWithoutNotify(_editor.InputLatencySeconds * 1000f);
				_latencySlider.onValueChanged.AddListener(OnLatencyChanged);
			}

			if (_snapToggle != null)
			{
				_snapToggle.SetIsOnWithoutNotify(_editor.BpmSnapEnabled);
				_snapToggle.onValueChanged.AddListener(OnSnapToggled);
			}

			if (_snapDropdown != null)
			{
				_snapDropdown.ClearOptions();
				_snapDropdown.AddOptions(new System.Collections.Generic.List<string> { "1/4", "1/8", "1/16" });
				_snapDropdown.SetValueWithoutNotify(SubdivisionToIndex(_editor.SnapSubdivision));
				_snapDropdown.onValueChanged.AddListener(OnSnapDropdownChanged);
			}

			if (_confirmYes != null) _confirmYes.onClick.AddListener(OnConfirmYes);
			if (_confirmNo != null) _confirmNo.onClick.AddListener(OnConfirmNo);
			if (_confirmDialog != null) _confirmDialog.SetActive(false);

			if (_editor != null)
			{
				_editor.OnLevelLoaded += RefreshLabels;
				_editor.OnDirtyChanged += RefreshLabels;
				_editor.OnModeChanged += RefreshLabels;
			}

			RefreshLabels();
		}

		private void OnDisable()
		{
			if (_saveButton != null) _saveButton.onClick.RemoveListener(_editor.SaveCurrent);
			if (_testButton != null) _testButton.onClick.RemoveListener(ToggleTestMode);
			if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
			if (_latencySlider != null) _latencySlider.onValueChanged.RemoveListener(OnLatencyChanged);
			if (_snapToggle != null) _snapToggle.onValueChanged.RemoveListener(OnSnapToggled);
			if (_snapDropdown != null) _snapDropdown.onValueChanged.RemoveListener(OnSnapDropdownChanged);
			if (_confirmYes != null) _confirmYes.onClick.RemoveListener(OnConfirmYes);
			if (_confirmNo != null) _confirmNo.onClick.RemoveListener(OnConfirmNo);

			if (_editor != null)
			{
				_editor.OnLevelLoaded -= RefreshLabels;
				_editor.OnDirtyChanged -= RefreshLabels;
				_editor.OnModeChanged -= RefreshLabels;
			}
		}

		private void ToggleTestMode()
		{
			if (_editor.Mode == EditorMode.Edit)
				_editor.EnterTestMode();
			else
				_editor.ExitTestMode();
			RefreshLabels();
		}

		private void OnLatencyChanged(float ms)
		{
			_editor.InputLatencySeconds = ms / 1000f;
			RefreshLabels();
		}

		private void OnSnapToggled(bool on)
		{
			_editor.BpmSnapEnabled = on;
		}

		private void OnSnapDropdownChanged(int index)
		{
			_editor.SnapSubdivision = IndexToSubdivision(index);
		}

		private void OnBackClicked()
		{
			if (_editor.IsDirty && _confirmDialog != null)
				_confirmDialog.SetActive(true);
			else
				_editor.BackToMenu();
		}

		private void OnConfirmYes()
		{
			if (_confirmDialog != null) _confirmDialog.SetActive(false);
			_editor.BackToMenu();
		}

		private void OnConfirmNo()
		{
			if (_confirmDialog != null) _confirmDialog.SetActive(false);
		}

		private void RefreshLabels()
		{
			if (_saveButtonLabel != null)
				_saveButtonLabel.text = _editor.IsDirty ? "• Save" : "Save";

			if (_testButtonLabel != null)
				_testButtonLabel.text = _editor.Mode == EditorMode.Edit ? "Test" : "Stop Test";

			if (_latencyLabel != null)
				_latencyLabel.text = $"Latency: {Mathf.RoundToInt(_editor.InputLatencySeconds * 1000f)} ms";

			if (_songTitleLabel != null)
				_songTitleLabel.text = _editor.SelectedSong != null ? _editor.SelectedSong.Title : "(no song)";
		}

		private static int SubdivisionToIndex(BeatSubdivision sub)
		{
			switch (sub)
			{
				case BeatSubdivision.Sixteenth: return 2;
				case BeatSubdivision.Eighth: return 1;
				default: return 0;
			}
		}

		private static BeatSubdivision IndexToSubdivision(int index)
		{
			switch (index)
			{
				case 2: return BeatSubdivision.Sixteenth;
				case 1: return BeatSubdivision.Eighth;
				default: return BeatSubdivision.Quarter;
			}
		}
	}
}
