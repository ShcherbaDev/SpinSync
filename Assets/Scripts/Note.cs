using UnityEngine;

public enum NoteGrade
{
	Miss, Bad, Good, Perfect
}

public class Note : MonoBehaviour
{
	[SerializeField] private float _spawnTime;
	[SerializeField] private float _travelDuration;
	[SerializeField] private float _targetRadius;
	[SerializeField] private Vector2 _direction;

	[SerializeField] private SpriteRenderer _spriteRenderer;

	public System.Action<Note, NoteGrade> OnNoteFinished;

	public float Progress
	{
		get { return (Time.time - _spawnTime) / _travelDuration; }
	}

	private void Start()
	{
		// Hide the sprite - it shows up smoothly on changing Progress
		Color transparent = _spriteRenderer.color;
		transparent.a = 0;
		_spriteRenderer.color = transparent;
	}

	public void Init(float travelDuration, float targetRadius, float angleDegrees)
	{
		_travelDuration = travelDuration;
		_targetRadius = targetRadius;
		_spawnTime = Time.time;

		float angleRadian = angleDegrees * Mathf.Deg2Rad;
		_direction = new Vector2(Mathf.Sin(angleRadian), Mathf.Cos(angleRadian)).normalized;

		transform.up = _direction;
	}

	public NoteGrade RateHit()
	{
		return Progress switch
		{
			< 0.6f => NoteGrade.Miss,
			<= 0.7f => NoteGrade.Bad,
			<= 0.85f => NoteGrade.Good,
			<= 1.1f => NoteGrade.Perfect,
			_ => NoteGrade.Miss
		};
	}

	private void Update()
	{
		transform.position = _direction * (Progress * _targetRadius);
		transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, Progress);

		if (_spriteRenderer)
		{
			Color newTransparency = _spriteRenderer.color;
			newTransparency.a = Progress;
			_spriteRenderer.color = newTransparency;
		}

		if (Progress >= 1.2f)
		{
			OnNoteFinished?.Invoke(this, NoteGrade.Miss);
			Destroy(gameObject);
		}
	}
}
