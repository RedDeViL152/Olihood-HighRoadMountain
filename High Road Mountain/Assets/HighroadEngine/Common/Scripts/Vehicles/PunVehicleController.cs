

using UnityEngine;
#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Network vehicle controller. In charge of the network manager for players in game
    /// </summary>
    [RequireComponent(typeof(BaseController))]
    public class PunVehicleController :
#if PUN_2_OR_NEWER
    MonoBehaviourPunCallbacks, IPunObservable
#else
    MonoBehaviour
#endif
    , INetworkVehicle
    {
        public string PlayerName;
        public int Score;

        protected float SteerValue;
        protected float GasPedalValue;
        protected BaseController _controller;
        protected RaceManager _raceManager;

        /// <summary>
        /// controller initialisation
        /// </summary>
        public virtual void Awake()
        {
            _controller = GetComponent<BaseController>();
        }

        /// <summary>
        /// We initialize photon
        /// </summary>
        public virtual void Start()
        {
#if PUN_2_OR_NEWER
            PlayerName = this.photonView.Owner.NickName;

            // name the gameobject by the player name
            this.gameObject.name = PlayerName;

            _raceManager = FindObjectOfType<RaceManager>();

            if (this.photonView.IsMine)
            {
                if (_raceManager != null)
                {
                    foreach (var c in _raceManager.CameraControllers)
                    {
                        c.HumanPlayers = new[] { this.transform };
                    }
                }
            }

            PunRaceManager.Instance.RegisterPlayer(this.photonView.OwnerActorNr, _controller);
#endif

            DisableControls();
        }

        /// <summary>
        /// We synchronize score
        /// </summary>
        public virtual void Update()
        {
#if PUN_2_OR_NEWER
            if (PhotonNetwork.OfflineMode)
            {
                if (_controller.IsPlaying)
                {
                    // We show an update of score of current player
                    _raceManager.ScoreText1.text = "";
                    _raceManager.ScoreText2.text = PlayerName;
                    _raceManager.ScoreText3.text = string.Format("Lap {0}/{1}",
                        _controller.CurrentLap >= _raceManager.Laps ?
                        _raceManager.Laps : _controller.CurrentLap + 1,
                        _raceManager.Laps
                    );
                }
            }
#endif

            Score = _controller.Score;
        }

#if PUN_2_OR_NEWER

        #region IPunObservable implementation
        void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            { // As local player we send our gas pedal and steering values to the other instances
                stream.SendNext(_controller.CurrentSteeringAmount);
                stream.SendNext(_controller.CurrentGasPedalAmount);
            }
            else
            { // As not local player we receive out gas pedal and sterring values from the owner
                _controller.CurrentSteeringAmount = (float)stream.ReceiveNext();
                _controller.CurrentGasPedalAmount = (float)stream.ReceiveNext();
            }
        }
        #endregion


        [PunRPC]
        void DisableControl()
        {
            if (this.photonView.IsMine)
            {
                _controller.DisableControls();
            }
        }
#endif

        public virtual void OnDestroy()
        {
#if PUN_2_OR_NEWER
            PunRaceManager.Instance.UnRegisterPlayer(this.photonView.OwnerActorNr);
#endif
        }

        /// <summary>
        /// Enables the controls when vehicle is controlled by a local player
        /// </summary>
        public virtual void EnableControls()
        {
#if PUN_2_OR_NEWER
            if (this.photonView.IsMine)
            {
                _controller.EnableControls(0);
            }
#endif
        }

        /// <summary>
        /// Disables the controls when vehicle is is controlled by a local player
        /// </summary>
        public virtual void DisableControls()
        {
#if PUN_2_OR_NEWER
            if (this.photonView.IsMine)
            {
                _controller.DisableControls();
            }
#endif
        }

        #region INetworkPlayer implementation

        public virtual void SetPlayerName(string value)
        {
            PlayerName = value;
        }

        #endregion
    }
}