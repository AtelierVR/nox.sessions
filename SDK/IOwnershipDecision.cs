using Nox.Entities;
using Nox.Players;

namespace Nox.Sessions {
	/// <summary>
	/// A session module taking part in the decision of an ownership request: only the master is asked,
	/// and a single refusal is enough to refuse the request (no answer means the request is accepted).
	/// </summary>
	public interface IOwnershipDecision {
		/// <summary>Decide whether a forwarded ownership request may be granted.</summary>
		/// <param name="entity">Entity whose ownership is requested.</param>
		/// <param name="requester">Player asking for the ownership.</param>
		/// <param name="receiver">Player that would get it (the requester itself).</param>
		/// <param name="accepted">The decision: <c>false</c> refuses the request.</param>
		/// <returns>
		/// Whether a decision was made. <c>false</c> leaves the decision to the other modules (and to
		/// the scripts), so a module only interested in some entities answers nothing.
		/// </returns>
		bool TryDecideOwnership(IEntity entity, IPlayer requester, IPlayer receiver, out bool accepted);
	}
}
