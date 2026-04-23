using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	public class SongPickerEntry : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _titleLabel;
		[SerializeField] private TextMeshProUGUI _artistLabel;
		[SerializeField] private TextMeshProUGUI _statusLabel;
		[SerializeField] private Button _selectButton;

		public void Bind(string title, string artist, bool hasExistingSave, System.Action onClick)
		{
			if (_titleLabel != null) _titleLabel.text = title;
			if (_artistLabel != null) _artistLabel.text = artist;
			if (_statusLabel != null) _statusLabel.text = hasExistingSave ? "Edit (existing)" : "New";

			if (_selectButton != null)
			{
				_selectButton.onClick.RemoveAllListeners();
				_selectButton.onClick.AddListener(() => onClick?.Invoke());
			}
		}
	}
}
