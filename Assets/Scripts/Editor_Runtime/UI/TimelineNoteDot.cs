using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpinSync.EditorRuntime.UI
{
	/// <summary>
	/// Companion component on a single dot in the TimelineStrip. Handles click vs drag.
	/// </summary>
	public class TimelineNoteDot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		[SerializeField] private Image _image;

		private TimelineStripUI _strip;
		private int _noteIndex;
		private bool _dragging;
		private Vector2 _pointerDownScreen;

		public RectTransform RectTransform { get { return (RectTransform)transform; } }
		public int NoteIndex { get { return _noteIndex; } }

		public void Bind(TimelineStripUI strip, int index)
		{
			_strip = strip;
			_noteIndex = index;
		}

		public void SetColor(Color color)
		{
			if (_image != null) _image.color = color;
		}

		public void OnPointerDown(PointerEventData ev)
		{
			_dragging = false;
			_pointerDownScreen = ev.position;
		}

		public void OnDrag(PointerEventData ev)
		{
			if (_strip == null) return;

			if (!_dragging)
			{
				if (Vector2.Distance(ev.position, _pointerDownScreen) < 5f)
					return;
				_dragging = true;
			}

			_strip.NotifyDotDrag(_noteIndex, ev.delta.x, _strip.StripRect);
		}

		public void OnPointerUp(PointerEventData ev)
		{
			if (!_dragging && _strip != null)
				_strip.NotifyDotClick(_noteIndex);
			_dragging = false;
		}
	}
}
