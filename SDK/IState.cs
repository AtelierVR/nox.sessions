namespace Nox.Sessions {
	public interface IState {
		/// <summary>
		/// Message describing the current state.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Progress of the current state (0.0 to 1.0) and -1 if not applicable.
		/// </summary>
		public float Progress { get; }

		/// <summary>
		/// Current status of the state.
		/// </summary>
		public Status Status { get; }
	}

	public enum Status {
		Pending = 0,
		Ready   = 1,
		Error   = 2,

		Finished = Ready | Error
	}
}