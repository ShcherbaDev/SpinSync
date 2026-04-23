using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpinSync.EditorRuntime
{
	/// <summary>
	/// Folder-based level persistence. Each level lives in its own directory containing
	/// level.json (metadata + notes) and song.mp3 (audio).
	///   - User authored: {Application.persistentDataPath}/Levels/&lt;SongId&gt;/
	///   - Shipped:       {Application.streamingAssetsPath}/Levels/&lt;SongId&gt;/
	/// Load() prefers the user folder and falls back to shipped. Save() always writes to user.
	/// </summary>
	public static class LevelStorage
	{
		private const string LevelsSubDirectory = "Levels";
		private const string LevelFileName = "level.json";
		private const string AudioFileName = "song.mp3";

		public static string UserRoot
		{
			get { return Path.Combine(Application.persistentDataPath, LevelsSubDirectory); }
		}

		public static string ShippedRoot
		{
			get { return Path.Combine(Application.streamingAssetsPath, LevelsSubDirectory); }
		}

		public static string UserFolder(string songId)
		{
			return Path.Combine(UserRoot, songId);
		}

		public static string ShippedFolder(string songId)
		{
			return Path.Combine(ShippedRoot, songId);
		}

		/// <summary>Returns the folder (user override first, then shipped) that contains a level.json for this id, or null.</summary>
		public static string ResolveFolder(string songId)
		{
			if (string.IsNullOrEmpty(songId)) return null;
			string u = UserFolder(songId);
			if (File.Exists(Path.Combine(u, LevelFileName))) return u;
			string s = ShippedFolder(songId);
			if (File.Exists(Path.Combine(s, LevelFileName))) return s;
			return null;
		}

		public static bool Exists(string songId)
		{
			return ResolveFolder(songId) != null;
		}

		public static bool UserOverrideExists(string songId)
		{
			if (string.IsNullOrEmpty(songId)) return false;
			return File.Exists(Path.Combine(UserFolder(songId), LevelFileName));
		}

		/// <summary>Path to the audio file for this level (user or shipped). Returns null if missing.</summary>
		public static string AudioPath(string songId)
		{
			string folder = ResolveFolder(songId);
			if (folder == null) return null;
			string p = Path.Combine(folder, AudioFileName);
			return File.Exists(p) ? p : null;
		}

		/// <summary>Enumerates all level SongIds visible to the game (shipped + user override). Duplicates deduped.</summary>
		public static IReadOnlyList<string> ListAll()
		{
			HashSet<string> seen = new HashSet<string>();
			List<string> ordered = new List<string>();

			AddIds(ShippedRoot, seen, ordered);
			AddIds(UserRoot, seen, ordered);

			return ordered;
		}

		private static void AddIds(string root, HashSet<string> seen, List<string> ordered)
		{
			if (!Directory.Exists(root)) return;
			foreach (string dir in Directory.EnumerateDirectories(root))
			{
				string id = Path.GetFileName(dir);
				if (string.IsNullOrEmpty(id)) continue;
				if (!File.Exists(Path.Combine(dir, LevelFileName))) continue;
				if (seen.Add(id)) ordered.Add(id);
			}
		}

		public static Level Load(string songId)
		{
			string folder = ResolveFolder(songId);
			if (folder == null) return null;

			string jsonPath = Path.Combine(folder, LevelFileName);
			try
			{
				string json = File.ReadAllText(jsonPath);
				Level level = JsonUtility.FromJson<Level>(json);
				if (level == null) return null;

				// Defensive: JsonUtility leaves null lists as null.
				if (level.Notes == null) level.Notes = new List<CustomNote>();
				level.FolderPath = folder;
				// Ensure SongId matches the folder name (folder is authoritative for identity).
				if (string.IsNullOrEmpty(level.SongId)) level.SongId = songId;
				return level;
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[LevelStorage] Failed to load level '{songId}' at '{jsonPath}': {e.Message}");
				return null;
			}
		}

		public static void Save(Level level)
		{
			if (level == null || string.IsNullOrEmpty(level.SongId))
			{
				Debug.LogError("[LevelStorage] Cannot save: level null or SongId empty.");
				return;
			}

			string folder = UserFolder(level.SongId);
			Directory.CreateDirectory(folder);
			string json = JsonUtility.ToJson(level, prettyPrint: true);
			File.WriteAllText(Path.Combine(folder, LevelFileName), json);

			// Mirror the shipped mp3 into the user folder the first time the user edits the level,
			// so reopening the level (which now resolves to UserFolder) still finds the audio.
			string userAudio = Path.Combine(folder, AudioFileName);
			if (!File.Exists(userAudio))
			{
				string shippedAudio = Path.Combine(ShippedFolder(level.SongId), AudioFileName);
				if (File.Exists(shippedAudio))
				{
					try { File.Copy(shippedAudio, userAudio); }
					catch (System.Exception e)
					{
						Debug.LogWarning($"[LevelStorage] Could not mirror shipped mp3 into user folder: {e.Message}");
					}
				}
			}

			level.FolderPath = folder;
		}
	}
}
