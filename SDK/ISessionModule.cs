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
		/// <param name="name"></param>
		/// <param name="payload"></param>
		/// <param name="sender"></param>
		public void OnEvent(long name, byte[] payload, IPlayer sender) { }

		/// <summary>
		/// Invoked on each server tick.
		/// <para><paramref name="tick"/> is the monotonically-increasing tick counter.</para>
		/// </summary>
		public void OnTick(long tick) { }

		/// <summary>
		/// Invoked when the session tick rate changes.
		/// <para><paramref name="tickRate"/> is the new tick rate in ticks per second.</para>
		/// </summary>
		public void OnTickRateChanged(int tickRate) { }

		/// <summary>
		/// Invoked when a networked session starts trying to restore a lost connection.
		/// <para><paramref name="attempt"/> is the attempt number, starting at 1.</para>
		/// </summary>
		/// <param name="attempt"></param>
		public void OnReconnecting(int attempt) { }

		/// <summary>
		/// Invoked once a networked session is connected again and back in its room after a
		/// disconnection (the world itself was never unloaded).
		/// </summary>
		public void OnReconnected() { }

		/// <summary>
		/// Invoked when the session is connected to the network.
		/// A module loaded after the connection is established also receives it from
		/// <see cref="OnLoaded"/> (the session replays the current state).
		/// </summary>
		public void OnConnected() { }

		/// <summary>
		/// Invoked when the session loses its network connection.
		/// </summary>
		/// <param name="reason">Why the connection was closed.</param>
		public void OnDisconnected(string reason) { }
	}
}