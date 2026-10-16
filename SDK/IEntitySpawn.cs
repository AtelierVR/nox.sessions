using Nox.Entities;

namespace Nox.Sessions {
	/// <summary>
	/// Spawn request emitted by a session (event <see cref="EventName"/>) when a network backend
	/// announces a networked entity the session does not know yet (create / sync).
	/// <para>
	/// The session never builds an entity itself: it emits this request and the first subscriber to
	/// answer through <see cref="Callback"/> wins — every later answer is ignored, and when nobody
	/// answers the entity is not created.
	/// </para>
	/// <para>
	/// Subscribers receive the session, then this request, in the event data (see
	/// <c>EventData.TryGet</c>) and answer synchronously with <see cref="Callback"/>.
	/// </para>
	/// </summary>
	public interface IEntitySpawn {
		/// <summary>Name of the event carrying an <see cref="IEntitySpawn"/> (data: the session, then the request).</summary>
		const string EventName = "session.entity_spawn_request";

		/// <summary>Session that emitted the spawn request.</summary>
		ISession Session { get; }

		/// <summary>Entity id assigned by the relay.</summary>
		uint Id { get; }

		/// <summary>Entity kind on the relay entity channel (e.g. <c>"prop"</c>, <c>"mirror"</c>).</summary>
		string Kind { get; }

		/// <summary>Entity id of the player the entity is attached to (<c>0</c> = world).</summary>
		uint OwnerId { get; }

		/// <summary>Entity id of the sender that triggered the spawn.</summary>
		uint SenderId { get; }

		/// <summary>Kind-specific flags carried by the spawn.</summary>
		byte Flags { get; }

		/// <summary>Whether a subscriber already answered the spawn (every later answer is ignored).</summary>
		bool Answered { get; }

		/// <summary>Answer the spawn with the built entity (first answer wins).</summary>
		void Callback(IEntity entity);
	}
}
