using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Modifies the scene's gravity
    /// This allows for fine tuning of the vehicle's physics
    /// By default the value is earth gravity * 3
    /// </summary>
    public class GravityManager : MonoBehaviour
    {
        /// Scene gravity
        public Vector3 SceneGravity = new Vector3(0.0f, -29.4f, 0.0f);

        /// <summary>
        /// At awake we update the gravity with our value
        /// </summary>
        public virtual void Awake()
        {
            Physics.gravity = SceneGravity;
        }
    }
}
