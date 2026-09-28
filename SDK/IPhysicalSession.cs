using UnityEngine;

namespace Nox.Sessions {
	/// <summary>
	/// Session that owns the physics environment: whether the simulation runs and the gravity
	/// applied.
	/// <para>
	/// Values are stored in the session <see cref="ISession.Data"/> container; they are applied
	/// to the engine by <c>ISessionAPI.SetCurrent</c> when this session becomes the current one.
	/// </para>
	/// </summary>
	public interface IPhysicalSession : ISession {
		/// <summary>
		/// Whether the physics simulation runs while this session is current.
		/// When <c>false</c> the simulation is paused (<c>SimulationMode.Script</c>).
		/// </summary>
		bool Simulation { get; set; }

		/// <summary>
		/// Gravity vector (orientation and magnitude) applied while this session is current.
		/// Maps directly to <see cref="Physics.gravity"/>.
		/// </summary>
		Vector3 Gravity { get; set; }
	}
}
