/// <summary>
/// Static hand-off from Gameplay to the Results scene. Populated by Gameplay right before
/// loading "Results". Cleared after consumption.
/// </summary>
public static class ResultsData
{
	public static bool HasResults;
	public static string SongTitle;
	public static string SongArtist;
	public static int TotalScore;
	public static int BestCombo;
	public static int PerfectCount;
	public static int GoodCount;
	public static int BadCount;
	public static int MissCount;

	public static void Clear()
	{
		HasResults = false;
		SongTitle = null;
		SongArtist = null;
		TotalScore = 0;
		BestCombo = 0;
		PerfectCount = 0;
		GoodCount = 0;
		BadCount = 0;
		MissCount = 0;
	}
}
