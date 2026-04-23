#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpinSync.EditorRuntime.EditorTools
{
	/// <summary>
	/// Copies user-authored level folders from persistentDataPath/Levels/ into
	/// Assets/StreamingAssets/Levels/ so they ship with the build as defaults for all players.
	/// </summary>
	public static class LevelBaker
	{
		[MenuItem("Tools/SpinSync/Bake Levels to StreamingAssets")]
		public static void Bake()
		{
			string src = LevelStorage.UserRoot;
			string dst = LevelStorage.ShippedRoot;

			if (!Directory.Exists(src) || Directory.GetDirectories(src).Length == 0)
			{
				EditorUtility.DisplayDialog(
					"Bake Levels",
					$"No level folders found in:\n{src}\n\nCreate levels in the Level Editor first.",
					"OK");
				return;
			}

			Directory.CreateDirectory(dst);

			int copiedFolders = 0, copiedFiles = 0, skipped = 0;
			foreach (string srcFolder in Directory.GetDirectories(src))
			{
				string songId = Path.GetFileName(srcFolder);
				if (string.IsNullOrEmpty(songId)) continue;

				string dstFolder = Path.Combine(dst, songId);
				Directory.CreateDirectory(dstFolder);

				foreach (string file in Directory.GetFiles(srcFolder))
				{
					if (file.EndsWith(".meta")) continue;
					try
					{
						File.Copy(file, Path.Combine(dstFolder, Path.GetFileName(file)), overwrite: true);
						copiedFiles++;
					}
					catch (System.Exception e)
					{
						Debug.LogError($"[LevelBaker] Failed to copy '{file}': {e.Message}");
						skipped++;
					}
				}
				copiedFolders++;
			}

			AssetDatabase.Refresh();

			string msg = $"Baked {copiedFolders} level folder(s) / {copiedFiles} file(s) to:\n{dst}";
			if (skipped > 0) msg += $"\n\nSkipped {skipped} (see console).";
			EditorUtility.DisplayDialog("Bake Levels", msg, "OK");
			Debug.Log($"[LevelBaker] Baked {copiedFolders} folders / {copiedFiles} files from '{src}' -> '{dst}'.");
		}

		[MenuItem("Tools/SpinSync/Reveal Shipped Levels Folder")]
		public static void RevealShipped()
		{
			string dst = LevelStorage.ShippedRoot;
			Directory.CreateDirectory(dst);
			EditorUtility.RevealInFinder(dst);
		}

		[MenuItem("Tools/SpinSync/Reveal User Levels Folder")]
		public static void RevealUser()
		{
			string src = LevelStorage.UserRoot;
			Directory.CreateDirectory(src);
			EditorUtility.RevealInFinder(src);
		}
	}
}
#endif
