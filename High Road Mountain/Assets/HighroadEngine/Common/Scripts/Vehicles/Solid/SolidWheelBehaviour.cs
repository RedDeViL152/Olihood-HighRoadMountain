using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.HighroadEngine
{		
	/// <summary>
	/// This class handles physics behaviour (suspension mostly) and wheel display based on current vehicle dynamics
	/// </summary>
	public class SolidWheelBehaviour : MonoBehaviour 
	{
		[Header("Configuration")]
		/// Reference to Solid Controller parent
		public SolidController VehicleController;
		/// Reference to Wheel Position gameObject
		public Transform WheelPosition;
		/// Reference to Wheel Model gameobject
		public Transform WheelModel; 
		[MMInformation("Set this to true if this wheel rotates when turning the car's wheel.\n", MMInformationAttribute.InformationType.Info, false)]
		public bool SteeringWheel;
		/// Rotation direction of the wheel 
		public enum RollingOrientationEnum { Normal, Inverse }
		[MMInformation("This boolean controls the rotation direction of the wheel.\n", MMInformationAttribute.InformationType.Info, false)]
		public RollingOrientationEnum RollingOrientation;

		[Header("Wheel behaviour")]
		/// Multiplier factor for the wheel's rotation. Depends also on the vehicle's speed
		public int WheelRotationSpeed = 600;
		[Range(0.1f, 50f)]
		/// Maximum rotation angle for the wheel's direction tree, based on the car's wheel's direction
		public float MaximumWheelSteeringAngle = 30;
		/// If this is true, the wheel touches the ground
		public bool IsGrounded { get; protected set;}
		/// Contact point between the road and the wheel at physic's Update
		public RaycastHit PhysicsHit { get { return _physicsHit; } }

		protected RaycastHit _physicsHit;
		protected Vector3 _wheelTargetPosition;
		protected Rigidbody _rigidbody;
		protected RaycastHit _updateHit;
		// Suspension values
		protected float _previousLength;
		protected float _currentLength;
		protected float _springVelocity;
		protected float _wheelDistance; // Target distance including the suspension's height and the wheel's height
		protected LayerMask _noLayerMask = ~0;

		/// <summary>
		/// Instance initialization.
		/// </summary>
		protected virtual void Start() 
		{
			_rigidbody = VehicleController.GetComponent<Rigidbody>();
		}
		
		/// <summary>
		/// Updates the spring's physics for the vehicle's dynamic behaviour
		/// </summary>
		protected virtual void FixedUpdate() 
		{
			IsGrounded = false;
			_wheelDistance = VehicleController.RestLength + VehicleController.RadiusWheel;

			//  we cast a ray towards the ground to know the distance between the vehicle and the ground
			if (Physics.Raycast(transform.position, -transform.up, out _physicsHit, _wheelDistance, _noLayerMask, QueryTriggerInteraction.Ignore)) 
			{
				// if the ground is closer than the desired distance or at the right distance
				IsGrounded = true;
				SpringPhysics();
				EnginePhysics();
			}
		}
			
		/// <summary>
		/// Updates the car position / independent from physics
		/// </summary>
		protected virtual void Update()
		{
			if (WheelPosition != null) 
			{
				UpdateWheelHeight();			
				UpdateWheelAngle();
				UpdateWheelRolling();
			}
		}

		/// <summary>
		/// Applies the spring physics.
		/// </summary>
		protected virtual void SpringPhysics()
		{
			// we store our previous state
			_previousLength = _currentLength;
			// we compute the new length
			_currentLength = _wheelDistance - PhysicsHit.distance;
			// we compute the difference between both lengths
			_springVelocity = (_currentLength - _previousLength) / Time.fixedDeltaTime;
			// we update the spring force based on that
			float SpringForce = VehicleController.SpringConstant * _currentLength;
			// we update our damper force
			// the lower the difference between current and previous, the lower the correction
			float DamperForce = VehicleController.DamperConstant * _springVelocity;
			// we apply our force towards the vehicle's up
			Vector3 springVector = transform.up * (SpringForce + DamperForce);
			_rigidbody.AddForceAtPosition(springVector, transform.position);
		}

		/// <summary>
		/// Applies acceleration force
		/// </summary>
		protected virtual void EnginePhysics()
		{
			_rigidbody.AddForceAtPosition(VehicleController.CurrentEngineForceValue * Time.fixedDeltaTime, 
				PhysicsHit.point + (transform.up * VehicleController.ForwardForceHeight), 
				ForceMode.Acceleration);
		}

		/// <summary>
		/// Updates the height of the wheel.
		/// </summary>
		protected virtual void UpdateWheelHeight()
		{
			if (Physics.Raycast(transform.position, -transform.up, out _updateHit, _wheelDistance))
			{
				_wheelTargetPosition = transform.position - transform.up * (_updateHit.distance - VehicleController.RadiusWheel);
				// if the wheel is buried in the ground, we force its position at ground level
				// otherwise we lerp towards the desired position
				if (WheelPosition.position.y >= _wheelTargetPosition.y)
				{
					_wheelTargetPosition.y = Mathf.Lerp(WheelPosition.position.y, _wheelTargetPosition.y, Time.deltaTime * 4);
				}
			}
			else
			{
				// if the ground is too far, the wheel is set at its lowest position
				_wheelTargetPosition = transform.position - transform.up * (VehicleController.RestLength);
				_wheelTargetPosition.y = Mathf.Lerp(WheelPosition.position.y, _wheelTargetPosition.y, Time.deltaTime * 2);
			}
			WheelPosition.position = _wheelTargetPosition;
		}

		/// <summary>
		/// Updates the wheel angle.
		/// </summary>
		protected virtual void UpdateWheelAngle()
		{
			if (SteeringWheel)
			{
				if (VehicleController.CurrentSteeringAmount != 0)
				{
					WheelPosition.transform.localEulerAngles = (VehicleController.CurrentSteeringAmount * MaximumWheelSteeringAngle * Vector3.up);
				}
				else
				{
					WheelPosition.transform.localEulerAngles = Vector3.zero;
				}
			}
		}

		/// <summary>
		/// Makes the wheel roll based on the current speed
		/// </summary>
		void UpdateWheelRolling()
		{
			Vector3 rotationAmount = Vector3.zero;

			if (VehicleController.CurrentGasPedalAmount != 0)
			{
				rotationAmount = Vector3.right * WheelRotationSpeed * Time.deltaTime * VehicleController.NormalizedSpeed * Mathf.Sign(VehicleController.CurrentGasPedalAmount);
				rotationAmount *= RollingOrientation == RollingOrientationEnum.Normal ? -1f : 1f;
			}
			else if (VehicleController.IsGrounded)
			{
				// We just follow velocity direction 
				rotationAmount = Vector3.right * WheelRotationSpeed * Time.deltaTime * VehicleController.NormalizedSpeed;
				// Findind rolling orientation
				rotationAmount *= VehicleController.Forward == (RollingOrientation == RollingOrientationEnum.Normal) ? -1f : 1f;
			}
		
			WheelModel.Rotate(rotationAmount);
		}
			
		/// <summary>
		/// Gizmos draws
		/// </summary>
		public virtual void OnDrawGizmos() 
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(transform.position, PhysicsHit.point);

			Gizmos.color = Color.red;
		
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, transform.position -transform.up * (VehicleController.RestLength + VehicleController.RadiusWheel));

			Gizmos.color = Color.magenta;
			Gizmos.DrawLine (PhysicsHit.point+ transform.up * VehicleController.ForwardForceHeight, (PhysicsHit.point + transform.up * VehicleController.ForwardForceHeight)  + VehicleController.CurrentEngineForceValue / 100);

			Gizmos.DrawWireSphere (_updateHit.point, 0.3f);
		}
	}
}