using Nox.Entities;
using Nox.Players;
using Nox.Worlds;
using UnityEngine;

namespace Nox.Sessions {
	public interface ISessionModule : IWorldModule {
		/// <summary>
		/// Invoked when the session is loaded.
		/// </summary>
		/// <param name="session"></param>
		public void OnLoaded(ISession session) { }

		/// <summary>
		/// Invoked when the session is unloaded.
		/// </summary>
		public void OnSessionSelected() { }

		/// <summary>
		/// Invoked when the session is unloaded.
		/// </summary>
		public void OnSessionDeselected() { }

		/// <summary>
		/// Invoked when a player joins the session.
		/// Is called after entity registration.
		/// </summary>
		/// <param name="player"></param>
		public void OnPlayerJoined(IPlayer player) { }

		/// <summary>
		/// Invoked when a player leaves the session.
		/// Is called before entity unregistration.
		/// </summary>
		/// <param name="player"></param>
		public void OnPlayerLeft(IPlayer player) { }

		/// <summary>
		/// Invoked when a player's authority is transferred to another player.
		/// if previous is null, the authority is newly assigned.
		/// </summary>
		/// <param name="current"></param>
		/// <param name="previous"></param>
		public void OnAuthorityTransferred(IPlayer current, IPlayer previous) { }
		
		/// <summary>
		/// Invoked when a player's visibility changes within the session.
		/// When the player has now a physical representation in the session, the bool is true.
		/// When the player is now invisible or not represented in the session, the bool is false.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="isVisible"></param>
		public void OnPlayerVisibilityChanged(IPlayer player, bool isVisible) { }

		/// <summary>
		/// Invoked when a scene is loaded in the session's world.
		/// </summary>
		/// <param name="descriptor"></param>
		/// <param name="index"></param>
		/// <param name="anchor"></param>
		public void OnSceneLoaded(IWorldDescriptor descriptor, int index, GameObject anchor) { }

		/// <summary>
		/// Invoked when a scene is unloaded in the session's world.
		/// </summary>
		/// <param name="index"></param>
		public void OnSceneUnloaded(int index) { }
		
		/// <summary>
		/// Invoked when an entity is registered to the session.
		/// </summary>
		/// <param name="entity"></param>
		public void OnEntityRegistered(IEntity entity) { }

		/// <summary>
		/// Invoked when an entity is unregistered from the session.
		/// </summary>
		/// <param name="entity"></param>
		public void OnEntityUnregistered(IEntity entity) { }

		/// <summary>
		/// Invoked when a custom event is triggered within the session.
		/// </summary>
		/// <param name="event"></param>
		/// <param name="raw"></param>
		/// <param name="sender"></param>
		public void OnEventTriggered(string @event, byte[] raw, IPlayer sender) { }
	}
}