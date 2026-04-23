using System.Collections.Generic;

namespace SpinSync.EditorRuntime
{
	public interface IEditorCommand
	{
		void Do();
		void Undo();
	}

	/// <summary>
	/// Inserts a note at the position dictated by sorted-by-time order.
	/// On Undo, removes the inserted note (tracked by index after Do).
	/// </summary>
	public class PlaceNoteCommand : IEditorCommand
	{
		private readonly List<CustomNote> _notes;
		private readonly CustomNote _newNote;
		private int _insertedIndex;

		public int InsertedIndex
		{
			get { return _insertedIndex; }
		}

		public PlaceNoteCommand(List<CustomNote> notes, CustomNote newNote)
		{
			_notes = notes;
			_newNote = newNote;
		}

		public void Do()
		{
			_insertedIndex = SortedListOps.InsertSortedByTime(_notes, _newNote);
		}

		public void Undo()
		{
			if (_insertedIndex >= 0 && _insertedIndex < _notes.Count && _notes[_insertedIndex] == _newNote)
				_notes.RemoveAt(_insertedIndex);
		}
	}

	public class DeleteNoteCommand : IEditorCommand
	{
		private readonly List<CustomNote> _notes;
		private readonly CustomNote _removed;
		private readonly int _originalIndex;

		public DeleteNoteCommand(List<CustomNote> notes, int index)
		{
			_notes = notes;
			_originalIndex = index;
			_removed = notes[index];
		}

		public void Do()
		{
			_notes.RemoveAt(_originalIndex);
		}

		public void Undo()
		{
			_notes.Insert(_originalIndex, _removed);
		}
	}

	/// <summary>
	/// Modifies time and/or angle of an existing note. Maintains sort order.
	/// Tracks the new index after Do so Undo can restore the original (time, angle, position).
	/// </summary>
	public class MoveNoteCommand : IEditorCommand
	{
		private readonly List<CustomNote> _notes;
		private readonly int _oldIndex;
		private readonly float _oldTime;
		private readonly float _oldAngle;
		private readonly float _newTime;
		private readonly float _newAngle;
		private int _newIndex;

		public int NewIndex
		{
			get { return _newIndex; }
		}

		public MoveNoteCommand(List<CustomNote> notes, int index, float newTime, float newAngle)
		{
			_notes = notes;
			_oldIndex = index;
			_oldTime = notes[index].Time;
			_oldAngle = notes[index].Angle;
			_newTime = newTime;
			_newAngle = newAngle;
		}

		public void Do()
		{
			CustomNote n = _notes[_oldIndex];
			_notes.RemoveAt(_oldIndex);
			n.Time = _newTime;
			n.Angle = _newAngle;
			_newIndex = SortedListOps.InsertSortedByTime(_notes, n);
		}

		public void Undo()
		{
			CustomNote n = _notes[_newIndex];
			_notes.RemoveAt(_newIndex);
			n.Time = _oldTime;
			n.Angle = _oldAngle;
			_notes.Insert(_oldIndex, n);
		}
	}

	internal static class SortedListOps
	{
		public static int InsertSortedByTime(List<CustomNote> notes, CustomNote item)
		{
			int lo = 0;
			int hi = notes.Count;
			while (lo < hi)
			{
				int mid = (lo + hi) >> 1;
				if (notes[mid].Time <= item.Time)
					lo = mid + 1;
				else
					hi = mid;
			}
			notes.Insert(lo, item);
			return lo;
		}
	}

	/// <summary>
	/// Bounded undo/redo stack. Capped at MaxDepth entries; oldest evicted when exceeded.
	/// </summary>
	public class EditorCommandStack
	{
		private const int MaxDepth = 100;

		private readonly LinkedList<IEditorCommand> _undo = new LinkedList<IEditorCommand>();
		private readonly Stack<IEditorCommand> _redo = new Stack<IEditorCommand>();

		public bool CanUndo
		{
			get { return _undo.Count > 0; }
		}

		public bool CanRedo
		{
			get { return _redo.Count > 0; }
		}

		public void Execute(IEditorCommand command)
		{
			command.Do();
			_undo.AddLast(command);
			while (_undo.Count > MaxDepth)
				_undo.RemoveFirst();
			_redo.Clear();
		}

		public IEditorCommand Undo()
		{
			if (_undo.Count == 0)
				return null;

			IEditorCommand command = _undo.Last.Value;
			_undo.RemoveLast();
			command.Undo();
			_redo.Push(command);
			return command;
		}

		public IEditorCommand Redo()
		{
			if (_redo.Count == 0)
				return null;

			IEditorCommand command = _redo.Pop();
			command.Do();
			_undo.AddLast(command);
			return command;
		}

		public void Clear()
		{
			_undo.Clear();
			_redo.Clear();
		}
	}
}
