

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PUN_2_OR_NEWER
using Photon.Pun;
using Photon.Realtime;
#endif

namespace MoreMountains.HighroadEngine
{
    public class PunLobbyManager :
#if PUN_2_OR_NEWER
    MonoBehaviourPunCallbacks
#else
    MonoBehaviour
#endif
    , IGenericNetworkLobbyManager
    {
        /// List of different games type supported by photon
        public enum GameTypes { TinyCars = 0, Aphex = 1 };
        /// Compatibility version number
        public byte PunGameVersion = 1;
        /// Reference to the current game type
        public GameTypes GameType;
        // Max players value
        public int MaxPlayers = 4;
        /// Name of the lobby scene
        public string LobbyScene = "TinyCarsOnlineLobby";

        [Header("Vehicles configuration")]
        /// the list of vehicles prefabs the player can choose from.
        public GameObject[] AvailableVehiclesPrefab;

        [Header("Tracks configuration")]
        /// the list of Track Scenes names. Used to load scene & show scene name in UI
        public string[] AvailableTracksSceneName;

        /// the list of tracks sprites. Used to show image of chosen track in UI
        public Sprite[] AvailableTracksSprite;
        /// Reference to OnlineLobbyUI object in scene
        public OnlineLobbyUI _onlineLobbyUI;
        // Activate debug logs in console

        protected bool _destroyInstance = false;

        /// <summary>
        /// Initializes the manager
        /// </summary>
        public virtual void Start()
        {
            DontDestroyOnLoad(this.gameObject);

            OnlineLobbyProxy.Instance = this;

            _onlineLobbyUI = GetComponentInChildren<OnlineLobbyUI>();

            // Setup Pun
#if PUN_2_OR_NEWER
            PhotonNetwork.AutomaticallySyncScene = true;
#endif
            // Init UI
            _onlineLobbyUI.ShowLobby();
            _onlineLobbyUI.ShowMainMenu();

            // Register call on scene loaded to destroy this object
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        /// <summary>
        /// Check if players are ready to play
        /// </summary>
        public virtual void CheckLobbyPlayersReadyState()
        {
#if PUN_2_OR_NEWER
            int _playersReadyCount = 0;

            foreach (Player photonPlayer in PhotonNetwork.CurrentRoom.Players.Values)
            {
                if (photonPlayer.CustomProperties.ContainsKey(PunRaceManager.PlayerProperty_LobbyReady_Key) &&
                    photonPlayer.CustomProperties[PunRaceManager.PlayerProperty_LobbyReady_Key] != null)
                {
                    if ((bool)photonPlayer.CustomProperties[PunRaceManager.PlayerProperty_LobbyReady_Key])
                    {
                        _playersReadyCount++;
                    }
                }
            }

            _onlineLobbyUI.UpdateWaitPlayersText(_playersReadyCount, PhotonNetwork.CurrentRoom.PlayerCount);

            if (PhotonNetwork.IsMasterClient)
            {
                if (_playersReadyCount == PhotonNetwork.CurrentRoom.PlayerCount)
                {
                    _onlineLobbyUI.ShowStartGame();
                }
                else
                {
                    _onlineLobbyUI.HideStartGame();
                }
            }
#endif
        }

#if PUN_2_OR_NEWER
        #region PUN implementation

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            _onlineLobbyUI.HidePopup();
            this.RefreshServerList();
        }

        public override void OnJoinedRoom()
        {
            _onlineLobbyUI.HidePopup();

            PhotonNetwork.Instantiate("PunLobbyPlayer", Vector3.zero, Quaternion.identity, 0);

            _onlineLobbyUI.ShowConnected();

            CheckLobbyPlayersReadyState();
        }

        public override void OnLeftRoom()
        {
            LoadingSceneManager.LoadScene(LobbyScene);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            _onlineLobbyUI.RemoveMatchesFromMatchmakingList();

            List<IGenericMatchInfo> list = new List<IGenericMatchInfo>();

            foreach (RoomInfo room in roomList)
            {
                MatchInfo _roominfo = new MatchInfo();
                _roominfo.GameIdString = room.Name;
                _roominfo.Name = "<"+room.Name.Substring(0, 8) + "...>";
                _roominfo.MaxSize = room.MaxPlayers;
                _roominfo.CurrentSize = room.PlayerCount;
                list.Add(_roominfo);
            }

            _onlineLobbyUI.ShowMatchesFromMatchmaking(list);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            CheckLobbyPlayersReadyState();
        }

        #endregion

#endif

        #region IGenericLobbyManager implementation

        int IGenericLobbyManager.MaxPlayers
        {
            get { return this.MaxPlayers; }
        }

        void IGenericLobbyManager.ReturnToLobby()
        {
            // nothing
        }

        void IGenericLobbyManager.ReturnToStartScreen()
        {
            _destroyInstance = true;
            LoadingSceneManager.LoadScene("StartScreen");
        }

        #endregion

        #region IGenericNetworkedLobbyManager implementation

        OnlineLobbyUI IGenericNetworkLobbyManager.OnlineLobbyUIManager
        {
            get { return _onlineLobbyUI; }
        }

        string[] IGenericNetworkLobbyManager.AvailableTracksSceneName
        {
            get { return this.AvailableTracksSceneName; }
        }

        Sprite[] IGenericNetworkLobbyManager.AvailableTracksSprite
        {
            get { return this.AvailableTracksSprite; }
        }

        GameObject[] IGenericNetworkLobbyManager.AvailableVehiclesPrefab
        {
            get { return this.AvailableVehiclesPrefab; }
        }

        bool IGenericNetworkLobbyManager.IsDirectConnectionEnabled()
        {
            return false;
        }

        bool IGenericNetworkLobbyManager.ArePlayersReadyToPlay()
        {

#if PUN_2_OR_NEWER
            if (!PhotonNetwork.InRoom)
            {
                return false;
            }

            if (PhotonNetwork.CurrentRoom.PlayerCount == 0)
            {
                return false;
            }

            Player[] _players = PhotonNetwork.PlayerList;
            foreach (Player player in _players)
            {
                if (!player.CustomProperties.ContainsKey(PunRaceManager.PlayerProperty_Ready_Key) || (bool)player.CustomProperties[PunRaceManager.PlayerProperty_Ready_Key] != true)
                {
                    return false;
                }
            }
#endif
            return true;
        }

        void IGenericNetworkLobbyManager.OnPlayerReadyToBeginChanged()
        {
            CheckLobbyPlayersReadyState();
        }

        void IGenericNetworkLobbyManager.OnMatchmaking()
        {
            _onlineLobbyUI.ShowMatchmaking();
            _onlineLobbyUI.ShowPopup("Connecting...");

#if PUN_2_OR_NEWER
            string _gameVersion = this.GameType.ToString() + "/" + PunGameVersion.ToString();
            // connect
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.ConnectUsingSettings();
#endif
        }

        void IGenericNetworkLobbyManager.JoinMatch(IGenericMatchInfo matchInfo)
        {
#if PUN_2_OR_NEWER
            PhotonNetwork.JoinRoom(matchInfo.GameIdString);
#endif
        }

        void IGenericNetworkLobbyManager.OnDirectConnection()
        {
            Debug.LogError("PUN does not support Direct connection, Consider using Bolt instead");
        }

        void IGenericNetworkLobbyManager.OnClickRefreshServerList()
        {
            RefreshServerList();
        }

        void IGenericNetworkLobbyManager.OnClickCreateMatchmakingGame()
        {
            _onlineLobbyUI.ShowPopup("Creating MatchMaking");
#if PUN_2_OR_NEWER
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = (byte)this.MaxPlayers;
            PhotonNetwork.CreateRoom(null, roomOptions);
#endif
        }

        void IGenericNetworkLobbyManager.OnReturnToMain()
        {
#if PUN_2_OR_NEWER
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }

            _onlineLobbyUI.ShowLobby();
            _onlineLobbyUI.ShowMainMenu();
#endif
        }

