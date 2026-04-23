using System;
using System.Collections.Generic;

namespace SpinSync.EditorRuntime
{
	/// <summary>
	/// Shared scheduler for the playhead-driven trigger loop.
	/// Used by both the built-in NoteSpawner (Timeline source) and the EditorNoteSpawner (custom JSON source).
	/// </summary>
	public static class NoteScheduler
	{
		/// <summary>
		/// Walks a sorted-by-time list, invoking onTrigger(index) whenever playhead crosses (time[i] - travelDuration).
		/// </summary>
		/// <param name="sortedTriggerTimes">Trigger times sorted ascending.</param>
		/// <param name="nextIndex">Index of the next un-triggered note; pass back the returned value next frame.</param>
		/// <param name="playhead">Current playhead time (seconds).</param>
		/// <param name="travelDuration">How early relative to a note's time it should be triggered (seconds).</param>
		/// <param name="onTrigger">Callback invoked for each note that should fire this tick.</param>
		/// <returns>Updated nextIndex.</returns>
		public static int AdvancePending(IReadOnlyList<float> sortedTriggerTimes, int nextIndex,
			float playhead, float travelDuration, Action<int> onTrigger)
		{
			while (nextIndex < sortedTriggerTimes.Count)
			{
				float triggerAt = sortedTriggerTimes[nextIndex] - travelDuration;
				if (playhead >= triggerAt)
				{
					onTrigger?.Invoke(nextIndex);
					nextIndex++;
				}
				else
				{
					break;
				}
			}
			return nextIndex;
		}

		/// <summary>
		/// Recomputes nextIndex after a scrub. Returns the first index whose trigger time is strictly greater than playhead.
		/// </summary>
		public static int RebaseIndex(IReadOnlyList<float> sortedTriggerTimes, float playhead, float travelDuration)
		{
			int lo = 0;
			int hi = sortedTriggerTimes.Count;
			while (lo < hi)
			{
				int mid = (lo + hi) >> 1;
				float triggerAt = sortedTriggerTimes[mid] - travelDuration;
				if (triggerAt <= playhead)
					lo = mid + 1;
				else
					hi = mid;
			}
			return lo;
		}
	}
}
