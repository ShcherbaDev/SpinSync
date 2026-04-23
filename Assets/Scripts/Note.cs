using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum NoteGrade
{
	Miss, Bad, Good, Perfect
}

public class Note : MonoBehaviour
{
	private const float AutoMissBuffer = 0.1f;

	[SerializeField] private float _spawnTime;
	[SerializeField] private float _travelDuration;
	[SerializeField] private float _targetRadius;
	[SerializeField] private Vector2 _direction;

	[SerializeField] private SpriteRenderer _outlineSpriteRenderer;
	[SerializeField] private SpriteRenderer _backgroundSpriteRenderer;

	private List<NoteGradeToScoreMapping> _gradeWindows;
	private float _autoMissProgress = 1.2f;

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

	public void SetComboVisuals(Color color)
	{
		if (_backgroundSpriteRenderer)
			_backgroundSpriteRenderer.color = color;
	}

	public void SetGradeWindows(List<NoteGradeToScoreMapping> windows)
	{
		_gradeWindows = windows;

		float maxProgress = 0f;
		if (windows != null)
		{
			foreach (NoteGradeToScoreMapping m in windows)
			{
				if (m.Grade == NoteGrade.Miss) continue;
				if (m.MaxProgress > maxProgress) maxProgress = m.MaxProgress;
			}
		}
		_autoMissProgress = maxProgress > 0f ? maxProgress + AutoMissBuffer : 1.2f;
	}

	public NoteGrade RateHit()
	{
		if (_gradeWindows == null) return NoteGrade.Miss;

		float progress = Progress;
		foreach (NoteGradeToScoreMapping m in _gradeWindows)
		{
			if (m.Grade == NoteGrade.Miss) continue;
			if (progress >= m.MinProgress && progress <= m.MaxProgress)
				return m.Grade;
		}
		return NoteGrade.Miss;
	}

	private void Update()
	{
		transform.position = _direction * (Progress * _targetRadius);
		transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, Progress);

		SetSpriteAlpha(_outlineSpriteRenderer, Progress);
		SetSpriteAlpha(_backgroundSpriteRenderer, Progress);

		if (Progress >= _autoMissProgress)
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
