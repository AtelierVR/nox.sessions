using Nox.Entities;
using Nox.Players;

namespace Nox.Sessions {
	/// <summary>
	/// A session module notified of the ownership activity of an entity, so it can react to it (a
	/// script keeping a local mirror, a UI, a game rule, ...).
	/// <para>
	/// A request is observed here <b>before</b> the answer is sent, next to
	/// <see cref="IOwnershipDecision"/>: use that interface to take part in the decision, this one to
	/// only observe.
	/// </para>
	/// </summary>
	public interface IOwnershipListener {
		/// <summary>Invoked when a player asks for the ownership of an entity.</summary>
		/// <param name="entity">Entity whose ownership is requested.</param>
		/// <param name="requester">Player asking for the ownership.</param>
		/// <param name="receiver">Player that would get it (the requester itself).</param>
		void OnOwnershipRequested(IEntity entity, IPlayer requester, IPlayer receiver) { }

		/// <summary>Invoked once the ownership of an entity changed.</summary>
		/// <param name="entity">Entity whose ownership changed.</param>
		/// <param name="new">New owner, <c>null</c> when the relay did not name a known player.</param>
		/// <param name="old">Previous owner, <c>null</c> when it is unknown or left the session.</param>
		void OnOwnershipChanged(IEntity entity, IPlayer @new, IPlayer old) { }
	}
}
