namespace Nox.Sessions {
	/// <summary>Outcome of an ownership change asked to the session.</summary>
	public enum OwnershipChangeResult {
		/// <summary>The entity is now owned by the target player.</summary>
		Granted,

		/// <summary>The request was sent and is waiting for the decision of a privileged player.</summary>
		Pending,

		/// <summary>A privileged player refused the request.</summary>
		Refused,

		/// <summary>The request could not be sent (not connected, entity unknown, ...).</summary>
		Failed,

		/// <summary>Nobody answered in time.</summary>
		Timeout,

		/// <summary>The wait was cancelled (including a disconnection of the server).</summary>
		Cancelled,
	}
}
