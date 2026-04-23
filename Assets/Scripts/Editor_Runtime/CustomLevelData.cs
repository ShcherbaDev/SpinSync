using System;
using System.Collections.Generic;

namespace SpinSync.EditorRuntime
{
	[Serializable]
	public class CustomLevelData
	{
		public int Version = 1;
		public string SongId;
		public List<CustomNote> Notes = new List<CustomNote>();
	}

	[Serializable]
	public class CustomNote
	{
		public float Time;
		public float Angle;
	}
}
