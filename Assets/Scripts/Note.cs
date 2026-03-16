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

	[SerializeField] private SpriteRenderer _outlineSpriteRenderer;
	[SerializeField] private SpriteRenderer _backgroundSpriteRenderer;

	public System.Action<Note, NoteGrade> OnNoteFinished;

	public float Progress
	{
		get { return (Time.time - _spawnTime) / _travelDuration; }
	}

	private void Start()
	{
		// Hide the sprites - they show up smoothly on changing Progress
		SetSpriteAlpha(_outlineSpriteRenderer, 0);
		SetSpriteAlpha(_backgroundSpriteRenderer, 0);
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

		SetSpriteAlpha(_outlineSpriteRenderer, Progress);
		SetSpriteAlpha(_backgroundSpriteRenderer, Progress);

		if (Progress >= 1.2f)
		{
			OnNoteFinished?.Invoke(this, NoteGrade.Miss);
			Destroy(gameObject);
		}
	}

	private void SetSpriteAlpha(SpriteRenderer spriteRenderer, float alpha)
	{
		if (!spriteRenderer)
			return;

		Color newColor = spriteRenderer.color;
		newColor.a = alpha;
		spriteRenderer.color = newColor;
	}
}
