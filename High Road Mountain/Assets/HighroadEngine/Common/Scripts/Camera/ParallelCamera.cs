using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.HighroadEngine
{
	/// <summary>
	/// A camera parallel to the target. The car's distance is parametrable.
	/// A Mouse mode allows for moving the target point by drag and drop in the game scene
	/// </summary>
	public class ParallelCamera : CameraController
	{
		[Header("Mouse Control")]
		[MMInformation("If true, allow mouse to control camera position and movement with a left button drag.\n", MMInformationAttribute.InformationType.Info, false)]
		/// Whether or not mouse mode is active
		public bool MouseController;
		/// the camera's movement speed
		public float DragSpeed = 2;
		/// the height of the camera relative to its target
		public float Height = 35f;
		/// x distance between camera and target
		public float XOffset = 0f;
		/// z distance between camera and target
		public float ZOffset = -35f;

		protected Vector3 dragOrigin;
		protected Transform _target;

		/// <summary>
		/// this type of camera can only follow one target
		/// </summary>
		public override bool IsSinglePlayerCamera
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Updates camera values in mouse controller mode
		/// </summary>
		public virtual void UpdateMouseController()
		{
			if (MouseController)
			{
				if (Input.GetMouseButtonDown(0))
				{
					dragOrigin = Input.mousePosition;
					return;
				}

				if (Input.GetMouseButton(0)) 
				{
					Vector3 targetPos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
					ZOffset += targetPos.y * DragSpeed;
					XOffset += targetPos.x * DragSpeed;
				}
			}
		}

		/// <summary>
		/// Refresh the target 
		/// </summary>
		public override void RefreshTargets()
		{
			_target = null;
		}

		/// <summary>
		/// Updates camera position
		/// </summary>
		protected override void CameraUpdate()
		{
			UpdateMouseController();

			// we identify which target we want to follow
			if (_target == null)
			{
				if (HumanPlayers.Length > 0)
				{
					_target = HumanPlayers[0];
				}
				else if (BotPlayers.Length > 0)
				{
					_target = BotPlayers[0];
				}
			}

			if (_target == null)
			{
				return;
			}

			Vector3 newPosition = _target.position;
			newPosition.x += XOffset;
			newPosition.z += ZOffset;
			newPosition.y = Height;
			transform.position = newPosition;
			transform.LookAt(_target);
		}
	}
}