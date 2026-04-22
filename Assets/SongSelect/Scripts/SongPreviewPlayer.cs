using DG.Tweening;
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
	private LevelData _currentSong;

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

	public void PlayPreview(LevelData song)
	{
		if (song == _currentSong) return;
		_currentSong = song;

		AudioSource oldSource = _activeSource;
		AudioSource newSource = (_activeSource == _sourceA) ? _sourceB : _sourceA;

		oldSource.DOKill();
		oldSource.DOFade(0f, _crossfadeDuration)
			.OnComplete(() => oldSource.Stop());

		newSource.DOKill();
		if (song != null && song.Song != null)
		{
			newSource.clip = song.Song;
			newSource.time = Mathf.Clamp(song.PreviewStartTime, 0f, Mathf.Max(0f, song.Song.length - 0.1f));
			newSource.volume = 0f;
			newSource.Play();
			newSource.DOFade(_targetVolume, _crossfadeDuration);
		}

		_activeSource = newSource;
	}

	public Tween FadeOutAll(float duration)
	{
		Sequence seq = DOTween.Sequence();
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
