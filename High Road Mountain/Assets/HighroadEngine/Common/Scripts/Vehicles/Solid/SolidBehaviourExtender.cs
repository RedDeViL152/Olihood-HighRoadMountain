using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// A utility class allowing you to extend the behaviour of a Solid vehicle.
	/// For example to add lights, skidmarks management, etc.
	/// You just have to implement the update and initialization if necessary.
	/// </summary>
	[RequireComponent(typeof(SolidController))]
	public abstract class SolidBehaviourExtender : MonoBehaviour 
	{
		protected SolidController _controller;

		/// <summary>
		/// Controller's initialization
		/// </summary>
		public virtual void Start()
		{
			_controller = GetComponent<SolidController>();
			_controller.OnCollisionEnterWithOther += OnVehicleCollisionEnter;
			Initialization();
		}

		/// <summary>
		/// Use this method to initialize objects in subclasses
		/// </summary>
		public virtual void Initialization()
		{
			// Nothing here
		}
		
		/// <summary>
		/// Update this method in subclasses
		/// </summary>
		public abstract void Update();

		/// <summary>
		/// Use this method to describe what happens when the vehicle collides with something
		/// </summary>
		/// <param name="tag">Objet en collision</param>
		public virtual void OnVehicleCollisionEnter(Collision other)
		{
			// Nothing here
		}
	}
}