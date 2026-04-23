using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	/// <summary>
	/// Side panel shown when a note is selected.
	/// Numeric Time/Angle fields, Delete button, "Set angle from platform" button, ghost sprite at selected angle.
	/// </summary>
	public class NoteInspectorUI : MonoBehaviour
	{
		[SerializeField] private LevelEditor _editor;
		[SerializeField] private Player _player;

		[Header("Panel")]
		[SerializeField] private GameObject _panelRoot;
		[SerializeField] private TMP_InputField _timeField;
		[SerializeField] private TMP_InputField _angleField;
		[SerializeField] private Button _deleteButton;
		[SerializeField] private Button _setAngleFromPlatformButton;

		[Header("Ghost Indicator (selected note position)")]
		[SerializeField] private SpriteRenderer _ghostSprite;
		[SerializeField, Min(0.05f)] private float _ghostAlpha = 0.5f;

		private bool _suppressFieldCallbacks;

		private void OnEnable()
		{
			if (_timeField != null) _timeField.onEndEdit.AddListener(OnTimeFieldEdited);
			if (_angleField != null) _angleField.onEndEdit.AddListener(OnAngleFieldEdited);
			if (_deleteButton != null) _deleteButton.onClick.AddListener(_editor.DeleteSelectedNote);
			if (_setAngleFromPlatformButton != null) _setAngleFromPlatformButton.onClick.AddListener(_editor.SetSelectedAngleFromPlatform);

			if (_editor != null)
			{
				_editor.OnSelectionChanged += Refresh;
				_editor.OnNotesChanged += Refresh;
				_editor.OnModeChanged += Refresh;
			}

			Refresh();
		}

		private void OnDisable()
		{
			if (_timeField != null) _timeField.onEndEdit.RemoveListener(OnTimeFieldEdited);
			if (_angleField != null) _angleField.onEndEdit.RemoveListener(OnAngleFieldEdited);
			if (_deleteButton != null) _deleteButton.onClick.RemoveListener(_editor.DeleteSelectedNote);
			if (_setAngleFromPlatformButton != null) _setAngleFromPlatformButton.onClick.RemoveListener(_editor.SetSelectedAngleFromPlatform);

			if (_editor != null)
			{
				_editor.OnSelectionChanged -= Refresh;
				_editor.OnNotesChanged -= Refresh;
				_editor.OnModeChanged -= Refresh;
			}
		}

		private void Refresh()
		{
			bool show = _editor != null
				&& _editor.Mode == EditorMode.Edit
				&& _editor.SelectedNoteIndex.HasValue
				&& _editor.SelectedNoteIndex.Value < _editor.Notes.Count;

			if (_panelRoot != null) _panelRoot.SetActive(show);

			if (_ghostSprite != null)
				_ghostSprite.gameObject.SetActive(show);

			if (!show) return;

			CustomNote note = _editor.Notes[_editor.SelectedNoteIndex.Value];

			_suppressFieldCallbacks = true;
			if (_timeField != null) _timeField.SetTextWithoutNotify(note.Time.ToString("0.000"));
			if (_angleField != null) _angleField.SetTextWithoutNotify(note.Angle.ToString("0.0"));
			_suppressFieldCallbacks = false;

			if (_ghostSprite != null && _player != null)
			{
				float rad = note.Angle * Mathf.Deg2Rad;
				Vector2 dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
				_ghostSprite.transform.position = (Vector3)(dir * _player.Radius);
				_ghostSprite.transform.up = dir;

				Color c = _ghostSprite.color;
				c.a = _ghostAlpha;
				_ghostSprite.color = c;
			}
		}

		private void OnTimeFieldEdited(string text)
		{
			if (_suppressFieldCallbacks || _editor == null || !_editor.SelectedNoteIndex.HasValue) return;
			if (!float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float t))
				return;

			CustomNote n = _editor.Notes[_editor.SelectedNoteIndex.Value];
			_editor.MoveSelectedNote(t, n.Angle);
		}

		private void OnAngleFieldEdited(string text)
		{
			if (_suppressFieldCallbacks || _editor == null || !_editor.SelectedNoteIndex.HasValue) return;
			if (!float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float a))
				return;

			CustomNote n = _editor.Notes[_editor.SelectedNoteIndex.Value];
			_editor.MoveSelectedNote(n.Time, a);
		}
	}
}
