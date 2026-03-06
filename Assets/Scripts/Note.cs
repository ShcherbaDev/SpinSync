using UnityEngine;

public enum HitGrade
{
	None, Miss, Bad, Good, Perfect
}

public class Note : MonoBehaviour
{
	[SerializeField] private float _spawnTime;
	[SerializeField] private float _travelDuration;
	[SerializeField] private float _targetRadius;
	[SerializeField] private Vector2 _direction;

	public float Progress
	{
		get { return (Time.time - _spawnTime) / _travelDuration; }
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

	// TODO: move to somewhere else
	public HitGrade RateHit()
	{
		return Progress switch
		{
			< 0.5f => HitGrade.Miss,
			<= 0.7f => HitGrade.Bad,
			<= 0.85f => HitGrade.Good,
			<= 1.1f => HitGrade.Perfect,
			_ => HitGrade.None
		};
	}

	private void Update()
	{
		transform.position = _direction * (Progress * _targetRadius);

		if (Progress >= 1.2f)
		{
			Debug.Log("Miss (too late)");
			Destroy(gameObject);
		}
	}
}
