using UnityEngine;
using System.Collections;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Stores data about a player in the lobby screen
    /// </summary>
    public interface IGenericLobbyPlayer
    {
        /// Event called when track selected has changed
        void OnTrackChange(int direction);
    }
}