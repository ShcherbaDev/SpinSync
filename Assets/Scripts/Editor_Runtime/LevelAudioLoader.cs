using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SpinSync.EditorRuntime
{
	/// <summary>
	/// Loads level audio (song.mp3) from disk asynchronously. Works for both user
	/// persistentDataPath levels and shipped StreamingAssets levels on standalone platforms.
	/// </summary>
	public static class LevelAudioLoader
	{
		public delegate void AudioClipCallback(AudioClip clip);

		public static IEnumerator Load(string songId, AudioClipCallback onDone)
		{
			string path = LevelStorage.AudioPath(songId);
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogWarning($"[LevelAudioLoader] No audio file found for level '{songId}'.");
				onDone?.Invoke(null);
				yield break;
			}

			string uri = PathToFileUri(path);
			using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
			{
				DownloadHandlerAudioClip dh = req.downloadHandler as DownloadHandlerAudioClip;
				if (dh != null) dh.streamAudio = false;

				yield return req.SendWebRequest();

				if (req.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError($"[LevelAudioLoader] Failed to load '{path}': {req.error}");
					onDone?.Invoke(null);
					yield break;
				}

				AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
				if (clip != null) clip.name = songId;
				onDone?.Invoke(clip);
			}
		}

		/// <summary>Convenience: load the clip into the Level.AudioClip field.</summary>
		public static IEnumerator LoadInto(Level level)
		{
			if (level == null) yield break;
			yield return Load(level.SongId, c => level.AudioClip = c);
		}

		private static string PathToFileUri(string absolutePath)
		{
			string normalized = absolutePath.Replace('\\', '/');
			if (!normalized.StartsWith("/")) normalized = "/" + normalized;
			return "file://" + normalized;
		}
	}
}
