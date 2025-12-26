using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine.Events;

namespace Nox.Sessions.Runtime {
	public class Main : IMainModInitializer, ISessionAPI {
		internal static IMainModCoreAPI CoreAPI { get; private set; }

		internal static Main Instance { get; private set; }

		internal ISession GetCurrentSession() {
			if (Current == null)
				return null;
			TryGet(Current, out var session);
			return session;
		}

		public string Current { get; private set; }

		private readonly HashSet<ISession> _sessions = new();


		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
		}

		private async UniTask CloseAll() {
			await SetCurrent(null);

			foreach (var session in _sessions.ToArray()) {
				await session.Dispose();
				Remove(session);
			}

			_sessions.Clear();
		}

		public async UniTask OnDisposeMainAsync() {
			await CloseAll();

			foreach (var register in _registers.ToArray())
				Unregister(register);
			_registers.Clear();

			Instance = null;
			CoreAPI  = null;
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Specific to Unity Editor:
		/// called when exiting play mode,
		/// to dispose of sessions properly.
		/// </summary>
		public void OnExitPlayMode()
			=> CloseAll().Forget();
		#endif

		public void Add(ISession session) {
			if (Has(session.Id)) return;
			_sessions.Add(session);
			OnSessionAdded.Invoke(session);
			CoreAPI.EventAPI.Emit("session_added", session);
		}

		public void Remove(ISession session) {
			if (!Has(session.Id)) return;

			// If the session being unregistered is the current one,
			// switch to another session if available, or set to null.
			if (Current == session.Id) {
				if (_sessions.Count > 1) {
					var otherSession = _sessions.First(s => s.Id != session.Id);
					SetCurrent(otherSession.Id).Forget();
				} else SetCurrent(null).Forget();
			}

			_sessions.Remove(session);
			OnSessionRemoved.Invoke(session);
			CoreAPI.EventAPI.Emit("session_removed", session);
		}

		public IEnumerable<ISession> GetSessions()
			=> _sessions.ToArray();

		public bool TryGet(string id, out ISession session) {
			session = _sessions.FirstOrDefault(s => s.Id == id);
			return session != null;
		}

		public bool Has(string id)
			=> _sessions.Any(s => s.Id == id);

		public async UniTask SetCurrent(string id) {
			if (Current == id)
				return;

			if (!TryGet(id, out var nSession))
				return;

			ISession oSession = null;
			if (Current != null)
				TryGet(Current, out oSession);

			Current = id;

			if (oSession != null)
				await oSession.OnDeselect(nSession);
			if (nSession != null)
				await nSession.OnSelect(oSession);

			OnCurrentChanged.Invoke(oSession, nSession);
			CoreAPI.EventAPI.Emit("session_current_changed", oSession, nSession);
		}

		public UnityEvent<ISession> OnSessionAdded { get; } = new();

		public UnityEvent<ISession> OnSessionRemoved { get; } = new();

		public UnityEvent<ISession, ISession> OnCurrentChanged { get; } = new();

		public UnityEvent<ISessionRegister> OnSessionRegisterAdded { get; } = new();

		public UnityEvent<ISessionRegister> OnSessionRegisterRemoved { get; } = new();

		private readonly HashSet<ISessionRegister> _registers = new();

		public void Register(ISessionRegister register) {
			if (!_registers.Add(register)) return;
			OnSessionRegisterAdded.Invoke(register);
			CoreAPI.EventAPI.Emit("session_register_added", register);
		}

		public void Unregister(ISessionRegister register) {
			if (!_registers.Remove(register)) return;
			OnSessionRegisterRemoved.Invoke(register);
			CoreAPI.EventAPI.Emit("session_register_removed", register);
		}

		public bool TryMake(string name, Dictionary<string, object> options, out ISession session) {
			session = null;

			foreach (var register in _registers) {
				if (!register.TryMakeSession(name, options, out session)) continue;
				CoreAPI.EventAPI.Emit("session_created", session);
				Add(session);
				return true;
			}

			return false;
		}
	}
}