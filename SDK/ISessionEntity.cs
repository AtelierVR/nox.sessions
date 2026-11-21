using Nox.Entities;
using UnityEngine.Events;

namespace Nox.Sessions {
	/// <summary>
	/// An entity of a session: everything a local object bound to a replicated entity needs, in one
	/// place (the parts it moves, the properties it shares, the events it sends and receives, its
	/// tick and the session itself).
	/// <para>
	/// A behaviour implementing it (the video player, a grabbable, ...) is the session entity of its
	/// replicated entity: the session ticks it, and the properties received from the other clients
	/// are raised through <see cref="OnProperty"/>. The ownership and the privilege come from
	/// <see cref="IOwnedEntity"/> and <see cref="ISession"/>.
	/// </para>
	/// </summary>
	public interface ISessionEntity : IOwnedEntity, IPropertiesEntity, IUpdateEntity {
		/// <summary>The session the entity lives in.</summary>
		ISession Session { get; }

		/// <summary>
		/// Whether the session replicates the entity: its id and its lifecycle belong to it (the relay
		/// creates and removes it), while a local-only entity is disposed by whoever owns it.
		/// </summary>
		bool IsNetworked
			=> false;

		/// <summary>
		/// Set up the local entity for its session (called when the entity is created). Before it,
		/// <see cref="Session"/> and the parts/properties are not available.
		/// </summary>
		void Setup(ISession session);

		#region Properties

		/// <summary>Declare a property of the entity (no-op when it already exists).</summary>
		bool DefineProperty(string name, object value = null, PropertyFlags flags = PropertyFlags.Synced);

		/// <summary>A property of the entity was received (or changed locally).</summary>
		UnityEvent<IProperty> OnProperty { get; }

		#endregion

		#region Parts

		/// <summary>Every part of the entity.</summary>
		IPart[] GetParts();

		#endregion

		#region Events

		/// <summary>Send a session event for this entity.</summary>
		void EmitEvent(long name, byte[] payload);

		/// <summary>A session event was received: its name, its payload and the sender entity id.</summary>
		UnityEvent<long, byte[], uint> OnEvent { get; }

		#endregion
	}
}
