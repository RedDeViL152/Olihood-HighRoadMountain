using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Respawn vehicles when they hit the collider this class is attached to.
    /// Works only with SolidController.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DeathGround : MonoBehaviour
    {
        // When vehicle crashes against the death ground, we instantiate these particles
        public ParticleSystem Explosion;

        [Range(0.0f, 1f)]
        /// transparency of the gizmo drawing death ground
        public float GizmoTransparency = 0.5f;

        /// <summary>
		/// When collision is triggered by a vehicle, a respawn occurs
		/// </summary>
		/// <param name="other">target object</param>
		public virtual void OnTriggerEnter(Collider other)
        {
            SolidController solidController = other.GetComponent<SolidController>();

            if (solidController != null)
            {
                // Generates an explosion
                if (Explosion != null)
                {
                    Instantiate(Explosion.gameObject, other.transform.position, Quaternion.identity);
                }
                solidController.Respawn();
            }
        }

        /// <summary>
        /// Draw death ground in scene view
        /// </summary>
        public virtual void OnDrawGizmos()
        {
            var boxCollider = GetComponent<BoxCollider>();
            Gizmos.color = new Color(255, 0, 0, GizmoTransparency);
            Gizmos.DrawCube(transform.position + boxCollider.center, boxCollider.size);
        }
    }
}
