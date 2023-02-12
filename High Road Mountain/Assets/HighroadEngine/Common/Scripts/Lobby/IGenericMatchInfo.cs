using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Generic MatchEntry for lobby Interface. 
    /// Used by lobby UI to list all matches a user can join
    /// </summary>
    public interface IGenericMatchInfo
    {
        /// Network identifier
        int NetworkId { get; set; }
        /// Game identifier
        int GameId { get; set; }
        /// Game identifier as a string (for Photon compatibility)
        string GameIdString { get; set; }
        /// Name of the match
        string Name { get; set; }
        /// Number of players ingame
        int CurrentSize { get; set; }
        /// Max number of players in this game
        int MaxSize { get; set; }
    }
}
