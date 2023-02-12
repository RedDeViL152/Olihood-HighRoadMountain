using UnityEngine;
using UnityEngine.UI;


#if PUN_2_OR_NEWER
using MoreMountains.Tools;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
#endif

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// This class manages the player state in the lobby.
    /// </summary>
    public class PunLobbyPlayer :
#if PUN_2_OR_NEWER
    MonoBehaviourPunCallbacks
#else
    MonoBehaviour
#endif
    , IGenericLobbyPlayer, ILobbyPlayerInfo
    {
        #region ILobbyPlayerInfo implementation

        public int Position { get; set; }

        public string Name
        {
            get { return _playerName; }

            set
            {
#if PUN_2_OR_NEWER
                this.photonView.Owner.NickName = value;
                OnPlayerNameChanged(this.photonView.Owner.NickName);
#endif
            }
        }

        public string VehicleName { get; set; }

        public int VehicleSelectedIndex { get; set; }

        public bool IsBot { get; set; }

        #endregion


        [Header("GUI Elements")]
        public Text PlayerName;
        public Button LeftButton;
        public Button RightButton;
        public Button ReadyButton;
        public Text VehicleText;
        public Image VehicleImage;
        public Sprite ReadyButtonSprite;
        public Sprite CancelReadyButtonSprite;

        protected int _currentTrackSelected; // index of the current track selected.
        protected string _playerName; // The name of the player.
        protected Vector3 elementPosition = Vector3.zero;
        protected RectTransform _rectTransform;
        protected bool readyToBegin = false;

        /// <summary>
        /// Gets the currently selected track.
        /// </summary>
        /// <value>The track selected.</value>
        public virtual int TrackSelected
        {
            get { return _currentTrackSelected; }
        }

        /// <summary>
        /// We use this event to initialize object properties and gui.
        /// This is called when a client connects for the first time and when a client goes back to the lobby after a race.
        /// </summary>
        public virtual void Start()
        {
            // clean up player properties 
#if PUN_2_OR_NEWER
            if (this.photonView.IsMine)
            {
                Hashtable _cleanup = new Hashtable();
                _cleanup.Add(PunRaceManager.PlayerProperty_LobbyReady_Key, null);
                _cleanup.Add(PunRaceManager.PlayerProperty_Ready_Key, null);
                _cleanup.Add(PunRaceManager.PlayerProperty_CarId_Key, null);
                this.photonView.Owner.SetCustomProperties(_cleanup);
            }

            // In case the lobby is not shown, when going back from the game screen, we show the lobby gui
            if (!OnlineLobbyProxy.Instance.OnlineLobbyUIManager.Lobby.gameObject.activeSelf)
            {
                // We change to connected mode
                OnlineLobbyProxy.Instance.OnlineLobbyUIManager.ShowLobby();
                OnlineLobbyProxy.Instance.OnlineLobbyUIManager.ShowConnected();
                OnlineLobbyProxy.Instance.OnlineLobbyUIManager.HidePopup();
            }

            //int slot = this.photonView.Owner.GetPlayerNumber();
            int slot = this.photonView.Owner.ActorNumber -  1;
            // we look for the associated player gui zone
            
            
            RectTransform slotZone = OnlineLobbyProxy.Instance.OnlineLobbyUIManager.PlayersSelection[slot];

            if (slotZone != null)
            {
                _rectTransform = GetComponent<RectTransform>();

                _rectTransform.SetParent(slotZone.transform, false);
                _rectTransform.anchoredPosition = Vector2.zero;

                if (elementPosition == Vector3.zero)
                {
                    // we store the initial and correct position for future updates
                    elementPosition = _rectTransform.position;
                }

                LeftButton.gameObject.SetActive(false);
                RightButton.gameObject.SetActive(false);
                ReadyButton.interactable = false;

                // If we are the owner and the masterClient, 
                //	 we have authority over choosing the track
                // else 
                //   we check in the room properties for the current selected track
                if (this.photonView.IsMine && this.photonView.Owner.IsMasterClient)
                {
                    OnCurrentTrackChanged(_currentTrackSelected);
                }
                else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PunRaceManager.RoomProperty_TrackId_Key))
                {
                    RefreshTrackSelection((int)PhotonNetwork.CurrentRoom.CustomProperties[PunRaceManager.RoomProperty_TrackId_Key]);
                }

                // car selection
                if (!this.photonView.IsMine)
                {
                    if (this.photonView.Owner.CustomProperties.ContainsKey(PunRaceManager.PlayerProperty_CarId_Key))
                    {
                        VehicleSelectedIndex = (int)this.photonView.Owner.CustomProperties[PunRaceManager.PlayerProperty_CarId_Key];
                    }
                    else
                    {
                        VehicleSelectedIndex = 0;
                    }
                }

                ShowSelectedVehicle();

                this.photonView.Owner.NickName = "Player #" + (slot + 1);
                OnPlayerNameChanged(this.photonView.Owner.NickName);

                if (this.photonView.Owner.CustomProperties.ContainsKey(PunRaceManager.PlayerProperty_LobbyReady_Key) &&
                    this.photonView.Owner.CustomProperties[PunRaceManager.PlayerProperty_LobbyReady_Key] != null)
                {
                    readyToBegin = (bool)this.photonView.Owner.CustomProperties[PunRaceManager.PlayerProperty_LobbyReady_Key];
                }
                else
                {
                    readyToBegin = false;
                }

                this.OnClientReady(readyToBegin);

                InitUI();
            }
            else
            {
                Debug.LogWarning("Zone UI for player is missing. Ensure Lobby is visible & in connected mode");
            }
