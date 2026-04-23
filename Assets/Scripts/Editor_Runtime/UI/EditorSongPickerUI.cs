using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	/// <summary>
	/// Modal at scene start that lists songs from a SongLibrary asset and loads one into the LevelEditor.
	/// Re-openable from the toolbar later if desired.
	/// </summary>
	public class EditorSongPickerUI : MonoBehaviour
	{
		[SerializeField] private LevelEditor _editor;
		[SerializeField] private SongLibrary _library;

		[Header("UI")]
		[SerializeField] private GameObject _modalRoot;
		[SerializeField] private RectTransform _entriesContainer;
		[SerializeField] private SongPickerEntry _entryPrefab;
		[SerializeField] private Button _closeButton;

		private void Start()
		{
			Show();
		}

		private void OnEnable()
		{
			if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
		}

		private void OnDisable()
		{
			if (_closeButton != null) _closeButton.onClick.RemoveListener(Hide);
		}

		public void Show()
		{
			if (_modalRoot != null) _modalRoot.SetActive(true);
			Populate();
		}

		public void Hide()
		{
			if (_modalRoot != null) _modalRoot.SetActive(false);
		}

		private void Populate()
		{
			if (_entriesContainer == null || _entryPrefab == null) return;

			for (int i = _entriesContainer.childCount - 1; i >= 0; i--)
				Destroy(_entriesContainer.GetChild(i).gameObject);

			if (_library == null || _library.Songs == null) return;

			foreach (LevelData song in _library.Songs)
			{
				if (song == null) continue;
				LevelData captured = song;

				SongPickerEntry entry = Instantiate(_entryPrefab, _entriesContainer);
				bool exists = CustomLevelStorage.Exists(song.name);
				entry.Bind(song.Title, song.Artist, exists, () =>
				{
					_editor.LoadSong(captured);
					Hide();
				});
			}
		}
	}

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
