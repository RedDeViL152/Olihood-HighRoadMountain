using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.HighroadEngine 
{
	/// <summary>
	/// Camera controller base class. Meant to be extended.
	/// </summary>
	public abstract class CameraController : MonoBehaviour 
	{
		/// If set to true, means this camera can only follow one vehicle.
		/// This value must be overrided by subclasses
		public abstract bool IsSinglePlayerCamera {get;}
		/// Define when camera movement is done
		public enum UpdateType { FixedUpdate, LateUpdate, Update }
		public UpdateType UpdateMode;
		/// List of human players
		public Transform[] HumanPlayers;
		/// List of bot players.
		public Transform[] BotPlayers;

		protected Camera _camera;

		/// <summary>
		/// Gets or sets a value indicating whether this game has started.
		/// </summary>
		/// <value><c>true</c> if game has started; otherwise, <c>false</c>.</value>
		public bool GameHasStarted {get; set;}

		/// <summary>
		/// Initializes the camera gameobject
		/// </summary>
		protected virtual void Awake() 
		{
			_camera = GetComponentInChildren<Camera>();
			GameHasStarted = false;
		}

		/// <summary>
		/// Override this method to refresh the list of targets.
		/// </summary>
		public abstract void RefreshTargets();

		/// <summary>
		/// Override this method to implement camera movement.
		/// </summary>
		protected abstract void CameraUpdate();

		/// <summary>
		/// Unity Update
		/// </summary>
		protected virtual void Update()
		{
			if (UpdateMode == UpdateType.Update)
			{
				CameraUpdate();
			}
		}

		/// <summary>
		/// Unity LateUpdate
		/// </summary>
		protected virtual void LateUpdate()
		{
			if (UpdateMode == UpdateType.LateUpdate)
			{
				CameraUpdate();
			}
		}

		/// <summary>
		/// Unity FixedUpdate
		/// </summary>
		protected virtual void FixedUpdate()
		{
			if (UpdateMode == UpdateType.FixedUpdate)
			{
				CameraUpdate();
			}
		}
	}
}