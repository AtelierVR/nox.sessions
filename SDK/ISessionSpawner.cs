using Cysharp.Threading.Tasks;
using Nox.Entities;

namespace Nox.Sessions {
	/// <summary>
	/// A session whose entities are replicated: it knows how to spawn and remove them. The mod that
	/// owns the entity channel implements it; a consumer only holding the <see cref="ISession"/> asks
	/// for it with a cast.
	/// </summary>
	public interface ISessionSpawner {
		/// <summary>
		/// Create a replicated entity of the given type. The mod owning the entity channel assigns the
		/// id; completes with the entity, or <c>null</c> when the type is not handled or the creation
		/// was refused.
		/// </summary>
		UniTask<IEntity> CreateEntity(uint type, byte flags = 0);

		/// <summary>Remove a replicated entity of the session.</summary>
		UniTask<bool> RemoveEntity(IEntity entity);

		/// <summary>
		/// Ask the server for the full entity list of the room: a client that joins after the master
		/// announced its entities only learns them by asking (nothing is broadcast to a client that was
		/// not there yet).
		/// </summary>
		UniTask<bool> SyncEntities();
	}
}
