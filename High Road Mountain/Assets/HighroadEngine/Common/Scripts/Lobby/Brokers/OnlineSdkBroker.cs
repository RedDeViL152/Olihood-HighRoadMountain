using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Online sdk broker.
    /// Manages the various online sdk that Highroad Engine supports, to avoid duplicating scenes.
    /// Will destroy itself once setup is done.
    /// </summary>
    public class OnlineSdkBroker : MonoBehaviour
    {
        public enum OnlineSdks { Unity, Pun };

        public static OnlineSdks SelectedOnlineSdk = OnlineSdks.Unity;

        /// <summary>
        /// The unity network handling gameobject. Will be destroyed from the scene if not needed.
        /// </summary>
        public GameObject UnityNetwork;

        /// <summary>
        /// The photon unity network handling gameobject. Will be destroyed from the scene if not needed.
        /// </summary>
        public GameObject PhotonUnityNetwork;

        /// Intialize proper network sdk
        public virtual void Awake()
        {
            if (OnlineSdkBroker.SelectedOnlineSdk == OnlineSdkBroker.OnlineSdks.Pun)
            {
                Destroy(UnityNetwork);

            }
            else if (OnlineSdkBroker.SelectedOnlineSdk == OnlineSdkBroker.OnlineSdks.Unity)
            {
                Destroy(PhotonUnityNetwork);
            }

            Destroy(this.gameObject);
        }
    }
}
