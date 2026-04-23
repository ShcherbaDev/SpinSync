using TMPro;
using UnityEngine;

namespace SpinSync.EditorRuntime
{
	/// <summary>
	/// Slim score/combo HUD for Test mode in the LevelEditor scene.
	/// Subscribes to Player.OnHitDetected, no lives, no game-over.
	/// </summary>
	public class EditorTestSession : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private Player _player;
		[SerializeField] private TextMeshProUGUI _scoreText;
		[SerializeField] private TextMeshProUGUI _comboText;

		[Header("Scoring")]
		[SerializeField] private int _missScore;
		[SerializeField] private int _badScore = 50;
		[SerializeField] private int _goodScore = 100;
		[SerializeField] private int _perfectScore = 200;

		private int _score;
		private int _combo;

		public void ResetSession()
		{
			_score = 0;
			_combo = 0;
			Refresh();
		}

		private void OnEnable()
		{
			if (_player != null)
				_player.OnHitDetected += HandleHit;

			ResetSession();
		}

		private void OnDisable()
		{
			if (_player != null)
				_player.OnHitDetected -= HandleHit;
		}

		private void HandleHit(Note note, NoteGrade grade)
		{
			if (grade == NoteGrade.Miss)
				_combo = 0;
			else
				_combo++;

			_score += GradeToScore(grade);
			Refresh();
		}

		private int GradeToScore(NoteGrade grade)
		{
			switch (grade)
			{
				case NoteGrade.Perfect: return _perfectScore;
				case NoteGrade.Good: return _goodScore;
				case NoteGrade.Bad: return _badScore;
				default: return _missScore;
			}
		}

		private void Refresh()
		{
			if (_scoreText != null)
				_scoreText.text = $"Score: {_score}";
			if (_comboText != null)
				_comboText.text = $"{_combo}x";
		}
	}
}
