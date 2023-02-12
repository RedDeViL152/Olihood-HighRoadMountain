using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{	
	/// <summary>
	/// This class handles skidmarks production for a wheel attached to a SolidWheel
	/// </summary>
	[RequireComponent(typeof(SolidWheelBehaviour))]
	public class SolidWheelSkidmarks : MonoBehaviour
	{
		/// The speed below which wheels don't leave skidmarks
		public float MinimalSkidMarksSlideSpeed = 5f;
		/// The maximal amount of skidmarks generated
		public float MaxSkidIntensity = 10f;

		protected SkidmarksManager SkidMarksObject;
		protected SolidWheelBehaviour _wheel;
		protected int lastSkid = 0;

		/// <summary>
		/// We look for the SkidmarksManager that must be in the scene to function
		/// One Skidmarks manager is enough for all vehicles
		/// </summary>
		public virtual void Start()
		{
			SkidMarksObject = Object.FindObjectOfType<SkidmarksManager>();

			if (SkidMarksObject == null)
			{
				Debug.LogWarning("This class needs a SkidmarksManager in the scene. Please add one.");
			}
				
			_wheel = GetComponent<SolidWheelBehaviour>();
		}
			
		/// <summary>
		/// Updates the skidmark's properties
		/// </summary>
		public virtual void Update()
		{
			if (SkidMarksObject != null)
			{
				float slideSpeed = _wheel.VehicleController.SlideSpeed;

				// If the wheel touches the ground and is at the proper speed
				if (_wheel.IsGrounded && (Mathf.Abs(slideSpeed) > MinimalSkidMarksSlideSpeed))
				{
					Vector3 velocity = _wheel.VehicleController.GetComponent<Rigidbody>().velocity;
					float intensity = Mathf.Clamp01(Mathf.Abs(slideSpeed) / MaxSkidIntensity);
					Vector3 skidPoint = _wheel.PhysicsHit.point;
					lastSkid = SkidMarksObject.AddSkidMark(skidPoint, _wheel.PhysicsHit.normal, intensity, lastSkid);
				} 
				else
				{
					// we reset the last skid segment's ID to to start a new skidmark
					lastSkid = -1;
				}
			}
		}
	}
}
