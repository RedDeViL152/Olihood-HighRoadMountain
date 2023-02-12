using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// This class handles particle emission when colliding with something
	/// </summary>
	public class SolidObstacleParticlesEmitter : SolidBehaviourExtender  
	{
		/// A reference to the particle emission prefab
		public ParticleSystem Sparkles;
		/// The minimal speed required to start emitting particles 
		public int MinimalSpeedForSparkles = 5;

		/// <summary>
		/// On Init we stop the particle emission
		/// </summary>
		public override void Initialization()
		{
			if (Sparkles != null)
			{
				Sparkles.Stop();
			}

		}

		public override void Update()
		{
			// nothing
		}

		/// <summary>
		/// When colliding with another object, if the speed is high enough, we play our sparkles
		/// </summary>
		/// <param name="other">Other.</param>
		public override void OnVehicleCollisionEnter(Collision other)
		{
			if (Sparkles != null)
			{
				if (_controller.Speed > MinimalSpeedForSparkles)
				{
					Sparkles.transform.position = other.contacts[0].point;
					Sparkles.Play();
				}
			}
		}
	}
}

