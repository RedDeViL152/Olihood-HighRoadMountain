using UnityEngine;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// This class handles smoke emission when the gas pedal is on
	/// </summary>
	public class SolidSmokesBehaviour : SolidBehaviourExtender 
	{
		/// The particle system to use to emit smoke
		public ParticleSystem Smokes;

		protected ParticleSystem.EmissionModule _smokes;

		public override void Initialization()
		{
			_smokes = Smokes.emission;
			_smokes.enabled = false;
		}
		
		public override void Update()
		{
			if (_controller.CurrentGasPedalAmount > 0) 
			{
				_smokes.enabled = true;
			} 
			else 
			{
				_smokes.enabled = false;
			}
		}
	}
}
