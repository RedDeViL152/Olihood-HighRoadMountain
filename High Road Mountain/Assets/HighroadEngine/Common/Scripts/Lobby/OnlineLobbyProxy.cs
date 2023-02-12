using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.HighroadEngine
{
    /// <summary>
    /// Online lobby proxy. Various Networking solutions will register themselves to this instance 
    /// because they all must implement IGenericNetworkedLobbyManager interface
    /// </summary>
    public class OnlineLobbyProxy
    {
        public static IGenericNetworkLobbyManager Instance;
    }
}
