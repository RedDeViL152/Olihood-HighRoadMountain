using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Generic network lobby manager interface. 
    /// Used by the race manager to get info about the players and send commands to the current Lobby Manager
    /// </summary>
    public interface IGenericNetworkLobbyManager : IGenericLobbyManager
    {
        /// Reference to OnlineLobbyManager UI gameobject
        OnlineLobbyUI OnlineLobbyUIManager { get; }
        /// List of available tracks name
        string[] AvailableTracksSceneName { get; }
        /// List of available tracks sprites (image of the track)
        Sprite[] AvailableTracksSprite { get; }
        /// List of available vehicles the user can choose from
        GameObject[] AvailableVehiclesPrefab { get; }
        /// Returns availability of direct connection option
        bool IsDirectConnectionEnabled();
        /// Returns true when game can start
        bool ArePlayersReadyToPlay();
        /// Called when player is changing its ready status
        void OnPlayerReadyToBeginChanged();
        /// Callback for matchmaking
        void OnMatchmaking();
        /// Join match event
        void JoinMatch(IGenericMatchInfo matchInfo);
        /// Direct connection event
        void OnDirectConnection();
        /// Called when user refresh server list
        void OnClickRefreshServerList();
        /// Called when user create a new matchmaking game
        void OnClickCreateMatchmakingGame();
        /// Return to main callback
        void OnReturnToMain();
        /// Return to main callback with user already connected
        void OnConnectedReturnToMain();
        /// Callback when game is starting
        void OnStartGame();
    }
}
