using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.HighroadEngine;
using UnityEngine.Events;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// Solid Controller class, that handles vehicles with suspension
	/// This allows for a more dynamic behaviour of the vehicles and allows for bumpy roads, slopes or even loops
	/// This controller also offers easier extendability thanks to the SolidBehaviourExtender.
	/// If you want to create a vehicle using this controller, you'll need to setup its suspension correctly, and pay attention to the weight repartition.
	/// For that, you can simply duplicate one of the demo vehicles, or have a look at the documentation that explains how to setup a vehicle, step by step.
	/// </summary>
	public class SolidController : BaseController 
	{
		/// The engine's power
		public float EngineForce = 1000;
		[Header("Vehicule Physics")]
		/// Point of gravity of the car is set below. This helps the Unity Physics with car stability
		public Vector3 CenterOfMass = new Vector3(0, -1, 0); 
		[Range(0.0f,5f)]
		/// The distance to the ground at which we consider the car is grounded
		public float GroundDistance = 1f;
		[Range(1,10)]
		/// the penalty applied when going offroad
		public float OffroadPenaltyFactor = 2f;
		/// the wheel's grip force. The higher the value, the less the car will slide when turning
		public float CarGrip = 10f;
		/// The speed above which the vehicle is considered as going full throttle. The vehicle's speed can be higher than that
		public float FullThrottleVelocity = 30;
		[Range(0.0f,5f)]
		/// The minimum required speed for the vehicle to turn
		public float MinimalTurningSpeed = 1f;
		[Range(-5.0f,5f)]
		/// The height at which forward force will be applied
		public float ForwardForceHeight = 1f;
		/// Additional torque force based on speed
		public AnimationCurve TorqueCurve;
		/// Rotation force when going backward
		public AnimationCurve BackwardForceCurve;
		[Range(0.0f,1f)]
		/// Grip factor multiplier. The higher that value, the more this vehicle will stick to the road, even at high speeds
		public float GripSpeedFactor = 0.02f;
		[Range(0,200)]
		/// The vehicle's maximum grip value. 
		public int MaxGripValue = 100;

		[Header("Suspension System")]
		/// The size of the wheel
		public float RadiusWheel = 0.5f;
		/// Spring
		public float SpringConstant = 20000f;
		/// Damper
		public float DamperConstant = 2000f;
		/// The length of the suspension spring when resting
		public float RestLength = 0.5f;
		/// The horizontal rotation force that will angle the car left or right to simulate spring compression when turning
		public float SpringTorqueForce = 1000f;
		/// An event triggered when the vehicle collides with something
		public UnityAction<Collision> OnCollisionEnterWithOther;
        /// An event triggered when the vehicle is respawned
        public UnityAction OnRespawn;

		protected float _springForce = 0f;
		protected float _damperForce = 0f;
		protected RaycastHit _hit;
		protected Vector3 _startPosition;
		protected Quaternion _startRotation;
		protected GameObject _groundGameObject;
		protected LayerMask _noLayerMask = ~0;

		/// Gears enum. Car can be forward driving or backward driving (reverse)
		public enum Gears {forward, reverse}
		/// The current gear value
		public Gears CurrentGear {get; protected set;}
		/// Current engine force value used by wheels
		public Vector3 CurrentEngineForceValue { get; protected set;}
		/// Gets a value indicating whether this car is offroad.
		public virtual bool IsOffRoad 
		{ 
			get { return (_groundGameObject != null && _groundGameObject.tag == "OffRoad"); }
		}
		/// <summary>
		/// Gets the normalized speed.
		/// </summary>
		/// <value>The normalized speed.</value>
		public virtual float NormalizedSpeed 
		{
			get { return Mathf.InverseLerp(0f, FullThrottleVelocity, Mathf.Abs(Speed)); }
		}
		/// <summary>
		/// Returns true if vehicle is going forward
		/// </summary>
		/// <value><c>true</c> if forward; otherwise, <c>false</c>.</value>
		public virtual bool Forward 
		{
			get { return transform.InverseTransformDirection(_rigidbody.velocity).z > 0; }
		}
		/// <summary>
		/// Returns true if vehicle is braking
		/// </summary>
		/// <value><c>true</c> if braking; otherwise, <c>false</c>.</value>
		public virtual bool Braking 
		{
			get { return Forward && (CurrentGasPedalAmount < 0); }
		}
		/// <summary>
		/// Returns the current angle of the car to the horizon.
		/// Used, for example, to disable AI's direction when the vehicle goes over a certain angle.
		/// allows for easier loop handling
		/// </summary>
		/// <value>The horizontal angle.</value>
		public virtual float HorizontalAngle
		{
			get { return Vector3.Dot(Vector3.up, transform.up); }
		}

		/// <summary>
		/// Gets the forward normalized speed.
		/// Used to evaluate the engine's power
		/// </summary>
		/// <value>The forward normalized speed.</value>
		public virtual float ForwardNormalizedSpeed
		{
			get
			{
				float forwardSpeed = Vector3.Dot(transform.forward, _rigidbody.velocity);
				return Mathf.InverseLerp(0f, FullThrottleVelocity, Mathf.Abs(forwardSpeed));
			}
		}

		/// The current lateral speed value of the vehicle
		public virtual float SlideSpeed {get; protected set;}

		/// <summary>
		/// Physics initialization
		/// </summary>
		protected override void Awake() 
		{
			base.Awake();

			// we change the center of mass below the vehicle to help with unity physics stability
			_rigidbody.centerOfMass += CenterOfMass;

			CurrentGear = Gears.forward;
		}

        /// <summary>
        /// Unity start function
        /// </summary>
        protected override void Start()
        {
            base.Start();
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        /// <summary>
        /// Update main function
        /// </summary>
        protected virtual void Update() 
		{
			UpdateGroundSituation();

			/*	MMDebug.DebugOnScreen("Steering", CurrentSteeringAmount);
			MMDebug.DebugOnScreen("acceleration", CurrentGasPedalAmount);
			MMDebug.DebugOnScreen("Speed", Speed);
			MMDebug.DebugOnScreen("SlideSpeed", SlideSpeed);
			MMDebug.DebugOnScreen("IsGrounded", IsGrounded);
			MMDebug.DebugOnScreen("Forward", Forward);
			MMDebug.DebugOnScreen("Braking", Braking);
			MMDebug.DebugOnScreen("_engineForce", CurrentEngineForceValue);
			MMDebug.DebugOnScreen("_rotationForce", CurrentRotationForceValue);
			MMDebug.DebugOnScreen("ForwardNormalizedSpeed", ForwardNormalizedSpeed);
			MMDebug.DebugOnScreen("HorizontalAngle", HorizontalAngle);*/
		}

		/// <summary>
		/// Updates the ground situation for this car.
		/// </summary>
		protected virtual void UpdateGroundSituation() 
		{
			IsGrounded = Physics.Raycast(transform.position, -transform.up, out _hit, GroundDistance, _noLayerMask, QueryTriggerInteraction.Ignore) ? true : false;
			_groundGameObject = _hit.transform != null ? _hit.transform.gameObject : null;
		}

		/// <summary>
		/// Fixed update.
		/// We apply physics and input evaluation.
		/// </summary>
		protected virtual void FixedUpdate() 
		{
			UpdateEngineForceValue();

			UpdateSlideForce();

			UpdateTorqueRotation();

			UpdateAirRotation();
		}

		/// <summary>
		/// Computes the engine's power. This value can be used by a wheel to apply force if conditions are met
		/// </summary>
		protected virtual void UpdateEngineForceValue()
		{
			// we use this intermediary variable to account for backwards mode
			float gasPedalForce = CurrentGasPedalAmount;

			if (IsOffRoad) 
			{
				gasPedalForce /= OffroadPenaltyFactor;
			}

			// if the player is accelerating
			if (CurrentGasPedalAmount > 0)
			{
				CurrentGear = Gears.forward;
			}

			// if the player is braking
			if (CurrentGasPedalAmount < 0)
			{
				// if it's fast enough, the car starts braking
				if ((Speed > MinimalTurningSpeed) && (CurrentGear == Gears.forward) && Forward)
				{
					// braking
				} else
				{
					// Otherwise, car is slow enough to go reverse
					CurrentGear = Gears.reverse;

					// We apply going backward penalty
					gasPedalForce = -BackwardForceCurve.Evaluate(-gasPedalForce);
				}
			}

			CurrentEngineForceValue = (Quaternion.AngleAxis(90, transform.right) * _hit.normal * (EngineForce * gasPedalForce));
		}

		/// <summary>
		/// Applies a torque force to the vehicle when the user wants to turn
		/// </summary>
		protected virtual void UpdateTorqueRotation()
		{
			if (IsGrounded)
			{ 
				Vector3 torque = transform.up * Time.fixedDeltaTime * _rigidbody.mass * TorqueCurve.Evaluate(NormalizedSpeed) * SteeringSpeed;
				// Going backward, we invert steering
				if (CurrentGear == Gears.reverse) 
				{
					torque = -torque;
				}
				_rigidbody.AddTorque(torque * CurrentSteeringAmount);
				// Horizontal torque. Simulates spring compression.
				_rigidbody.AddTorque(transform.forward * SpringTorqueForce * Time.fixedDeltaTime * _rigidbody.mass * CurrentSteeringAmount * ForwardNormalizedSpeed);
			}
		}

		/// <summary>
		/// Applies slide force to the vehicle
		/// </summary>
		protected virtual void UpdateSlideForce()
		{
			if (IsGrounded)
			{
				// We store the horizontal velocity
				Vector3 flatVelocity = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);

				// we compute our lateral speed value
				// we store it so it can be used by skidmarks
				SlideSpeed = Vector3.Dot(transform.right, flatVelocity);

				// we compute the vehicle's grip value based on speed and settings
				float grip = Mathf.Lerp(MaxGripValue, CarGrip, Speed * GripSpeedFactor);

				Vector3 slideForce = transform.right * (-SlideSpeed * grip);
				_rigidbody.AddForce(slideForce * Time.fixedDeltaTime * _rigidbody.mass);
			}
		}

		/// <summary>
		/// Handles rotation of the vehicle in the air
		/// </summary>
		protected virtual void UpdateAirRotation()
		{
			if (!IsGrounded)
			{
				// Slow turning in air
				if (Speed > MinimalTurningSpeed)
				{
					Vector3 airRotationForce = transform.up * CurrentSteeringAmount * SteeringSpeed * Time.fixedDeltaTime * _rigidbody.mass;
					_rigidbody.AddTorque(airRotationForce);
				}
			}
		}

		/// <summary>
		/// Resets the position of the vehicle.
		/// </summary>
		public override void Respawn()
		{
			Vector3 resetPosition;
			Quaternion resetRotation;

			// Getting current reset position 
			if (Score == 0)
			{
				resetPosition = _startPosition;
				resetRotation = _startRotation;
			}
			else 
			{
				Transform resetTransform = _currentWaypoint == 0 ? _checkpoints[_checkpoints.Length - 1] : _checkpoints[_currentWaypoint - 1];
				resetPosition = resetTransform.position;
				resetRotation = resetTransform.rotation;
			}

			_rigidbody.velocity = Vector3.zero;
			transform.position = resetPosition;
			transform.rotation = resetRotation;

            OnRespawn();
        }

		/// <summary>
		/// Raises the collision enter event.
		/// </summary>
		/// <param name="other">Other object.</param>
		protected virtual void OnCollisionEnter(Collision other)
		{
			if (OnCollisionEnterWithOther != null) 
			{
				OnCollisionEnterWithOther(other);
			}
		}

		/// <summary>
		/// Draws debug info
		/// </summary>
		protected virtual void OnDrawGizmos() 
		{
			// distance to ground
			Gizmos.color = Color.green;
			Gizmos.DrawLine (transform.position, transform.position - (transform.up * (GroundDistance)));
		}
	}
}