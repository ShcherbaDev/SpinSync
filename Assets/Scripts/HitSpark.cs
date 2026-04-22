using DG.Tweening;
using UnityEngine;

public class HitSpark : MonoBehaviour
{
	[SerializeField] private SpriteRenderer _spriteRenderer;

	[Header("Animation")]
	[SerializeField, Min(0f)] private float _duration = 0.25f;
	[SerializeField] private float _startScale = 0.3f;
	[SerializeField] private float _endScale = 1f;
	[SerializeField] private float _maxSpinDegrees = 90f;
	[SerializeField] private float _maxStartRotation = 30f;
	[SerializeField] private Ease _scaleEase = Ease.OutCubic;

	private Sequence _sequence;

	private void Reset()
	{
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public void Play(Color color, float scaleMultiplier)
	{
		if (!_spriteRenderer)
			_spriteRenderer = GetComponent<SpriteRenderer>();

		_spriteRenderer.color = color;

		transform.localScale = Vector3.one * _startScale;
		transform.localEulerAngles = new Vector3(0f, 0f, Random.Range(-_maxStartRotation, _maxStartRotation));

		float finalScale = _endScale * scaleMultiplier;
		float spin = Random.Range(-_maxSpinDegrees, _maxSpinDegrees);

		_sequence = DOTween.Sequence();
		_sequence.Join(transform.DOScale(finalScale, _duration).SetEase(_scaleEase));
		_sequence.Join(_spriteRenderer.DOFade(0f, _duration));
		_sequence.Join(transform.DORotate(new Vector3(0f, 0f, transform.localEulerAngles.z + spin), _duration, RotateMode.Fast));
		_sequence.OnComplete(() => Destroy(gameObject));
	}

	private void OnDestroy()
	{
		_sequence?.Kill();
	}
}
