using Nox.Entities;
using Nox.Players;
using UnityEngine.Events;

namespace Nox.Sessions {
	/// <summary>
	/// An entity whose ownership is arbitrated by the session: it belongs to a player (its creator,
	/// then whoever the session granted it to).
	/// </summary>
	public interface IOwnedEntity : IEntity {
		/// <summary>
		/// Owner id that means « the session master »: an entity nobody claimed belongs to whoever holds
		/// the authority, so a master change or disconnection hands it over without a transfer.
		/// <para>
		/// <c>uint.MaxValue</c> is used because the relay allocates player ids starting at 0 (a player id
		/// is always a valid owner): only an id it can never assign can mean « no explicit owner ».
		/// </para>
		/// </summary>
		public const uint MASTER_OWNER_ID = uint.MaxValue;

		/// <summary>The player owning this entity, or <c>null</c> when it has none (or when that player
		/// left the session).</summary>
		IPlayer Owner { get; }

		/// <summary>The local player owns this entity: it drives its state.</summary>
		bool IsOwner { get; }

		/// <summary>Invoked when the ownership of this entity changes.</summary>
		/// <remarks>Arguments: the new owner, the previous owner (null when unknown or gone).</remarks>
		UnityEvent<IPlayer, IPlayer> OnOwnershipChanged { get; }

		/// <summary>
		/// Apply an ownership decided by the session. Called before <see cref="OnOwnershipChanged"/>: an
		/// entity that keeps its owner locally stores the new one here, and the session raises the event.
		/// </summary>
		/// <param name="receiver">Player the entity is given to (<c>null</c> when it is unknown locally).</param>
		/// <param name="requester">
		/// Player that asked for it (<c>null</c> when the relay changed it without a request, or when that
		/// player is unknown locally).
		/// </param>
		void ApplyOwner(IPlayer receiver, IPlayer requester) { }

		/// <summary>
		/// Decide whether this entity accepts an ownership request, on top of the session modules
		/// (<see cref="IOwnershipDecision"/>). A single refusal is enough to refuse the request.
		/// </summary>
		/// <param name="requester">Player asking for the ownership.</param>
		/// <param name="receiver">Player that would get it (the requester itself).</param>
		/// <param name="accepted">The decision: <c>false</c> refuses the request.</param>
		/// <returns>
		/// Whether the entity has an opinion. <c>false</c> (the default) leaves the decision to the
		/// session modules and to the scripts.
		/// </returns>
		bool TryDecideOwnership(IPlayer requester, IPlayer receiver, out bool accepted) {
			accepted = true;
			return false;
		}
	}
}
