using UnityEngine;
using System.Collections;
using System.Linq;
using MoreMountains.Tools;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// Base class controller.
	/// Must be used by vehicles specific controllers.
	/// Manages Score and input management.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class BaseController : MonoBehaviour, IActorInput
	{
		[Header("Bonus")]
		/// the force applied when the vehicle is in a boost zone
		public float BoostForce = 1f; 
		/// The temporary value of rigidbody.drag when the vehicle is inside a loop zone. This allows for better movement inside the loop.
		public float RigidBodyDragInLoop = 1f;

		[Header("Engine")]
		/// the speed at which the car steers 
		public float SteeringSpeed = 100f; 
		/// Set this to true if you want the vehicle to accelerate forever 
		public bool AutoForward = false;

		protected enum Zones { SpeedBoost, JumpBoost, LoopZone };
		protected Rigidbody _rigidbody;
		protected Collider _collider;
		protected RaceManager _raceManager;
		protected Transform[] _checkpoints;
		protected int _currentWaypoint = 0;
		protected int _lastWaypointCrossed = -1;
		protected float _defaultDrag;
		protected int _controllerId = -1;
		/// Ẁhen > 0, vehicle has finished the race. This is it final rank
		protected int _finisherPosition = 0; 

		/// <summary>
		/// Returns the current lap.
		/// </summary>
		public int CurrentLap {get; protected set;}

		/// <summary>
		/// Gets or sets the current steering amount from -1 (full left) to 1 (full right).
		/// 0 when none.
		/// </summary>
		/// <value>The current steering amount.</value>
		public float CurrentSteeringAmount {get; set;}

		protected float _currentGasPedalAmount;

		/// <summary>
		/// Gets or sets the current gas pedal amount.
		/// 1 for full acceleration. -1 for full brake or reverse. 0 for nothing.
		/// </summary>
		/// <value>The current gas pedal amount.</value>
		public float CurrentGasPedalAmount 
		{
			get 
			{ 
				if (AutoForward) 
				{
					// We need to find if this player is a bot
					VehicleAI ai = GetComponent<VehicleAI>();
					if (ai != null && ai.Active)
					{
						return _currentGasPedalAmount;
					}
					else 
					{
						// human players are always accelerating 
						return IsPlaying ? 1 : 0; 
					}
				}
				else
				{
					return _currentGasPedalAmount;
				}

			} 
			set { _currentGasPedalAmount = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this user is playing.
		/// </summary>
		/// <value><c>true</c> if this instance is playing; otherwise, <c>false</c>.</value>
		public virtual bool IsPlaying {get; protected set;}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is grounded.
		/// </summary>
		/// <value><c>true</c> if this instance is grounded; otherwise, <c>false</c>.</value>
		public virtual bool IsGrounded {get; protected set;}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is on speed boost.
		/// </summary>
		/// <value><c>true</c> if this instance is on speed boost; otherwise, <c>false</c>.</value>
		public virtual bool IsOnSpeedBoost {get; protected set;}

		/// <summary>
		/// Gets the vehicle speed.
		/// </summary>
		/// <value>The speed.</value>
		public virtual float Speed 
		{ 
			get { return _rigidbody.velocity.magnitude; } 
		}

		/// <summary>
		/// Gets the player score.
		/// </summary>
		/// <value>The score.</value>
		public virtual int Score 
		{
			get 
			{
				if (_checkpoints != null)
				{
					return (CurrentLap * _checkpoints.Length) + _currentWaypoint;
				}
				else
				{
					return 0;
				}
			}
		}

		/// <summary>
		/// Gets the distance to the next waypoint.
		/// </summary>
		/// <value>The distance to next waypoint.</value>
		public virtual float DistanceToNextWaypoint 
		{
			get 
			{
				if (_checkpoints.Length == 0)
				{
					return 0;
				}

				Vector3 checkpoint = _checkpoints[_currentWaypoint].position;
				return Vector3.Distance(transform.position, checkpoint);
			}
		}

		/// <summary>
		/// Gets the final rank.
		/// 0 if vehicle has not ended.
		/// </summary>
		/// <value>The final rank.</value>
		public int FinalRank
		{ 
			get { return _finisherPosition; }
		}

		/// <summary>
		/// Initializes various references
		/// </summary>
		protected virtual void Awake()
		{
			// Init managers
			_collider = GetComponent<Collider>();
			_raceManager = FindObjectOfType<RaceManager>();
			_rigidbody = GetComponent<Rigidbody>();

			IsOnSpeedBoost = false;
			_defaultDrag = _rigidbody.drag;
		}

		/// <summary>
		/// Initializes checkpoints
		/// </summary>
		protected virtual void Start() 
		{
			// We get checkpoints as an array of transform
			if (_raceManager != null) 
			{
				_checkpoints = _raceManager.Checkpoints.Select(x => x.transform).ToArray();
			}
		}

		/// <summary>
		/// Gets a value indicating whether this vehicle has just finished the race.
		/// This property will return true only once.
		/// </summary>
		/// <param name="finalRankPosition">The rank of this vehicle when going throught endline. This value will
		/// be stored and can be returned by FinalRank propery.</param>
		/// <value><c>true</c> if this vehicle has finished; otherwise, <c>false</c>.</value>
		public virtual bool HasJustFinished(int finalRankPosition) 
		{
			if (_finisherPosition > 0)
			{
				return false;
			}

			bool raceEndedForPlayer;
			raceEndedForPlayer = _raceManager.ClosedLoopTrack ? 
				(Score >= (_raceManager.Laps * _checkpoints.Length)) 
				: (Score >= _checkpoints.Length);
			
			if (raceEndedForPlayer)
			{
				_finisherPosition = finalRankPosition;
			}

			return raceEndedForPlayer;
		}

		#region IActorInput implementation

		// Manages User interactions from keyboard, joystick, touch joypad

		public virtual void MainActionPressed() 
		{
			CurrentGasPedalAmount = 1;
		}

		public virtual void MainActionDown() 
		{
			CurrentGasPedalAmount = 1;
		}

		public virtual void MainActionReleased() 
		{
			CurrentGasPedalAmount = 0;
		}

		public virtual void AltActionPressed()
		{
			CurrentGasPedalAmount = -1;
		}

		public virtual void AltActionDown()
		{
			CurrentGasPedalAmount = -1;
		}

		public virtual void AltActionReleased()
		{
			CurrentGasPedalAmount = 0;
		}

		public virtual void RespawnActionPressed()
		{
			Respawn();
		}

		public virtual void RespawnActionDown()
		{
			// nothing
		}

		public virtual void RespawnActionReleased()
		{
			// nothing
		}

		public virtual void LeftPressed() 
		{ 
			CurrentSteeringAmount = -1;
		}

		public virtual void RightPressed() 
		{ 
			CurrentSteeringAmount = 1;
		}

		public virtual void UpPressed() 
		{ 
			CurrentGasPedalAmount = 1;
		}

		public virtual void DownPressed() 
		{ 
			CurrentGasPedalAmount = -1;
		}

		public virtual void MobileJoystickPosition(Vector2 value)
		{
			CurrentSteeringAmount = value.x;
		}

		public virtual void HorizontalPosition(float value) 
		{
			CurrentSteeringAmount = value;
		}

		public virtual void VerticalPosition(float value) 
		{
			CurrentGasPedalAmount = value;
		}

		public virtual void LeftReleased()
		{ 
			CurrentSteeringAmount = 0;
		}

		public virtual void RightReleased()
		{ 
			CurrentSteeringAmount = 0;
		}

		public virtual void UpReleased()
		{
			CurrentGasPedalAmount = 0;
		}

		public virtual void DownReleased()
		{ 
			CurrentGasPedalAmount = 0;
		}

		#endregion

		/// <summary>
		/// This method triggers the respawn of the vehicle to its last checkpoint
		/// </summary>
		public virtual void Respawn()
		{
			// Must be overriden in child classes
		}

		/// <summary>
		/// Describes what happens when the object starts colliding with something
		/// Used for checkpoint interaction
		/// </summary>
		/// <param name="other">Other.</param>
		public virtual void OnTriggerEnter(Collider other) 
		{
			// Vehicle just crossed a checkpoint
			if (other.tag == "Checkpoint") 
			{
				int newLap = CurrentLap;
				int newWaypoint = _currentWaypoint;

				// If this checkpoint was the next checkpoint for this vehicle 
				if (_checkpoints[_currentWaypoint] == other.transform && ((_lastWaypointCrossed + 1) == _currentWaypoint)) 
				{
					newWaypoint++;
					_lastWaypointCrossed++;
				}

				// If this was the last checkpoint for the lap
				if (newWaypoint == _checkpoints.Length) 
				{
					newLap++;
					newWaypoint = 0;
					_lastWaypointCrossed = -1;
				}

				_currentWaypoint = newWaypoint;
				CurrentLap = newLap;
			}
		}

		/// <summary>
		/// Describes what happens when something is colliding with our object
		/// Used to apply a boost force to the vehicle while staying in a boost zone.
		/// </summary>
		/// <param name="other">Other.</param>
		public virtual void OnTriggerStay(Collider other) 
		{
			if (other.tag == Zones.SpeedBoost.ToString())
			{
				// While in speedboost, we accelerate vehicle
				_rigidbody.AddForce(transform.forward * BoostForce, ForceMode.Impulse);
				IsOnSpeedBoost = true;

			} 

			if (other.tag == Zones.JumpBoost.ToString())
			{
				_rigidbody.AddForce(transform.up * BoostForce, ForceMode.Impulse);
				IsOnSpeedBoost = true;
			}

			if (other.tag == Zones.LoopZone.ToString())
			{
				_rigidbody.drag = RigidBodyDragInLoop;
			}
		}

		/// <summary>
		/// Describes what happens when the collision ends
		/// Removes "boost" state when the vehicle exits a boost zone
		/// </summary>
		/// <param name="other">Other.</param>
		public virtual void OnTriggerExit(Collider other)
		{
			if (other.tag == Zones.SpeedBoost.ToString() || other.tag == Zones.JumpBoost.ToString())
			{
				IsOnSpeedBoost = false;
			}
			if (other.tag == Zones.LoopZone.ToString())
			{
				// We reset physics
				_rigidbody.drag = _defaultDrag;
			}
		}

		/// <summary>
		/// Enables the controls.
		/// </summary>
		/// <param name="controllerId">Controller identifier.</param>
		public virtual void EnableControls(int controllerId) 
		{
			IsPlaying = true;
			CurrentSteeringAmount = 0;
			CurrentGasPedalAmount = 0;
			_controllerId = controllerId;

			// If player is not a bot
			if (_controllerId != -1) 
			{
				InputManager.Instance.SetPlayer(_controllerId, this);
			}
		}

		/// <summary>
		/// Disables the controls.
		/// </summary>
		public virtual void DisableControls() 
		{
			IsPlaying = false;
			CurrentSteeringAmount = 0;
			CurrentGasPedalAmount = 0;

			// If player is not a bot
			if (_controllerId != -1) 
			{
				InputManager.Instance.DisablePlayer(_controllerId);
			}
		}
	}
}
