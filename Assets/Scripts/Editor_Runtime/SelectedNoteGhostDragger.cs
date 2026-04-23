using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SpinSync.EditorRuntime
{
	/// <summary>
	/// Drag anywhere in world space (not on UI) to change the selected note's angle.
	/// The ghost follows the mouse direction from the platform center.
	/// Only the angle changes — time is preserved.
	/// </summary>
	public class SelectedNoteGhostDragger : MonoBehaviour
	{
		[SerializeField] private LevelEditor _editor;
		[SerializeField] private Player _player;
		[SerializeField] private Camera _camera;
		[SerializeField, Tooltip("Minimum world distance from platform center before drag updates apply (avoids wild flips at origin).")]
		private float _deadZoneWorld = 0.3f;

		private bool _dragging;

		private void Reset()
		{
			if (_camera == null) _camera = Camera.main;
		}

		private void OnEnable()
		{
			_dragging = false;
		}

		private void Update()
		{
			if (_editor == null || _editor.Mode != EditorMode.Edit || !_editor.SelectedNoteIndex.HasValue)
			{
				_dragging = false;
				return;
			}

			Mouse mouse = Mouse.current;
			if (mouse == null) return;

			Camera cam = _camera != null ? _camera : Camera.main;
			if (cam == null) return;

			if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
				_dragging = true;

			if (!_dragging) return;

			if (mouse.leftButton.isPressed)
			{
				Vector2 screen = mouse.position.ReadValue();
				Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
				Vector2 dir = new Vector2(world.x, world.y);
				if (dir.sqrMagnitude < _deadZoneWorld * _deadZoneWorld) return;

				float angleDeg = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
				if (angleDeg < 0f) angleDeg += 360f;

				int idx = _editor.SelectedNoteIndex.Value;
				var notes = _editor.Notes;
				if (idx < 0 || idx >= notes.Count) return;
				_editor.MoveSelectedNote(notes[idx].Time, angleDeg);
			}

			if (mouse.leftButton.wasReleasedThisFrame)
				_dragging = false;
		}

		private static bool IsPointerOverUI()
		{
			EventSystem es = EventSystem.current;
			if (es == null) return false;
			Mouse mouse = Mouse.current;
			if (mouse == null) return es.IsPointerOverGameObject();
			PointerEventData ped = new PointerEventData(es) { position = mouse.position.ReadValue() };
			var results = new System.Collections.Generic.List<RaycastResult>();
			es.RaycastAll(ped, results);
			return results.Count > 0;
		}
	}
}
