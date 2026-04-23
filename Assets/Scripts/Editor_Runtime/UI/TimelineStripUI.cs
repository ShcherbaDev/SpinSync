using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	/// <summary>
	/// Horizontal strip showing all notes as dots positioned by (Time / SongDuration).
	/// Click a dot to select. Drag a dot horizontally (>5 px) to mutate its time.
	/// </summary>
	public class TimelineStripUI : MonoBehaviour
	{
		private const float DragThresholdPixels = 5f;

		[SerializeField] private LevelEditor _editor;

		[Header("Layout")]
		[SerializeField] private RectTransform _stripRect;
		[SerializeField] private RectTransform _playheadLine;
		[SerializeField] private TimelineNoteDot _dotPrefab;
		[SerializeField] private RectTransform _dotsContainer;
		[SerializeField] private Color _normalDotColor = Color.white;
		[SerializeField] private Color _selectedDotColor = new Color(1f, 0.85f, 0.2f, 1f);

		private readonly List<TimelineNoteDot> _dotPool = new List<TimelineNoteDot>();
		private int _activeDots;

		private void OnEnable()
		{
			if (_editor != null)
			{
				_editor.OnLevelLoaded += Rebuild;
				_editor.OnNotesChanged += Rebuild;
				_editor.OnSelectionChanged += RefreshSelectionColors;
			}
			Rebuild();
		}

		private void OnDisable()
		{
			if (_editor != null)
			{
				_editor.OnLevelLoaded -= Rebuild;
				_editor.OnNotesChanged -= Rebuild;
				_editor.OnSelectionChanged -= RefreshSelectionColors;
			}
		}

		private void Update()
		{
			if (_editor == null || _stripRect == null) return;

			float dur = _editor.SongDuration;
			if (dur <= 0f) return;

			if (_playheadLine != null)
			{
				float w = _stripRect.rect.width;
				float x = (_editor.PlayheadTime / dur) * w;
				Vector2 ap = _playheadLine.anchoredPosition;
				ap.x = x;
				_playheadLine.anchoredPosition = ap;
			}
		}

		public void Rebuild()
		{
			_activeDots = 0;
			if (_editor == null || _dotPrefab == null || _dotsContainer == null || _stripRect == null)
			{
				HideUnused();
				return;
			}

			IReadOnlyList<CustomNote> notes = _editor.Notes;
			float dur = _editor.SongDuration;
			if (dur <= 0f)
			{
				HideUnused();
				return;
			}

			float w = _stripRect.rect.width;

			for (int i = 0; i < notes.Count; i++)
			{
				TimelineNoteDot dot = AcquireDot();
				dot.Bind(this, i);
				float x = (notes[i].Time / dur) * w;
				dot.RectTransform.anchoredPosition = new Vector2(x, 0f);
				dot.SetColor(_editor.SelectedNoteIndex == i ? _selectedDotColor : _normalDotColor);
				dot.gameObject.SetActive(true);
			}

			HideUnused();
		}

		private void RefreshSelectionColors()
		{
			for (int i = 0; i < _activeDots; i++)
			{
				TimelineNoteDot dot = _dotPool[i];
				dot.SetColor(_editor.SelectedNoteIndex == dot.NoteIndex ? _selectedDotColor : _normalDotColor);
			}
		}

		private TimelineNoteDot AcquireDot()
		{
			if (_activeDots < _dotPool.Count)
			{
				TimelineNoteDot existing = _dotPool[_activeDots++];
				return existing;
			}

			TimelineNoteDot fresh = Instantiate(_dotPrefab, _dotsContainer);
			_dotPool.Add(fresh);
			_activeDots++;
			return fresh;
		}

		private void HideUnused()
		{
			for (int i = _activeDots; i < _dotPool.Count; i++)
			{
				if (_dotPool[i] != null)
					_dotPool[i].gameObject.SetActive(false);
			}
		}

		// ---- Called by TimelineNoteDot ----

		public void NotifyDotClick(int index)
		{
			_editor?.SelectNote(index);
		}

		public void NotifyDotDrag(int index, float screenDeltaX, RectTransform stripRect)
		{
			if (_editor == null) return;

			float dur = _editor.SongDuration;
			if (dur <= 0f) return;

			IReadOnlyList<CustomNote> notes = _editor.Notes;
			if (index < 0 || index >= notes.Count) return;

			float w = stripRect.rect.width;
			if (w <= 0f) return;

			float secondsDelta = (screenDeltaX / w) * dur;
			float newTime = notes[index].Time + secondsDelta;

			_editor.SelectNote(index);
			_editor.MoveSelectedNote(newTime, notes[index].Angle);
		}

		public RectTransform StripRect { get { return _stripRect; } }
	}
}
