using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// This class handles vehicle's sound : motor sound and crash sound
	/// </summary>
	public class SolidSoundBehaviour : SolidBehaviourExtender 
	{
		[Header("Sounds")]
		public AudioClip EngineSound;
		public AudioClip CrashSound;
		[Range(0.1f,10f)]
		/// Sound volume of the engine
		public float EngineVolume = 0.5f;
		[Range(0f,5f)]
		/// No crash sound when vehicule is below this speed
		public float MinimalCrashSpeed = 2.0f;
		[Range(1f,10f)]
		/// Maximum pitch value for the engine sound
		public float EngineMaxPitch = 5.5f;
		[Range(0f,1f)]
		/// Speed is multiplied by this factor when calculating engine sound pitch value
		public float SpeedFactor = 0.1f;

		protected SoundManager _soundManager;
		protected AudioSource _engineSound;
		protected float _engineSoundPitch;

		public override void Initialization()
		{
			if (EngineSound != null)
			{
				_soundManager = FindObjectOfType<SoundManager>();
				if (_soundManager != null)
				{
					_engineSound = _soundManager.PlayLoop(EngineSound, transform.position);

					if (_engineSound != null)
					{
						_engineSoundPitch = _engineSound.pitch;
						_engineSound.volume = EngineVolume;
					}
				} 
				else
				{
					Debug.LogWarning("Missing SoundManager Object in scene. Please add one.");
				}					
			}
		}
		
		public override void Update()
		{
			if (_engineSound == null)
			{
				return;
			}
			_engineSound.pitch = Mathf.Min(EngineMaxPitch, Mathf.Max(_engineSoundPitch, _engineSoundPitch * _controller.Speed * SpeedFactor));
		}

		public override void OnVehicleCollisionEnter(Collision other)
		{
			if (CrashSound != null)
			{
				if (other.gameObject.layer != LayerMask.NameToLayer("Ground"))
				{
					if (_soundManager != null)
					{
						if (other.relativeVelocity.magnitude >= MinimalCrashSpeed) 
						{
							_soundManager.PlaySound(CrashSound, transform.position, true);
						}
					}
				}
			}
		}
	}
}
