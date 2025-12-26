using System;
using Cysharp.Threading.Tasks;
using Nox.Players;
using UnityEngine.Events;

namespace Nox.Sessions {
	public interface INetSession : ISession {
		/// <summary>
		/// Indicates whether the session is currently connected to the network.
		/// </summary>
		public bool IsConnected { get; }

		/// <summary>
		/// The utc time of the server.
		/// </summary>
		public DateTime Time { get; }

		/// <summary>
		/// The ping to the server in milliseconds.
		/// </summary>
		public int Ping { get; }

		/// <summary>
		/// Invoked when the session successfully connects to the network.
		/// </summary>
		public UnityEvent OnConnected { get; }

		/// <summary>
		/// Invoked when the session is disconnected from the network.
		/// </summary>
		public UnityEvent<string> OnDisconnected { get; }
		
		/// <summary>
		/// Emits a custom event to the session.
		/// Is emitting from local player.
		/// </summary>
		/// <param name="event"></param>
		/// <param name="raw"></param>
		/// <returns></returns>
		public UniTask<bool> EmitEvent(long @event, byte[] raw);

		/// <summary>
		/// Invoked when a custom event is triggered within the session.
		/// The int is the event code.
		/// The byte[] is the raw event data.
		/// The IPlayer is the sender of the event.
		/// </summary>
		/// <returns></returns>
		public UnityEvent<long, byte[], IPlayer> OnEventReceived { get; }
	}
}