using DG.Tweening;
using SpinSync.EditorRuntime;
using UnityEngine;

public class SongPreviewPlayer : MonoBehaviour
{
	[SerializeField, Range(0f, 1f)] private float _targetVolume = 0.6f;
	[SerializeField, Min(0f)] private float _crossfadeDuration = 0.3f;
	[SerializeField, Min(1f), Tooltip("How many seconds of the clip to loop before restarting at PreviewStartTime")]
	private float _previewLoopLength = 20f;

	private AudioSource _sourceA;
	private AudioSource _sourceB;
	private AudioSource _activeSource;
	private Level _currentSong;

	public float TargetVolume => _targetVolume;

	private void Awake()
	{
		_sourceA = CreateSource("PreviewSourceA");
		_sourceB = CreateSource("PreviewSourceB");
		_activeSource = _sourceA;
	}

	private AudioSource CreateSource(string sourceName)
	{
		GameObject go = new GameObject(sourceName);
		go.transform.SetParent(transform, false);
		AudioSource src = go.AddComponent<AudioSource>();
		src.playOnAwake = false;
		src.loop = true;
		src.volume = 0f;
		return src;
	}

	public void PlayPreview(Level song)
	{
		if (song == _currentSong) return;
		_currentSong = song;

		AudioSource oldSource = _activeSource;
		AudioSource newSource = (_activeSource == _sourceA) ? _sourceB : _sourceA;

		oldSource.DOKill();
		oldSource.DOFade(0f, _crossfadeDuration)
			.SetLink(oldSource.gameObject)
			.OnComplete(() => { if (oldSource) oldSource.Stop(); });

		newSource.DOKill();
		if (song != null && song.AudioClip != null)
		{
			newSource.clip = song.AudioClip;
			newSource.time = Mathf.Clamp(song.PreviewStartTime, 0f, Mathf.Max(0f, song.AudioClip.length - 0.1f));
			newSource.volume = 0f;
			newSource.Play();
			newSource.DOFade(_targetVolume, _crossfadeDuration).SetLink(newSource.gameObject);
		}
		else if (song != null)
		{
			// Clip not loaded yet — queue the preview to start once loading finishes.
			StartCoroutine(StartWhenClipReady(song, newSource));
		}

		_activeSource = newSource;
	}

	private System.Collections.IEnumerator StartWhenClipReady(Level song, AudioSource targetSource)
	{
		if (song.AudioClip == null)
			yield return LevelAudioLoader.LoadInto(song);

		if (_currentSong != song || song.AudioClip == null) yield break;
		if (targetSource == null || targetSource != _activeSource) yield break;

		targetSource.clip = song.AudioClip;
		targetSource.time = Mathf.Clamp(song.PreviewStartTime, 0f, Mathf.Max(0f, song.AudioClip.length - 0.1f));
		targetSource.volume = 0f;
		targetSource.Play();
		targetSource.DOFade(_targetVolume, _crossfadeDuration).SetLink(targetSource.gameObject);
	}

	public Tween FadeOutAll(float duration)
	{
		Sequence seq = DOTween.Sequence().SetLink(gameObject);
		if (_sourceA && _sourceA.isPlaying) seq.Join(_sourceA.DOFade(0f, duration));
		if (_sourceB && _sourceB.isPlaying) seq.Join(_sourceB.DOFade(0f, duration));
		return seq;
	}

	private void Update()
	{
		if (_currentSong == null || _activeSource == null || !_activeSource.isPlaying) return;
		if (_activeSource.clip == null) return;

		float endTime = _currentSong.PreviewStartTime + _previewLoopLength;
		if (endTime >= _activeSource.clip.length) return;

		if (_activeSource.time >= endTime)
			_activeSource.time = _currentSong.PreviewStartTime;
	}
}
