using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpinSync.EditorRuntime
{
	/// <summary>
	/// Self-contained level: metadata + notes. Persisted as level.json alongside song.mp3
	/// inside a per-song folder (both in StreamingAssets/Levels/&lt;id&gt;/ and persistentDataPath/Levels/&lt;id&gt;/).
	/// Replaces the former LevelData ScriptableObject and CustomLevelData JSON.
	/// </summary>
	[Serializable]
	public class Level
	{
		public int Version = 1;

		[Header("Identity")]
		public string SongId;
		public string Title;
		public string Artist;

		[Header("Gameplay")]
		[Min(1f)] public float BPM = 120f;
		[Range(1, 5)] public int Difficulty = 1;
		public Color AccentColor = new Color(0.4f, 0.7f, 1f, 1f);
		[Min(0f)] public float PreviewStartTime;
		public float NoteTravelDuration = 0.5f;

		public List<CustomNote> Notes = new List<CustomNote>();

		/// <summary>Folder the level was loaded from (null for freshly-constructed instances). Not serialized.</summary>
		[NonSerialized] public string FolderPath;

		/// <summary>Audio clip loaded from song.mp3 in FolderPath. Populated by LevelAudioLoader. Not serialized.</summary>
		[NonSerialized] public AudioClip AudioClip;
	}

	[Serializable]
	public class CustomNote
	{
		public float Time;
		public float Angle;
	}
}
