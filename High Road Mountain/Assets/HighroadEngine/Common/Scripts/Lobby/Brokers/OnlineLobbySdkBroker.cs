using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Online lobby sdk broker.
    /// Manages the various online sdk that Highroad Engine supports, to avoid duplicating scenes.
    /// Will destroy itself once setup is done.
    /// </summary>
    public class OnlineLobbySdkBroker : MonoBehaviour
    {
        /// <summary>
        /// The unity network handling gameobject. Will be destroyed from the scene if not needed.
        /// </summary>
        public GameObject UnityNetwork;

        /// <summary>
        /// The photon unity network prefab. Will be instantiated and setup properly only if needed
        /// </summary>
        public string PhotonUnityNetworkPrefabResource;

        public GameObject OnlineUI;

        /// Intialize proper network sdk
        public virtual void Awake()
        {
            if (OnlineSdkBroker.SelectedOnlineSdk == OnlineSdkBroker.OnlineSdks.Pun)
            {
                Destroy(UnityNetwork);
                GameObject _prefab = Resources.Load<GameObject>(PhotonUnityNetworkPrefabResource) as GameObject;
                GameObject instance = Instantiate(_prefab, Vector3.zero, Quaternion.identity);

                OnlineUI.transform.SetParent(instance.transform);
            }
            else if (OnlineSdkBroker.SelectedOnlineSdk == OnlineSdkBroker.OnlineSdks.Unity)
            {
                OnlineUI.transform.SetParent(UnityNetwork.transform);
            }

            Destroy(this.gameObject);
        }
    }
}