#endif
        }

#if PUN_2_OR_NEWER
        #region PunBehaviour Callbacks

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(PunRaceManager.RoomProperty_TrackId_Key))
            {
                RefreshTrackSelection((int)propertiesThatChanged[PunRaceManager.RoomProperty_TrackId_Key]);
            }
        }

        public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
        {
            if (target != this.photonView.Owner)
            {
                return;
            }

            if (changedProps.ContainsKey(PunRaceManager.PlayerProperty_LobbyReady_Key) &&
                changedProps[PunRaceManager.PlayerProperty_LobbyReady_Key] != null)
            {
                readyToBegin = (bool)changedProps[PunRaceManager.PlayerProperty_LobbyReady_Key];
                this.OnClientReady(readyToBegin);
            }

            if (changedProps.ContainsKey(PunRaceManager.PlayerProperty_CarId_Key) &&
                changedProps[PunRaceManager.PlayerProperty_CarId_Key] != null)
            {
                this.VehicleSelectedIndex = (int)changedProps[PunRaceManager.PlayerProperty_CarId_Key];
                this.ShowSelectedVehicle();
            }
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.LocalPlayer.CustomProperties.Remove(PunRaceManager.PlayerProperty_LobbyReady_Key);
            PhotonNetwork.LocalPlayer.CustomProperties.Remove(PunRaceManager.PlayerProperty_Ready_Key);
            PhotonNetwork.LocalPlayer.CustomProperties.Remove(PunRaceManager.PlayerProperty_CarId_Key);
        }

        #endregion
