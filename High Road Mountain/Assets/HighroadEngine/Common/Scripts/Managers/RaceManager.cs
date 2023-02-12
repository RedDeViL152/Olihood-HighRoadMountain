using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MoreMountains.Tools;
using UnityEngine.Events;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Race manager. Class in charge of the cars instantiation, camera, ranking and UI.
    /// Have a look at the Start() method for race initialisation and Update() for game management.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        [Header("Start positions")]
        [MMInformation("Set the size of the <b>Startpositions</b>, then position the Vector3 using either the inspector or by moving the handles directly in scene view. The order of the array will be the order the car positions.\n", MMInformationAttribute.InformationType.Info, false)]
        /// the list of start positions
        public Vector3[] StartPositions;
        [Range(0, 359)]
        public int StartAngleDegree;
        /// if true, human players will be placed after bots at start
        public bool BotsFirstInStartingLine = true;

        [Header("Camera")]
        [MMInformation("If checked, provide below the list of cameras (of type CameraController) the player can switch between.\n", MMInformationAttribute.InformationType.Info, false)]
        public bool DynamicCameras;
        /// the list of cameras the player can use
        public CameraController[] CameraControllers;

        [Header("UI")]
        /// Text object where start countdown is shown
        public Text StartGameCountdown;
        /// Panel shown when game has ended
        public RectTransform EndGamePanel;
        /// Text object for players ranking at the end of the game
        public Text EndGameRanking;
        /// Back button to return to lobby screen 
        public Button BackButton;
        /// Back button to return to lobby screen when racing is finished
        public Button BackToMenuButton;
        /// Button to change camera when multiple cameras are available
        public Button CameraChangeButton;
        /// Button to respawn player's vehicle
        public Button RespawnButton;
        /// Current score ingame
        public Text ScoreText1;
        public Text ScoreText2;
        public Text ScoreText3;

        [Header("Playing options")]
        /// If false, last checkpoint is the end of the race like a rally
        public bool ClosedLoopTrack = true;
        /// If true, race is ended when first player finish
        public bool FirstFinisherEndsRace = true;
        /// Number of laps for victory
        public int Laps = 3;
        [MMInformation("In network mode, Start Game Countdown must be at least 2 seconds to avoid incorrect synchronization.\n", MMInformationAttribute.InformationType.Info, false)]
        /// Countdown timer before starting the race.
        public int StartGameCountDownTime = 3;
        [MMInformation("If set to true, cars won't collide themselves. Please note that this value is overrided in network mode by the NetworkRaceManager class.\n", MMInformationAttribute.InformationType.Info, false)]
        /// Are collisions active in network play
        public bool NoCollisions = false;

        [Header("Track configuration")]
        [MMInformation("Add Gameobjects to the <b>Checkpoints</b> (set size first), then position the checkpoints objects using either the inspector or by moving the handles directly in scene view. The order of the array will be the order the checkpoints to go through.\n", MMInformationAttribute.InformationType.Info, false)]

        public GameObject[] Checkpoints;

        /// Reference to AI Waypoints object
        public GameObject AIWaypoints;

        [Header("Test Mode")]
        /// List of human players vehicle gameobjects
        public GameObject[] TestHumanPlayers;
        /// List of bot players vehicle gameobjects
        public GameObject[] TestBotPlayers;
        /// a dictionary of the vehicle's gameobjects, using the player's number as index
        public Dictionary<int, BaseController> Players { get; protected set; }

        /// The current race elapsed time. Used in the ranking
        protected float _currentGameTime;
        protected int _currentCamera;
        /// Sublist of camera controllers we can currently use. For instance, we remove single player camera in multi local mode
        protected CameraController[] _cameraControllersAvailable;
        protected IGenericLobbyManager _lobbyManager;
        protected bool _isPlaying;
        protected bool _isNetworkMode;
        protected bool _testMode = false;
        protected int _finisherBonusScore = 10000;
        protected int _currentFinisherRank = 1;

        // The following actions are used to delegate behaviour into the networkrace class when game is online
        public UnityAction OnDisableControlForPlayers;
        public UnityAction<BaseController> OnDisableControlForPlayer;
        public UnityAction OnEnableControlForPlayers;
        public UnityAction<string> OnUpdateCountdownText;
        public UnityAction<string> OnShowEndGameScreen;
        public delegate List<BaseController> OnUpdatePlayersListDelegate();
        public OnUpdatePlayersListDelegate OnUpdatePlayersList;

        /// <summary>
        /// We checks proper initialization of the RaceManager object
        /// </summary>
        public virtual void Awake()
        {
            if (Checkpoints.Length == 0)
            {
                Debug.LogWarning("List of checkpoints should be initialized in RaceManager gameobject inspector.");
            }

            if (StartPositions.Length == 0)
            {
                Debug.LogWarning("List of StartPositions is empty. You should provide at least one start position.");
            }

            if (!ClosedLoopTrack && Laps > 1)
            {
                Debug.LogWarning("Track is open and doesn't loop. Laps value is > 1 in RaceManager inspector and will ignored.");
            }

            // Helps iOS Framerate
            Application.targetFrameRate = 300;
        }

        /// <summary>
        /// We initialize the race.
        /// </summary>
        public virtual void Start()
        {
            _isPlaying = false;

            if (EndGamePanel != null)
            {
                EndGamePanel.gameObject.SetActive(false);
            }

            if (CameraChangeButton != null)
            {
                CameraChangeButton.onClick.RemoveAllListeners();
                CameraChangeButton.onClick.AddListener(OnCameraChange);
            }

            if (ScoreText1 != null)
            {
                ScoreText1.text = "";
            }

            if (ScoreText2 != null)
            {
                ScoreText2.text = "";
            }

            if (ScoreText3 != null)
            {
                ScoreText3.text = "";
            }

            if (RespawnButton != null)
            {
                RespawnButton.gameObject.SetActive(false);
            }

            // Find proper lobby manager between online (UNET network) & local
            if (OnlineLobbyProxy.Instance != null)
            {
                _isNetworkMode = true;

                if (OnlineLobbyProxy.Instance != null) _lobbyManager = OnlineLobbyProxy.Instance; 

                // in network, all cameras can be used in single mode
                _cameraControllersAvailable = CameraControllers;

                if (DynamicCameras)
                {
                    // By default, we active first camera and disallow others
                    ChangeActiveCameraController(0);
                }

                // In Network case, we don't do anything else. Callbacks are managed by the NetworkRaceManager class
            }
            else
            {
                // we initialize the local array of car players 
                Players = new Dictionary<int, BaseController>();

                _isNetworkMode = false;
                _lobbyManager = LocalLobbyManager.Instance;

                OnDisableControlForPlayers = DisableControlForPlayers;
                OnDisableControlForPlayer = DisableControlForPlayer;
                OnEnableControlForPlayers = EnableControlForPlayers;
                OnUpdateCountdownText = UpdateCountdownText;
                OnShowEndGameScreen = ShowEndGameScreen;
                OnUpdatePlayersList = UpdatePlayersList;

                // we register the backbutton callback
                if (BackButton != null)
                {
                    BackButton.onClick.RemoveAllListeners();
                    BackButton.onClick.AddListener(ReturnToMenu);
                }

                if (BackToMenuButton != null)
                {
                    BackToMenuButton.onClick.RemoveAllListeners();
                    BackToMenuButton.onClick.AddListener(ReturnToMenu);
                }

                // 	Test mode (lobby is empty)
                if (LocalLobbyManager.Instance.Players().Count == 0)
                {
                    InitTestMode();
                }

                ManagerStart();
            }

            // Independant to network or local start, we hide or show the camera change button
            if (_cameraControllersAvailable.Length <= 1)
            {
                if (CameraChangeButton != null)
                {
                    CameraChangeButton.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Initializes the test mode players racing
        /// </summary>
        protected virtual void InitTestMode()
        {
            _testMode = true;

            int currentPosition = 0;

            if (TestHumanPlayers != null)
            {
                foreach (var player in TestHumanPlayers)
                {
                    if (player.GetComponent<BaseController>() != null)
                    {
                        LocalLobbyManager.Instance.AddPlayer(new LocalLobbyPlayer
                        {
                            Position = currentPosition,
                            Name = player.GetComponent<VehicleInformation>().LobbyName + (currentPosition + 1),
                            VehicleName = player.name,
                            VehicleSelectedIndex = -1,
                            IsBot = false
                        });
                        currentPosition++;
                    }
                    else
                    {
                        Debug.LogWarning(player.name + " can't be instantiated for test mode. Test mode needs a prefab with a BaseController Component.");
                    }
                }
            }

            if (TestBotPlayers != null)
            {
                foreach (var player in TestBotPlayers)
                {
                    if (player.GetComponent<BaseController>() != null && player.GetComponent<VehicleAI>() != null)
                    {
                        LocalLobbyManager.Instance.AddPlayer(new LocalLobbyPlayer
                        {
                            Position = currentPosition,
                            Name = player.GetComponent<VehicleInformation>().LobbyName + (currentPosition + 1),
                            VehicleName = player.name,
                            VehicleSelectedIndex = -1,
                            IsBot = true
                        });
                        currentPosition++;
                    }
                    else
                    {
                        Debug.LogWarning(player.name + " can't be instantiated for test mode. AI Test mode needs a prefab with a BaseController and VehicleAI Component.");
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the players and their cars.
        /// </summary>
        public virtual void ManagerStart()
        {
            if (_isNetworkMode)
            {
                Debug.LogError("This function should not be called in network mode");
                return;
            }

            InitPlayers();

            UpdateNoPlayersCollisions();

            // we disable players controls at start to let the race countdown runs
            OnDisableControlForPlayers();

            // we start the race countdown
            StartCoroutine(StartGameCountdownCoroutine());
        }

        /// <summary>
        /// Updates the no players collisions from the NoCollisions flag in inspector.
        /// </summary>
        public virtual void UpdateNoPlayersCollisions()
        {
            if (NoCollisions)
            {
                int vehiclesLayer = LayerMask.NameToLayer("Actors");
                Physics.IgnoreLayerCollision(vehiclesLayer, vehiclesLayer);
            }
        }

        /// <summary>
        /// Initializes the players of the race. Instantiation of gameobjects in single / local mode
        /// </summary>
        protected virtual void InitPlayers()
        {
            if (_isNetworkMode)
            {
                Debug.LogError("This function should not be called in network mode");
                return;
            }

            // List of car controllers transforms. Used by camera controller target
            List<Transform> cameraHumanTargets = new List<Transform>();
            List<Transform> cameraBotsTargets = new List<Transform>();

            // start position for current instantiated player
            int _currentStartPosition = 0;

            // we iterate through each lobby player with bots first
            List<ILobbyPlayerInfo> playersAtStart;


            // we order players list with bots first
            if (BotsFirstInStartingLine)
            {
                playersAtStart =
                        LocalLobbyManager.Instance.Players()
                        .Select(x => x.Value)
                        .OrderByDescending(x => x.IsBot)
                        .ToList();
            }
            else
            {
                playersAtStart = new List<ILobbyPlayerInfo>(LocalLobbyManager.Instance.Players().Values);
            }

            if (playersAtStart.Count > StartPositions.Length)
            {
                Debug.LogWarning("You only have "
                + StartPositions.Length
                + " start positions in your scene but have "
                + playersAtStart.Count
                + " players ready to start. Either remove players or add more Start positions in your scene.");
            }
            else
            {
                foreach (ILobbyPlayerInfo item in playersAtStart)
                {
                    GameObject prefab;

                    if (item.VehicleSelectedIndex >= 0)
                    {
                        prefab = LocalLobbyManager.Instance.AvailableVehiclesPrefabs[item.VehicleSelectedIndex];
                    }
                    else
                    {
                        // test mode, we find the prefab with a resources load
                        prefab = Resources.Load("vehicles/" + item.VehicleName) as GameObject;
                    }

                    // we first instantiate car for this player. 
                    // the car name value is used to load the prefab from Resources/Vehicles folder.
                    GameObject newPlayer = Instantiate(
                                               prefab,
                                               StartPositions[_currentStartPosition],
                                               Quaternion.Euler(new Vector3(0, StartAngleDegree, 0))
                                           ) as GameObject;

                    // we add this new object to the list of players
                    BaseController car = newPlayer.GetComponent<BaseController>();
                    Players[item.Position] = car;

                    car.name = item.Name;

                    // We initialize AI component
                    VehicleAI ai = newPlayer.GetComponent<VehicleAI>();
                    if (ai != null)
                    {
                        ai.Active = item.IsBot;
                    }

                    // we add this car gameobject to the camera targets
                    if (item.IsBot)
                    {
                        cameraBotsTargets.Add(newPlayer.transform);
                    }
                    else
                    {
                        cameraHumanTargets.Add(newPlayer.transform);
                    }

                    // we go to next start position
                    _currentStartPosition++;
                }
            }

            // we add the list of players to the cameras
            List<CameraController> availableCam = new List<CameraController>();
            foreach (var c in CameraControllers)
            {
                if (c == null)
                {
                    return;
                }
                c.gameObject.SetActive(false);

                // This camera can't be used in local multi
                if (cameraHumanTargets.Count > 1 && c.IsSinglePlayerCamera)
                {
                    continue;
                }

                c.BotPlayers = cameraBotsTargets.ToArray();
                c.HumanPlayers = cameraHumanTargets.ToArray();
                availableCam.Add(c);
            }

            // we init respawn button
            if (cameraHumanTargets.Count == 1)
            {
                InitRespawnButton(cameraHumanTargets[0].gameObject);
            }

            _cameraControllersAvailable = availableCam.ToArray();

            if (DynamicCameras && _cameraControllersAvailable.Length == 0)
            {
                Debug.LogError("No camera available found. Please ensure at least one camera is configurated in RaceManager inspector.");
                return;
            }
            else
            {
                if (DynamicCameras)
                {
                    // By default, we active first camera
                    ChangeActiveCameraController(0);
                }
            }
        }

        /// <summary>
        /// Switch current active camera to new index value
        /// </summary>
        /// <param name="index">Index.</param>
        protected virtual void ChangeActiveCameraController(int index)
        {
            for (int i = 0; i < _cameraControllersAvailable.Length; i++)
            {
                if (i == index)
                {
                    _cameraControllersAvailable[i].gameObject.SetActive(true);
                }
                else
                {
                    _cameraControllersAvailable[i].gameObject.SetActive(false);
                }
            }

        }

        /// <summary>
        /// Inits the respawn button.
        /// </summary>
        /// <param name="vehicle">Vehicle.</param>
        public virtual void InitRespawnButton(GameObject vehicle)
        {
            if (RespawnButton != null)
            {
                RespawnButton.onClick.RemoveAllListeners();
                SolidController controller = vehicle.GetComponent<SolidController>();
                if (controller != null)
                {
                    RespawnButton.onClick.AddListener(controller.Respawn);
                    RespawnButton.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// On Update, we sort the player's list and update score
        /// </summary>
        public virtual void Update()
        {
            // if game is on
            if (_isPlaying)
            {
                _currentGameTime += Time.deltaTime;

                // without UI, we don't compute anyting
                if (EndGamePanel != null)
                {
                    // we sort the players list with their score & their distance to the next checkpoint
                    var playersRank = OnUpdatePlayersList()
                        .OrderBy(p => p.FinalRank == 0) // First the finishers
                        .ThenBy(p => p.FinalRank) // Ordered by final rank position
                        .ThenByDescending(p => p.Score) // Then laters
                        .ThenBy(p => p.DistanceToNextWaypoint)
                        .ToList();

                    if (playersRank.Count > 0)
                    {
                        // update score screen
                        if (ScoreText1 != null && ScoreText2 != null && ScoreText3 != null)
                        {
                            string newscore1 = "";
                            string newscore2 = "";
                            string newscore3 = "";

                            int position = 1;
                            // we show current scores
                            foreach (var p in playersRank)
                            {
                                newscore1 += position + "\r\n";
                                newscore2 += string.Format("| {0}\r\n",
                                    p.name
                                );

                                if (ClosedLoopTrack)
                                {
                                    newscore3 += string.Format("lap {0}/{1}\r\n",
                                        p.CurrentLap >= Laps ? Laps : p.CurrentLap + 1,
                                        Laps
                                    );
                                }
                                position++;
                            }

                            ScoreText1.text = newscore1;
                            ScoreText2.text = newscore2;
                            ScoreText3.text = newscore3;
                        }

                        if (FirstFinisherEndsRace)
                        {
                            // if first player of the race has finished game, we end the race
                            if (playersRank[0].HasJustFinished(1))
                            {
                                OnDisableControlForPlayers();
                                _isPlaying = false;
                                ShowFinalRanking(playersRank);
                            }
                        }
                        else
                        {
                            // every player must have finished before race is ended
                            foreach (BaseController player in playersRank)
                            {
                                if (player.HasJustFinished(_currentFinisherRank))
                                {
                                    OnDisableControlForPlayer(player);
                                    _currentFinisherRank++;
                                }
                            }

                            bool hasEveryoneFinished = true;
                            foreach (BaseController player in playersRank)
                            {
                                if (player.FinalRank == 0)
                                {
                                    hasEveryoneFinished = false;
                                    break;
                                }
                            }

                            if (hasEveryoneFinished)
                            {
                                OnDisableControlForPlayers();
                                _isPlaying = false;
                                ShowFinalRanking(playersRank);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the players list
        /// </summary>
        /// <returns>The players list.</returns>
        protected virtual List<BaseController> UpdatePlayersList()
        {
            List<BaseController> cars = new List<BaseController>();
            foreach (var car in Players.Values)
            {
                cars.Add(car);
            }
            return cars;
        }

        /// <summary>
        /// Shows the final ranking and return button
        /// </summary>
        /// <param name="playersRank">Players rank.</param>
        protected virtual void ShowFinalRanking(List<BaseController> playersRank)
        {
            // Show final ranking & controls to end
            string finalRank = "--- finished in " + System.Math.Round(_currentGameTime, 2) + " secs. ---";

            for (int i = 0; i < playersRank.Count; i++)
            {
                finalRank += "\r\n" + playersRank[i].name;
            }

            OnShowEndGameScreen(finalRank);
        }

        /// <summary>
        /// Shows the end game screen with ranking and exit button
        /// </summary>
        /// <param name="text">Ranking.</param>
        protected virtual void ShowEndGameScreen(string text)
        {
            if (_isNetworkMode)
            {
                Debug.LogError("This function should not be called in network mode");
                return;
            }

            EndGameRanking.text = text;
            EndGamePanel.gameObject.SetActive(true);
        }

        /// <summary>
        /// Returns to lobby
        /// </summary>
        public virtual void ReturnToMenu()
        {
            if (_testMode)
            {
                Debug.LogWarning("In Test Mode, you can't quit current scene.");
                return;
            }

            _lobbyManager.ReturnToLobby();
        }

        /// <summary>
        /// Starts the game countdown coroutine.
        /// </summary>
        /// <returns>yield enumerator</returns>
        public virtual IEnumerator StartGameCountdownCoroutine()
        {
            // without UI, we don't need to countdown
            if (EndGamePanel != null)
            {
                float remainingCountDown = StartGameCountDownTime;

                // while at least 1 second
                while (remainingCountDown > 1)
                {
                    // we yield this loop
                    yield return null;

                    remainingCountDown -= Time.deltaTime;
                    // new remaining time is int value 
                    int newFloorTime = Mathf.FloorToInt(remainingCountDown);
                    OnUpdateCountdownText("Start in " + newFloorTime);
                }
            }

            // remanining count is now < 1 second, 
            // since it would mean 0 in int value, we start the game

            _isPlaying = true;
            foreach (CameraController cam in _cameraControllersAvailable)
            {
                cam.GameHasStarted = true;
            }

            OnEnableControlForPlayers();

            OnUpdateCountdownText("GO !");
            _currentGameTime = 0f;

            yield return new WaitForSeconds(1);

            // after 1 more second, we hide the countdown text object
            OnUpdateCountdownText("");
        }

        /// <summary>
        /// Updates the countdown text.
        /// </summary>
        /// <param name="text">Text.</param>
        public virtual void UpdateCountdownText(string text)
        {
            if (StartGameCountdown == null)
            {
                return;
            }

            if (_isNetworkMode)
            {
                Debug.LogError("This function should not be called in network mode");
                return;
            }

            if (text == "")
            {

                StartGameCountdown.gameObject.SetActive(false);
            }
            else
            {
                StartGameCountdown.text = text;
            }
        }

        /// <summary>
        /// Enables the control for players locally
        /// </summary>
        public virtual void EnableControlForPlayers()
        {
            if (_isNetworkMode)
            {
                Debug.LogError("This function should not be called in network mode");
                return;
            }

            // we iterate throught LobbyManager to find active players with their control position
            for (int i = 0; i < LocalLobbyManager.Instance.Players().Count; i++)
            {
                if (Players.ContainsKey(i))
                {
                    int controllerID = -1;

                    // if the player is a bot, controllerid will stay -1
                    if (!LocalLobbyManager.Instance.GetPlayer(i).IsBot)
                    {
                        controllerID = i;
                    }

                    Players[i].EnableControls(controllerID);
                }
            }
        }

        /// <summary>
        /// Disables the control for players locally
        /// </summary>
        public virtual void DisableControlForPlayers()
        {
            if (_isNetworkMode)
            {
                Debug.LogError("This function should not be called in network mode");
                return;
            }

            // We iterate throught LobbyManager to find active players with their control position
            for (int i = 0; i < LocalLobbyManager.Instance.Players().Count; i++)
            {
                if (Players.ContainsKey(i))
                {
                    Players[i].DisableControls();
                }
            }
        }

        /// <summary>
        /// Disables the control for a player locally
        /// </summary>
        public virtual void DisableControlForPlayer(BaseController controller)
        {
            if (_isNetworkMode)
            {
                Debug.LogError("This function should not be called in network mode");
                return;
            }

            controller.DisableControls();
        }

        /// <summary>
        /// Called when player change current selected camera.
        /// Loops throught each available camera
        /// </summary>
        protected virtual void OnCameraChange()
        {
            _currentCamera = (_currentCamera + 1) % CameraControllers.Length;
            ChangeActiveCameraController(_currentCamera);
        }

        /// <summary>
        /// Help with RaceManager in scene view
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
#if UNITY_EDITOR

            // Drawing checkpoints
            if (Checkpoints == null)
            {
                return;
            }

            if (Checkpoints.Length == 0)
            {
                return;
            }

            // for each point in the path
            for (int i = 0; i < Checkpoints.Length; i++)
            {
                if (Checkpoints[i] != null)
                {
                    // we draw a wireframe cube for the checkpoint
                    Gizmos.color = Color.blue;

                    Matrix4x4 currentMatrix = Gizmos.matrix;
                    Gizmos.matrix = Checkpoints[i].transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    Gizmos.matrix = currentMatrix;

                    Gizmos.DrawSphere(Checkpoints[i].transform.position, 0.5f);

                    // we draw a line towards the next point in the path
                    if ((i + 1) < Checkpoints.Length)
                    {
                        if (Checkpoints[i + 1] != null)
                        {
                            MMDebug.DrawGizmoArrow(Checkpoints[i].transform.position, Checkpoints[i + 1].transform.position - Checkpoints[i].transform.position, Color.blue);
                        }
                    }

                    // we draw a line from the first to the last point if we're looping
                    if (i == (Checkpoints.Length - 1))
                    {
                        if (ClosedLoopTrack)
                        {
                            MMDebug.DrawGizmoArrow(Checkpoints[i].transform.position, Checkpoints[0].transform.position - Checkpoints[i].transform.position, Color.blue);
                        }
                    }
                }
            }

#endif
        }
    }
}