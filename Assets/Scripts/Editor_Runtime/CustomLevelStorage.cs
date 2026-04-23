using System.IO;
using UnityEngine;

namespace SpinSync.EditorRuntime
{
	public static class CustomLevelStorage
	{
		private const string SubDirectory = "CustomLevels";
		private const string FileExtension = ".json";

		public static string DirectoryPath
		{
			get { return Path.Combine(Application.persistentDataPath, SubDirectory); }
		}

		public static string FilePathFor(string songId)
		{
			return Path.Combine(DirectoryPath, songId + FileExtension);
		}

		public static bool Exists(string songId)
		{
			if (string.IsNullOrEmpty(songId))
				return false;

			return File.Exists(FilePathFor(songId));
		}

		public static void Save(CustomLevelData data)
		{
			if (data == null || string.IsNullOrEmpty(data.SongId))
			{
				Debug.LogError("[CustomLevelStorage] Cannot save: data is null or SongId is empty.");
				return;
			}

			Directory.CreateDirectory(DirectoryPath);

			string json = JsonUtility.ToJson(data, prettyPrint: true);
			File.WriteAllText(FilePathFor(data.SongId), json);
		}

		public static CustomLevelData Load(string songId)
		{
			if (!Exists(songId))
				return null;

			try
			{
				string json = File.ReadAllText(FilePathFor(songId));
				CustomLevelData data = JsonUtility.FromJson<CustomLevelData>(json);

				// Defensive: JsonUtility leaves null lists as null
				if (data != null && data.Notes == null)
					data.Notes = new System.Collections.Generic.List<CustomNote>();

				return data;
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[CustomLevelStorage] Failed to load '{songId}': {e.Message}");
				return null;
			}
		}
	}
}
