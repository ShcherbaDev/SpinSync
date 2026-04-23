using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Displays the post-song summary from <see cref="ResultsData"/> and offers a button back to LevelSelect.
/// </summary>
public class ResultsScreen : MonoBehaviour
{
	[Header("Song")]
	[SerializeField] private TMP_Text _songTitleText;
	[SerializeField] private TMP_Text _songArtistText;

	[Header("Stats")]
	[SerializeField] private TMP_Text _totalScoreText;
	[SerializeField] private TMP_Text _bestComboText;
	[SerializeField] private TMP_Text _perfectCountText;
	[SerializeField] private TMP_Text _goodCountText;
	[SerializeField] private TMP_Text _badCountText;
	[SerializeField] private TMP_Text _missCountText;

	[Header("Navigation")]
	[SerializeField] private Button _backButton;
	[SerializeField] private string _levelSelectSceneName = "LevelSelect";

	private void Awake()
	{
		if (_backButton != null)
			_backButton.onClick.AddListener(GoToLevelSelect);
	}

	private void OnDestroy()
	{
		if (_backButton != null)
			_backButton.onClick.RemoveListener(GoToLevelSelect);
	}

	private void Start()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		if (!ResultsData.HasResults)
		{
			Debug.LogWarning("[ResultsScreen] No ResultsData set; showing zeros.");
		}

		if (_songTitleText) _songTitleText.text = string.IsNullOrEmpty(ResultsData.SongTitle) ? "(song)" : ResultsData.SongTitle;
		if (_songArtistText) _songArtistText.text = string.IsNullOrEmpty(ResultsData.SongArtist) ? "" : ResultsData.SongArtist;

		if (_totalScoreText) _totalScoreText.text = ResultsData.TotalScore.ToString();
		if (_bestComboText) _bestComboText.text = $"{ResultsData.BestCombo}x";
		if (_perfectCountText) _perfectCountText.text = ResultsData.PerfectCount.ToString();
		if (_goodCountText) _goodCountText.text = ResultsData.GoodCount.ToString();
		if (_badCountText) _badCountText.text = ResultsData.BadCount.ToString();
		if (_missCountText) _missCountText.text = ResultsData.MissCount.ToString();

		ResultsData.Clear();
	}

	private void GoToLevelSelect()
	{
		SceneManager.LoadScene(_levelSelectSceneName);
	}
}
