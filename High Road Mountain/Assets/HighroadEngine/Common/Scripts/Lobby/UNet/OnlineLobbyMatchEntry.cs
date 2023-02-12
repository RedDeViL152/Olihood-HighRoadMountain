using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;

namespace MoreMountains.HighroadEngine 
{
	/// <summary>
	/// Online lobby match entry UI element. Used to join a specific match in matchmaking
	/// </summary>
	public class OnlineLobbyMatchEntry : MonoBehaviour 
	{
		// The match join button.
		public Button MatchJoinButton;

		protected string _matchName;

		// The matchInfo for that entry
		IGenericMatchInfo _matchInfo;

		/// <summary>
		/// Initializes the button with match description and button onclick
		/// </summary>
		/// <param name="matchInfo">Match description used to populate button value.</param>
		/// <param name="manager">Manager referenced on the onclick event</param>
		public virtual void Init(IGenericMatchInfo matchInfo, IGenericNetworkLobbyManager manager)
		{
			_matchInfo = matchInfo;

			// Match name is combined with the current number of players & max size
			string info = _matchInfo.Name + "  (" + matchInfo.CurrentSize + "/" + matchInfo.MaxSize + ")";
			MatchJoinButton.GetComponentInChildren<Text>().text = info;

			MatchJoinButton.onClick.RemoveAllListeners();
			MatchJoinButton.onClick.AddListener(() => OnClick());
		}

		/// <summary>
		/// Describes what happens when the button is clicked
		/// </summary>
		public virtual void OnClick()
		{
			OnlineLobbyProxy.Instance.OnlineLobbyUIManager.TitleLabel.text = "GAME " + _matchInfo.Name;

			OnlineLobbyProxy.Instance.JoinMatch(_matchInfo);
		}
	}
}
