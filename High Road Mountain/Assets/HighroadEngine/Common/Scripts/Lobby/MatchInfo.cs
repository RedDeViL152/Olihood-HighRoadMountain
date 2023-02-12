using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MoreMountains.HighroadEngine {

	/// <summary>
	/// Generic MatchEntry for lobby Interface. 
	/// Used by lobby UI to list all matches for the user to join
	/// </summary>
	public class MatchInfo : IGenericMatchInfo 
	{
		public int NetworkId { get; set; }

		public int GameId { get; set; }

		public string GameIdString { get; set; }

		public string Name { get; set; }

		public int CurrentSize { get; set; }

		public int MaxSize { get; set; }
	}
}
