using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	/// <summary>
	/// Modal at scene start that lists every level under StreamingAssets/Levels/ (plus any user
	/// overrides in persistentDataPath) and loads one into the LevelEditor.
	/// </summary>
	public class EditorSongPickerUI : MonoBehaviour
	{
		[SerializeField] private LevelEditor _editor;

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

			IReadOnlyList<string> ids = LevelStorage.ListAll();
			foreach (string id in ids)
			{
				Level level = LevelStorage.Load(id);
				if (level == null) continue;

				string capturedId = id;
				SongPickerEntry entry = Instantiate(_entryPrefab, _entriesContainer);
				bool hasUserOverride = LevelStorage.UserOverrideExists(id);
				entry.Bind(level.Title, level.Artist, hasUserOverride, () =>
				{
					_editor.LoadSong(capturedId);
					Hide();
				});
			}
		}
	}
}
