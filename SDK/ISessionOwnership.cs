using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Entities;
using Nox.Players;
using UnityEngine.Events;

namespace Nox.Sessions {
	/// <summary>
	/// Ownership of the session entities: the events raised when an ownership is requested / changed,
	/// and the operations to ask for it or to refuse a forwarded request.
	/// <para>
	/// An entity belongs to one player (its creator, then whoever the ownership was granted to). A
	/// non privileged player asks for it, the relay forwards the request to the privileged players
	/// (the master arbitrates), and the answer arrives as <see cref="OnOwnershipChanged"/> (granted)
	/// or as a refusal.
	/// </para>
	/// </summary>
	public interface ISessionOwnership {
		/// <summary>Invoked when the ownership of an entity changed.</summary>
		/// <remarks>Arguments: the entity, the new owner, the previous owner (null when unknown).</remarks>
		UnityEvent<IEntity, IPlayer, IPlayer> OnOwnershipChanged { get; }

		/// <summary>Invoked when a player asks for the ownership of an entity.</summary>
		/// <remarks>Arguments: the entity, the requester, the request id (to refuse it).</remarks>
		UnityEvent<IEntity, IPlayer, ushort> OnOwnershipRequested { get; }

		/// <summary>
		/// Change the ownership of an entity. Only a privileged player may name another
		/// <paramref name="playerId"/>; <c>0</c> is the sender itself.
		/// </summary>
		/// <param name="entityId">Entity whose ownership is wanted.</param>
		/// <param name="playerId">Player that would get it (<c>0</c> = the sender itself).</param>
		/// <param name="wait">
		/// Wait for the answer: <c>false</c> sends the request and returns
		/// <see cref="OwnershipChangeResult.Pending"/>; <c>true</c> completes once the ownership was
		/// granted or refused (of the relay, or of the master which decides a forwarded request).
		/// </param>
		/// <param name="token">Cancels the wait (also cancelled when the server disconnects).</param>
		UniTask<OwnershipChangeResult> ChangeOwnership(
			uint entityId,
			uint playerId = 0,
			bool wait = false,
			CancellationToken token = default
		);

		/// <summary>Refuse a forwarded ownership request; the requester then receives an error.</summary>
		UniTask<bool> RefuseOwnership(ushort request);
	}
}
