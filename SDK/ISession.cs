using Cysharp.Threading.Tasks;
using Nox.CCK.Properties;
using Nox.Entities;
using Nox.Players;
using UnityEngine.Events;

namespace Nox.Sessions {
	public interface ISession : ISession<IDimensions, IEntities> { }

	public interface ISession<out TDimension, out TEntities> : IPropertyObject
		where TDimension : IDimensions
		where TEntities : IEntities {
		/// <summary>
		/// Unique identifier for the session.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Dimension associated with the session.
		/// </summary>
		public TDimension Dimensions { get; }

		/// <summary>
		/// Manager for entities within the session.
		/// </summary>
		public TEntities Entities { get; }

		/// <summary>
		/// The master player of the session.
		/// </summary>
		public IPlayer MasterPlayer { get; set; }

		/// <summary>
		/// The local player of the session.
		/// </summary>
		public IPlayer LocalPlayer { get; set; }

		/// <summary>
		/// Current state of the session.
		/// </summary>
		public IState State { get; }

		public UniTask Dispose();

		public UniTask OnSelect(ISession old);

		public UniTask OnDeselect(ISession @new);

		/// <summary>
		/// Invoked when a player joins the session.
		/// Is called after entity registration.
		/// </summary>
		public UnityEvent<IPlayer> OnPlayerJoined { get; }

		/// <summary>
		/// Invoked when a player leaves the session.
		/// Is called before entity unregistration.
		/// </summary>
		public UnityEvent<IPlayer> OnPlayerLeft { get; }
		
		/// <summary>
		/// Invoked when authority over a player is transferred.
		/// The first IPlayer is the new authority holder.
		/// The second IPlayer is the previous authority holder.
		/// </summary>
		public UnityEvent<IPlayer, IPlayer> OnAuthorityTransferred { get; }

		/// <summary>
		/// Invoked when a player's visibility changes within the session.
		/// When the player has now a physical representation in the session, the bool is true.
		/// When the player is now invisible or not represented in the session, the bool is false.
		/// </summary>
		public UnityEvent<IPlayer, bool> OnPlayerVisibility { get; }

		/// <summary>
		/// Invoked when an entity is registered to the session.
		/// </summary>
		public UnityEvent<IEntity> OnEntityRegistered { get; }

		/// <summary>
		/// Invoked when an entity is unregistered from the session.
		/// </summary>
		public UnityEvent<IEntity> OnEntityUnregistered { get; }

		/// <summary>
		/// Invoked when the session's state changes.
		/// </summary>
		UnityEvent<IState> OnStateChanged { get; }
	}
}