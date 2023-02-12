using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// This class allows for a fast demo scene boot, without a RaceManager
    /// You just have to put it in a scene for the cameras to auto bind themselves to the vehicle
    /// and for that vehicle to be bound to Player1's input.
    /// If more than one vehicle/camera are present in the scene and active, a button appears in the GUI
    /// to switch between vehicles and cameras
    /// Disabling FindCamerasInScene and FindVehiclesInScene allows you to associate cameras and vehicles manually via the Cameras and Vehicles lists
    /// </summary>
    public class DemonstratorManager : MonoBehaviour
    {
        [Header("Configuration")]
        /// Hide or show GUI Tool
        public bool ShowGui = true;
        [Header("Objects")]
        /// Whether or not this manager should find all active cameras in the scene
        public bool FindCamerasInScene = true;
        /// The "manual" list of cameras if you'd rather force that
        public CameraController[] Cameras;
        /// Whether or not the manager should find all active vehicles in the scene
        public bool FindVehiclesInScene = true;
        /// The "manual" list of vehicles to use
        public BaseController[] Vehicles;

        protected int _wantedCameraIndex = 0;
        protected int _wantedVehicleIndex = 0;
        protected int _currentCameraIndex = -1;
        protected int _currentVehicleIndex = -1;

        /// <summary>
        /// Objects initialization
        /// </summary>
        public virtual void Start()
        {
            HideUI();

            if (FindCamerasInScene)
            {
                Cameras = Object.FindObjectsOfType<CameraController>();
                if (Cameras.Length == 0)
                {
                    Debug.LogWarning("No Cameras found in the scene. Please add at least one active Gameobject with a CameraController component.");
                }

                foreach (CameraController c in Cameras)
                {
                    c.gameObject.SetActive(false);
                }

                _wantedCameraIndex = Cameras.Length - 1;
            }

            if (FindVehiclesInScene)
            {
                Vehicles = Object.FindObjectsOfType<BaseController>();

                if (Vehicles.Length == 0)
                {
                    Debug.LogWarning("No Vehicle found in the scene. Please add at least one active Gameobject with a BaseController component.");
                }

                foreach (BaseController v in Vehicles)
                {
                    v.gameObject.SetActive(false);
                }
            }


        }

        /// <summary>
        /// Updates active camera and vehicle if needed
        /// </summary>
        public virtual void Update()
        {
            if (_currentVehicleIndex != _wantedVehicleIndex && Vehicles.Length > 0)
            {
                if (_currentVehicleIndex >= 0 && Vehicles[_currentVehicleIndex] != null)
                {
                    Vehicles[_currentVehicleIndex].DisableControls();
                    Vehicles[_currentVehicleIndex].gameObject.SetActive(false);
                }

                if (Vehicles[_wantedVehicleIndex] != null)
                {
                    Vehicles[_wantedVehicleIndex].gameObject.SetActive(true);
                    Vehicles[_wantedVehicleIndex].EnableControls(0);
                }

                _currentVehicleIndex = _wantedVehicleIndex;
                RefreshCamera();
            }

            if (_currentCameraIndex != _wantedCameraIndex && Cameras.Length > 0)
            {
                RefreshCamera();
                _currentCameraIndex = _wantedCameraIndex;
            }
        }

        /// <summary>
        /// Masks UI controls if necessary
        /// </summary>
        public virtual void HideUI()
        {
            GameObject ui = GameObject.Find("UI");

            if (ui != null)
            {
                ui.SetActive(false);
            }
        }


        /// <summary>
        /// Updates the active camera
        /// </summary>
        public virtual void RefreshCamera()
        {
            if (Cameras.Length > 0)
            {
                if (_currentCameraIndex >= 0 && Cameras[_currentCameraIndex] != null)
                {
                    Cameras[_currentCameraIndex].gameObject.SetActive(false);
                }
                if (Cameras[_wantedCameraIndex] != null)
                {
                    Cameras[_wantedCameraIndex].gameObject.SetActive(true);
                    Cameras[_wantedCameraIndex].HumanPlayers = new Transform[] {
                        Vehicles[_currentVehicleIndex].transform
                    };
                    Cameras[_wantedCameraIndex].RefreshTargets();
                }
            }
        }

        /// <summary>
        /// Displays the UI controls allowing to change camera and vehicle
        /// </summary>
        protected virtual void OnGUI()
        {
            if (ShowGui)
            {
                if (Cameras.Length > 1)
                {
                    if (GUILayout.Button("Change camera"))
                    {
                        _wantedCameraIndex = (_wantedCameraIndex + 1) % Cameras.Length;
                    }
                }

                if (_currentVehicleIndex >= 0)
                {
                    if (GUILayout.Button(("Reset vehicle position")))
                    {
                        Vehicles[_currentVehicleIndex].Respawn();
                    }
                }

                if (Vehicles.Length > 1)
                {
                    if (GUILayout.Button("Change vehicle"))
                    {
                        _wantedVehicleIndex = (_wantedVehicleIndex + 1) % Vehicles.Length;
                    }
                }
            }
        }
    }
}