using UnityEditor;
using UnityEngine;

public static class BPMEstimator
{
	private const float MinBPM = 60f;
	private const float MaxBPM = 200f;

	// Analysis window covered from the start of the clip. Longer = slower, more accurate.
	private const float MaxAnalysisSeconds = 45f;

	// Hop size between energy frames (samples per frame). 1024 at 44.1kHz ≈ 23ms/frame.
	private const int HopSize = 1024;

	public static bool TryEstimate(AudioClip clip, out float bpm, out string error)
	{
		bpm = 0f;
		error = null;

		if (clip == null)
		{
			error = "No audio clip assigned.";
			return false;
		}

		int channels = clip.channels;
		int sampleRate = clip.frequency;
		int totalSamples = Mathf.Min(clip.samples, Mathf.RoundToInt(MaxAnalysisSeconds * sampleRate));

		if (totalSamples <= 0 || sampleRate <= 0)
		{
			error = "Audio clip has no samples.";
			return false;
		}

		float[] raw = new float[totalSamples * channels];
		if (!clip.GetData(raw, 0))
		{
			error = "Could not read samples. Set the clip's Load Type to 'Decompress On Load' and try again.";
			return false;
		}

		float[] mono = DownmixToMono(raw, totalSamples, channels);
		float[] onset = ComputeOnsetEnvelope(mono, HopSize);
		if (onset.Length < 4)
		{
			error = "Audio too short to analyze.";
			return false;
		}

		float framesPerSecond = (float)sampleRate / HopSize;

		try
		{
			EditorUtility.DisplayProgressBar("BPM Estimator", "Analyzing audio...", 0.5f);
			bpm = PickBestBPM(onset, framesPerSecond);
		}
		finally
		{
			EditorUtility.ClearProgressBar();
		}

		if (bpm < MinBPM || bpm > MaxBPM)
		{
			error = $"Could not lock onto a beat. Best guess was {bpm:0.0} BPM.";
			return false;
		}

		return true;
	}

	private static float[] DownmixToMono(float[] raw, int totalSamples, int channels)
	{
		float[] mono = new float[totalSamples];
		if (channels == 1)
		{
			System.Array.Copy(raw, mono, totalSamples);
			return mono;
		}

		for (int i = 0; i < totalSamples; i++)
		{
			float sum = 0f;
			int baseIdx = i * channels;
			for (int c = 0; c < channels; c++)
				sum += raw[baseIdx + c];
			mono[i] = sum / channels;
		}
		return mono;
	}

	private static float[] ComputeOnsetEnvelope(float[] mono, int hopSize)
	{
		int numFrames = mono.Length / hopSize;
		float[] energy = new float[numFrames];

		for (int f = 0; f < numFrames; f++)
		{
			int start = f * hopSize;
			float sum = 0f;
			for (int i = 0; i < hopSize; i++)
			{
				float s = mono[start + i];
				sum += s * s;
			}
			// log-compressed energy emphasizes onsets over sustained loud passages
			energy[f] = Mathf.Log(1f + sum);
		}

		float[] onset = new float[numFrames];
		for (int f = 1; f < numFrames; f++)
			onset[f] = Mathf.Max(0f, energy[f] - energy[f - 1]);

		// Normalize
		float max = 0f;
		for (int f = 0; f < numFrames; f++)
			if (onset[f] > max) max = onset[f];
		if (max > 0f)
			for (int f = 0; f < numFrames; f++) onset[f] /= max;

		return onset;
	}

	private static float PickBestBPM(float[] onset, float framesPerSecond)
	{
		int minLag = Mathf.RoundToInt(60f / MaxBPM * framesPerSecond);
		int maxLag = Mathf.RoundToInt(60f / MinBPM * framesPerSecond);
		maxLag = Mathf.Min(maxLag, onset.Length - 1);

		float bestScore = 0f;
		int bestLag = 0;

		for (int lag = minLag; lag <= maxLag; lag++)
		{
			float score = 0f;
			int count = onset.Length - lag;
			for (int f = 0; f < count; f++)
				score += onset[f] * onset[f + lag];
			score /= count;

			if (score > bestScore)
			{
				bestScore = score;
				bestLag = lag;
			}
		}

		if (bestLag == 0) return 0f;

		// Parabolic interpolation around the peak for sub-frame accuracy
		float refined = bestLag;
		if (bestLag > minLag && bestLag < maxLag)
		{
			float s0 = Autocorrelate(onset, bestLag - 1);
			float s1 = Autocorrelate(onset, bestLag);
			float s2 = Autocorrelate(onset, bestLag + 1);
			float denom = (s0 - 2f * s1 + s2);
			if (Mathf.Abs(denom) > 1e-6f)
				refined = bestLag + 0.5f * (s0 - s2) / denom;
		}

		float bpm = 60f * framesPerSecond / refined;

		// Fold into a tighter musical range if we locked onto a half-time or double-time multiple.
		while (bpm < 90f) bpm *= 2f;
		while (bpm > 180f) bpm *= 0.5f;

		return bpm;
	}

	private static float Autocorrelate(float[] onset, int lag)
	{
		float sum = 0f;
		int count = onset.Length - lag;
		for (int f = 0; f < count; f++)
			sum += onset[f] * onset[f + lag];
		return sum / count;
	}
}
