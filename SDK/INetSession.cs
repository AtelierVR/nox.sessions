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
		/// Indicates whether the session is currently trying to restore a lost connection.
		/// </summary>
		public bool IsReconnecting { get; }

		/// <summary>
		/// Number of the current reconnection attempt, or 0 when no reconnection is running.
		/// </summary>
		public int ReconnectAttempt { get; }

		/// <summary>
		/// The utc time of the server.
		/// If not connected, returns <see cref="DateTime.MinValue"/>.
		/// </summary>
		public DateTime Time { get; }

		/// <summary>
		/// The ping to the server in milliseconds.
		/// If not connected, returns -1.
		/// </summary>
		public double Ping { get; }

		/// <summary>
		/// Maximum allowed payload size in bytes for <see cref="EmitEvent"/>.
		/// </summary>
		public int EventPayloadSize { get; }

		/// <summary>
		/// Current tick rate (ticks per second) of the session, as reported by the server.
		/// </summary>
		public int TickRate { get; }

		/// <summary>
		/// Invoked when the server-side tick rate changes.
		/// Provides the new tick rate value.
		/// </summary>
		public UnityEvent<int> OnTickRateChanged { get; }

		/// <summary>
		/// Invoked when the session successfully connects to the network.
		/// </summary>
		public UnityEvent OnConnected { get; }

		/// <summary>
		/// Invoked when the session is disconnected from the network.
		/// </summary>
		public UnityEvent<string> OnDisconnected { get; }

		/// <summary>
		/// Invoked with the attempt number (starting at 1) each time the session tries to
		/// restore a lost connection.
		/// </summary>
		public UnityEvent<int> OnReconnecting { get; }

		/// <summary>
		/// Invoked once the session is connected again and back in its room after a disconnection.
		/// </summary>
		public UnityEvent OnReconnected { get; }
		
		/// <summary>
		/// Invoked when the ping value changes significantly.
		/// Provides the new ping value in milliseconds.
		/// </summary>
		public UnityEvent<double> OnPingChanged { get; }
		
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