#endif
        /// <summary>
        /// Updates the player's name in the textbox
        /// </summary>
        protected virtual void OnPlayerNameChanged(string name)
        {
            PlayerName.text = name;
        }

        /// <summary>
        /// We need to anchor the GameObject's position otherwise when going back from game to lobby,
        /// the object would be moved to an incorrect position.
        /// </summary>
        protected virtual void Update()
        {
            if (transform.position != elementPosition)
            {
                transform.position = elementPosition;
            }
        }

        /// <summary>
        /// Initializes the UI
        /// </summary>
        protected virtual void InitUI()
        {
#if PUN_2_OR_NEWER
            if (this.photonView.IsMine)
            {
                LeftButton.gameObject.SetActive(true);
                RightButton.gameObject.SetActive(true);
                LeftButton.onClick.RemoveAllListeners();
                LeftButton.onClick.AddListener(OnLeftButton);
                RightButton.onClick.RemoveAllListeners();
                RightButton.onClick.AddListener(OnRightButton);
                ReadyButton.onClick.RemoveAllListeners();
                ReadyButton.onClick.AddListener(OnReady);
                ReadyButton.interactable = true;
                OnClientReady(false);

                if (this.photonView.IsMine && this.photonView.Owner.IsMasterClient)
                {
                    OnlineLobbyProxy.Instance.OnlineLobbyUIManager.ShowTrackSelection(this);
                }
            }
            else
            {
                LeftButton.gameObject.SetActive(false);
                RightButton.gameObject.SetActive(false);
                ReadyButton.interactable = false;
                UpdateReadyButtonText(false);
            }
#endif
        }

        /// <summary>
        /// Describes what happens when pressing the left button
        /// </summary>
        public virtual void OnLeftButton()
        {
            OnVehicleChange(-1);
        }

        /// <summary>
        /// Describes what happens when pressing the right button
        /// </summary>
        public virtual void OnRightButton()
        {
            OnVehicleChange(1);
        }

        /// <summary>
        /// Changes the current player vehicle
        /// </summary>
        /// <param name="direction">Direction.</param>
        public virtual void OnVehicleChange(int direction)
        {
            int newVehicleSelected = this.VehicleSelectedIndex + direction;

            if (newVehicleSelected < 0)
            {
                newVehicleSelected = OnlineLobbyProxy.Instance.AvailableVehiclesPrefab.Length - 1;
            }
            else if (newVehicleSelected > (OnlineLobbyProxy.Instance.AvailableVehiclesPrefab.Length - 1))
            {
                newVehicleSelected = 0;
            }

            OnVehicleChanged(newVehicleSelected);
        }


        /// <summary>
        /// Describes what happens when the vehicle changes
        /// </summary>
        /// <param name="value">Value.</param>
        public virtual void OnVehicleChanged(int value)
        {
            VehicleSelectedIndex = value;
#if PUN_2_OR_NEWER
            this.photonView.Owner.SetCustomProperties(
                new Hashtable() { {
                        PunRaceManager.PlayerProperty_CarId_Key,
                        VehicleSelectedIndex
                    } });
#endif
            ShowSelectedVehicle();
        }

        /// <summary>
        /// Shows the selected vehicle.
        /// </summary>
        protected virtual void ShowSelectedVehicle()
        {
            VehicleInformation info = OnlineLobbyProxy.Instance.AvailableVehiclesPrefab[VehicleSelectedIndex].GetComponent<VehicleInformation>();
            VehicleText.text = info.LobbyName;
            VehicleImage.sprite = info.lobbySprite;
            VehicleName = VehicleText.text;
        }

        #region IGenericLobbyPlayer implementation

        void IGenericLobbyPlayer.OnTrackChange(int direction)
        {
            int newValue = _currentTrackSelected + direction;

            if (newValue < 0)
            {
                newValue = OnlineLobbyProxy.Instance.AvailableTracksSceneName.Length - 1;
            }
            else if (newValue >= OnlineLobbyProxy.Instance.AvailableTracksSceneName.Length)
            {
                newValue = 0;
            }

            OnCurrentTrackChanged(newValue);
        }

        #endregion


        /// <summary>
        /// The current track gets changed.
        /// </summary>
        /// <param name="value">Value.</param>
        void OnCurrentTrackChanged(int value)
        {
            RefreshTrackSelection(value);
#if PUN_2_OR_NEWER
            // save this in the room custom properties
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable() { {
                        PunRaceManager.RoomProperty_TrackId_Key,
                        _currentTrackSelected
                    } });
            }
#endif
        }

        void RefreshTrackSelection(int value)
        {
            _currentTrackSelected = value;
            OnlineLobbyProxy.Instance.OnlineLobbyUIManager.UpdateTrackInfo(_currentTrackSelected);
        }

        /// <summary>
        /// Describes what happens when the player chooses the "READY" action in GUI
        /// </summary>
        public virtual void OnReady()
        {
            readyToBegin = !readyToBegin;
#if PUN_2_OR_NEWER
            this.photonView.Owner.SetCustomProperties(
                new Hashtable() { {
                        PunRaceManager.PlayerProperty_LobbyReady_Key,
                        readyToBegin
                    } });
#endif
            OnClientReady(readyToBegin);
        }

        /// <summary>
        /// When player is ready, READY button is disabled.
        /// </summary>
        public void OnClientReady(bool readyState)
        {
            if (readyState)
            {
                UpdateReadyButtonText(true);
                ReadyButton.interactable = false;
                LeftButton.gameObject.SetActive(false);
                RightButton.gameObject.SetActive(false);
            }
            else
            {
                UpdateReadyButtonText(false);
                ReadyButton.interactable = true;
                LeftButton.gameObject.SetActive(true);
                RightButton.gameObject.SetActive(true);
            }

            OnlineLobbyProxy.Instance.OnPlayerReadyToBeginChanged();
        }

        /// <summary>
        /// Updates the ready button text.
        /// </summary>
        /// <param name="ready">If set to <c>true</c> ready.</param>
        protected virtual void UpdateReadyButtonText(bool ready)
        {
            Text buttonText = ReadyButton.GetComponentInChildren<Text>();

            if (ready)
            {
                buttonText.text = "- READY -";
                ReadyButton.image.sprite = CancelReadyButtonSprite;
            }
            else
            {
                buttonText.text = "READY ?";
                ReadyButton.image.sprite = ReadyButtonSprite;
            }
        }
    }
}