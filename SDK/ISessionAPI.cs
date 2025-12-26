using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace Nox.Sessions {
	public interface ISessionAPI {
		/// <summary>
		/// Register a session register.
		/// </summary>
		/// <param name="register"></param>
		public void Register(ISessionRegister register);

		/// <summary>
		/// Unregister a session register.
		/// </summary>
		/// <param name="register"></param>
		public void Unregister(ISessionRegister register);

		/// <summary>
		/// Get all sessions currently managed.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ISession> GetSessions();

		/// <summary>
		/// Try to get a session by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		public bool TryGet(string id, out ISession session);
		
		/// <summary>
		/// Try to make a session using a registered session register.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="options"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		public bool TryMake(string name, Dictionary<string, object> options, out ISession session);

		/// <summary>
		/// Check if a session with the given ID exists.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool Has(string id);

		/// <summary>
		/// Get the current session ID.
		/// </summary>
		/// <returns></returns>
		public string Current { get; }

		/// <summary>
		/// Set the current session by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public UniTask SetCurrent(string id);

		/// <summary>
		/// Invoked when a session is registered.
		/// </summary>
		UnityEvent<ISession> OnSessionAdded { get; }

		/// <summary>
		/// Invoked when a session is unregistered.
		/// </summary>
		UnityEvent<ISession> OnSessionRemoved { get; }

		/// <summary>
		/// Invoked when the current session changes.
		/// </summary>
		UnityEvent<ISession, ISession> OnCurrentChanged { get; }

	}
}