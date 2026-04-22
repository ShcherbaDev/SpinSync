using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SongLibrary", menuName = "Song Library")]
public class SongLibrary : ScriptableObject
{
	public List<LevelData> Songs = new List<LevelData>();
}
