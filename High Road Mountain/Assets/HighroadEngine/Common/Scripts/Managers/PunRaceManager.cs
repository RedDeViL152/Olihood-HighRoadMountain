

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Tools;

#if PUN_2_OR_NEWER
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon;
using Photon.Realtime;
#endif

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// If the game is an online PUN Game, this class overrides methods from Race Manager to manage specific 
    /// online parts.
    /// In case of local mode, this class is automatically disabled.
    /// </summary>
    public class PunRaceManager : MonoBehaviour
    {
        public static PunRaceManager Instance;

        public const string PlayerProperty_Ready_Key = "Rdy";
        public const string PlayerProperty_CarId_Key = "cId";
        public const string PlayerProperty_LobbyReady_Key = "Lrdy";
        public const string RoomProperty_TrackId_Key = "tId";

        protected const byte DisableControlForPlayers_EventCode = 0;
        protected const byte EnableControlForPlayers_EventCode = 1;
        protected const byte StartCountDown_EventCode = 2;
        protected const byte ShowEndGameScreen_EventCode = 3;

#if PUN_2_OR_NEWER
        RaiseEventOptions RaiseEventOptionsToAll = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
#endif

        [Header("Race Manager")]
        public RaceManager RaceManager;

        [Header("Playing Options")]
        [MMInformation("By Default, we set the game with no collisions in network. This is because Unity Physics Engine is non-deterministic. This will cause interference between server and each client and provoques a bad experience for the players." +
                       "\n\n" +
                       "Consider TrueSync Asset for true deterministic Physics", MMInformationAttribute.InformationType.Info, false)]
        /// Determines if collisions are active in network play
        public bool NoCollisions = true;

#if PUN_2_OR_NEWER
        Dictionary<int, BaseController> PlayerControllers = new Dictionary<int, BaseController>();
#endif

#if PUN_2_OR_NEWER
        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (RaceManager == null)
            {
                RaceManager = FindObjectOfType<RaceManager>();
                if (RaceManager == null)
                {
                    Debug.LogError("Missing RaceManager in scene", this);
                }
            }


            PhotonNetwork.OfflineMode = !PhotonNetwork.InRoom;

            if (PhotonNetwork.OfflineMode)
            {
                PhotonNetwork.CreateRoom("Offline Room");
            }

            // Setting up the RaceManager callbacks
            // Code from NetworkRaceManager is executed instead of RaceManager one
            RaceManager.OnDisableControlForPlayers = DisableControlForPlayers;
            RaceManager.OnEnableControlForPlayers = EnableControlForPlayers;
            RaceManager.OnDisableControlForPlayer = DisableControlForPlayer;
            RaceManager.OnUpdateCountdownText = UpdateCountdownText;
            RaceManager.OnShowEndGameScreen = ShowEndGameScreen;
            RaceManager.OnUpdatePlayersList = UpdatePlayersList;

            // We register end game backbutton callback
            RaceManager.BackToMenuButton.onClick.RemoveAllListeners();
            RaceManager.BackToMenuButton.onClick.AddListener(ReturnToMenu);

            // We override racemanager value with the networkracemanager value
            RaceManager.NoCollisions = NoCollisions;
            RaceManager.UpdateNoPlayersCollisions();

            // We register backbutton callback
            RaceManager.BackButton.onClick.RemoveAllListeners();
            RaceManager.BackButton.onClick.AddListener(ReturnToMenu);

            // starting the count down procedure
            RaceManager.StartGameCountdown.text = "WAITING FOR PUN PLAYERS";

            if (!PhotonNetwork.OfflineMode)
            {
                int vehicleId = 0;
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerProperty_CarId_Key))
                {
                    vehicleId = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProperty_CarId_Key];
                }
                GameObject vehicle = OnlineLobbyProxy.Instance.AvailableVehiclesPrefab[vehicleId];

                PhotonNetwork.Instantiate("Vehicles/" + vehicle.name,
                    RaceManager.StartPositions[PhotonNetwork.LocalPlayer.GetPlayerNumber()],
                    Quaternion.Euler(new Vector3(0, RaceManager.StartAngleDegree, 0)),
                    0
                );
            }

            // first we set outselves as ready, we have loaded the scene successfully.
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable() { { PlayerProperty_Ready_Key, true } });

            // we are the masterClient, we initiate the starting countdown
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(ManagerStart());
            }
            else
            {
                // We hide the back to menu button from end game panel
                RaceManager.BackToMenuButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Register to PUN network Client events
        /// </summary>
        public void OnEnable()
        {
            PhotonNetwork.NetworkingClient.EventReceived += this.OnEvent;
        }

        /// <summary>
        /// Clean up PUN event registration
        /// </summary>
        public void OnDisable()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= this.OnEvent;
        }

        ///<summary>
		/// Handle Photon events
		///</summary>
        protected virtual void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case DisableControlForPlayers_EventCode:
                    RaisedEvent_DisableControlForPlayers();
                    break;
                case EnableControlForPlayers_EventCode:
                    RaisedEvent_EnableControlForPlayers();
                    break;
                case StartCountDown_EventCode:
                    RaisedEvent_StartCountDown();
                    break;
                case ShowEndGameScreen_EventCode:
                    RaisedEvent_ShowEndGameScreen((string)photonEvent.Parameters[ParameterCode.Data]);
                    break;
            }
        }

        /// <summary>
        /// Start Manager actions
        /// </summary>
        public virtual IEnumerator ManagerStart()
        {
            if (!PhotonNetwork.OfflineMode)
            {
                // Manager will not start until each player has changed scene and is ready to play
                while (!OnlineLobbyProxy.Instance.ArePlayersReadyToPlay())
                {
                    yield return null;
                }
            }

            PhotonNetwork.RaiseEvent(StartCountDown_EventCode, null, RaiseEventOptionsToAll, SendOptions.SendReliable);

            // we disable players controls at start to let the race countdown run
            RaceManager.OnDisableControlForPlayers();

            // the Start Game Countdown must be 2 seconds at least
            // otherwise, the network doesn't have time to properly synchronize
            if (RaceManager.StartGameCountDownTime < 2)
            {
                Debug.LogWarning("StartGameCountDownTime was changed to 2 seconds (from "
                    + RaceManager.StartGameCountdown
                    + ". In network, StartGameCountDownTime must be at least 2 seconds.");
                RaceManager.StartGameCountDownTime = 2;
            }
        }

        /// <summary>
        /// Registers the player.
        /// </summary>
        /// <param name="ownerActorNr">Owner actor nr.</param>
        /// <param name="baseController">Base controller.</param>
        public virtual void RegisterPlayer(int ownerActorNr, BaseController baseController)
        {
            PlayerControllers[ownerActorNr] = baseController;
        }

        /// <summary>
        /// Unregister player ( player left room or got disconnected)
        /// </summary>
        /// <param name="ownerActorNr">Owner actor nr.</param>
        public virtual void UnRegisterPlayer(int ownerActorNr)
        {
            PlayerControllers.Remove(ownerActorNr);
        }

        #region RaceManager Composition

        /// <summary>
        /// Disables the control for players.
        /// </summary>
        public virtual void DisableControlForPlayers()
        {
            PhotonNetwork.RaiseEvent(DisableControlForPlayers_EventCode, null, RaiseEventOptionsToAll, SendOptions.SendReliable);
        }

        /// <summary>
        /// Disable the control for a given player
        /// </summary>
        public virtual void DisableControlForPlayer(BaseController Player)
        {
            // Get the photonView attached to the BaseController;

            PhotonView _pv = Player.GetComponent<PhotonView>();

            _pv.RPC("DisableControl", RpcTarget.All);

        }

        /// <summary>
        /// Enables the control for players.
        /// </summary>
        public virtual void EnableControlForPlayers()
        {
            PhotonNetwork.RaiseEvent(EnableControlForPlayers_EventCode, null, RaiseEventOptionsToAll, SendOptions.SendReliable);
        }

        /// <summary>
        /// Update of the countdown text in clients
        /// </summary>
        /// <param name="text">Text of the countdown</param>
        public virtual void UpdateCountdownText(string text)
        {
            if (text == "")
            {
                RaceManager.StartGameCountdown.gameObject.SetActive(false);
            }
            else
            {
                RaceManager.StartGameCountdown.text = text;
            }
        }

        /// <summary>
        /// Shows the end game screen. Call from server
        /// </summary>
        /// <param name="text">Text of the end of the game</param>
        public virtual void ShowEndGameScreen(string text)
        {
            PhotonNetwork.RaiseEvent(ShowEndGameScreen_EventCode, text, RaiseEventOptionsToAll, SendOptions.SendReliable);
        }

        /// <summary>
        /// Returns the players list actually playing game
        /// </summary>
        /// <returns>The players list.</returns>
        protected virtual List<BaseController> UpdatePlayersList()
        {
            return PlayerControllers.Values.ToList();
        }

        /// <summary>
        /// return to lobby
        /// </summary>
        public virtual void ReturnToMenu()
        {
            PhotonNetwork.Disconnect();
        }

        #endregion RaceManager composition

        #region Pun Raised Event

        /// <summary>
        /// Disable the control for players.
        /// </summary>
		protected virtual void RaisedEvent_DisableControlForPlayers()
        {
            foreach (PhotonView _pv in PhotonNetwork.PhotonViewCollection)
            {
                PunVehicleController _pvc = _pv.GetComponent<PunVehicleController>();
                if (_pvc != null) _pvc.DisableControls();
            }
        }


        /// <summary>
        /// Enable the control for players.
        /// </summary>
        protected virtual void RaisedEvent_EnableControlForPlayers()
        {
            foreach (PhotonView _pv in PhotonNetwork.PhotonViewCollection)
            {
                PunVehicleController _pvc = _pv.GetComponent<PunVehicleController>();
                if (_pvc != null) _pvc.EnableControls();
            }
        }

        /// <summary>
        /// Start game countdown
        /// </summary>
        protected virtual void RaisedEvent_StartCountDown()
        {
            StartCoroutine(RaceManager.StartGameCountdownCoroutine());
        }

        /// <summary>
        /// Shows the end game screen in client.
        /// </summary>
        /// <param name="text">Text of the end of the game</param>
        public virtual void RaisedEvent_ShowEndGameScreen(string text)
        {
            RaceManager.EndGameRanking.text = text;
            RaceManager.EndGamePanel.gameObject.SetActive(true);
            RaceManager.BackToMenuButton.gameObject.SetActive(true);
        }

        #endregion

#endif
    }
}