        void IGenericNetworkLobbyManager.OnConnectedReturnToMain()
        {
#if PUN_2_OR_NEWER
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
#endif
            _onlineLobbyUI.ShowLobby();
            _onlineLobbyUI.ShowMainMenu();
        }


        void IGenericNetworkLobbyManager.OnStartGame()
        {
#if PUN_2_OR_NEWER
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
            int _trackId = (int)PhotonNetwork.CurrentRoom.CustomProperties[PunRaceManager.RoomProperty_TrackId_Key];
            string trackSceneName = AvailableTracksSceneName[_trackId];

            PhotonNetwork.LoadLevel(trackSceneName);
#endif
        }
        #endregion

        void RefreshServerList()
        {
            Debug.LogWarning("Refresh not necessary on PUN when using default lobby. If you stay on the MasterServer, then yes, you'll need to refresh the room list");
        }

        /// <summary>
        /// We use this event to destroy this object when the scene has changed and the instance must be destroyed
        /// </summary>
        protected virtual void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_destroyInstance)
            {
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
                return;
            }

            string newscene = SceneManager.GetSceneAt(0).name;

            if (newscene == LobbyScene)
            {
                // we go back to lobby scene
                _onlineLobbyUI.ShowLobby();
            }
            else if (PunRaceManager.Instance != null)
            {
                // if the currently loaded scene is an available track scene
                _onlineLobbyUI.HideLobby();
            }
            else
            {
                // we are going back to the start screen or another unknown scene, we kill the LobbyManager

                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}