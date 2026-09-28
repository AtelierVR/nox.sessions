using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Sessions;
using Nox.Scripting;
using Nox.Session.Runtime.Commands;
using Nox.Sessions.Runtime.Converters;
using Nox.Sessions.Runtime.Modules;
using Nox.Sessions.Runtime.Settings;
using Nox.Settings;
using UnityEngine;
using UnityEngine.Events;
using System.Threading;


#if UNITY_EDITOR
using Nox.Editor.Panel;
#endif

namespace Nox.Sessions.Runtime {
	public class Main : IMainModInitializer, ISessionAPI {
		public static IMainModCoreAPI CoreAPI { get; private set; }

		public static Main Instance { get; private set; }

		private Socket _socket;

		static internal ISettingAPI SettingAPI
			=> CoreAPI?.ModAPI
				?.GetMod("settings")
				?.GetInstance<ISettingAPI>();

		#if UNITY_EDITOR
		static internal IPanelAPI PanelAPI
			=> CoreAPI?.ModAPI
				?.GetMod("editor.panel")
				?.GetInstance<IPanelAPI>();
		#endif

		internal ISession GetCurrentSession() {
			if (Current == null)
				return null;
			TryGet(Current, out var session);
			return session;
		}

		public string Current { get; private set; }
		private LanguagePack _lang;
		private readonly HashSet<ISession> _sessions = new();
		private IHandler[] _handlers = Array.Empty<IHandler>();
		private Commands commands;


		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_lang    = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			_handlers = new IHandler[] {
				new RenderEntity(),
				new ClearPhysical()
			};
			;
			foreach (var handler in _handlers)
				SettingAPI.Add(handler);
			commands = new Commands();
			_socket = new Socket();
			_socket.Initialize();

			var scripting = api.ModAPI.GetMod("scripting")?.GetInstance<IScriptingAPI>();
			if (scripting != null) {
				scripting.RegisterConverter(PlayerConverter.Player);
				scripting.RegisterConverter(TeamConverter.Team);
				scripting.RegisterModule(PlayersModule.Module);
				scripting.RegisterModule(TeamsModule.Module);
				scripting.RegisterModule(NetworkModule.Module);
				scripting.RegisterModule(GizmoModule.Module);
				scripting.RegisterModule(PhysicsModule.Module);
			}
		}

		public void OnUpdateMain()
			=> GetCurrentSession()
				?.Update();

		private async UniTask CloseAll() {
			await SetCurrent(null);

			foreach (var session in _sessions.ToArray()) {
				await session.Dispose();
				Remove(session);
			}

			_sessions.Clear();
			LanguageManager.RemovePack(_lang);
		}

		public async UniTask OnDisposeMainAsync() {
			commands?.Dispose();
			commands = null;

			_socket?.Dispose();
			_socket = null;

			await CloseAll();

			foreach (var register in _registers.ToArray())
				Unregister(register);
			_registers.Clear();

			foreach (var handler in _handlers.ToArray())
				SettingAPI.Remove(handler.GetPath());
			_handlers = Array.Empty<IHandler>();

			var scripting = CoreAPI?.ModAPI.GetMod("scripting")?.GetInstance<IScriptingAPI>();
			scripting?.UnregisterConverter(PlayerConverter.Player);
			scripting?.UnregisterConverter(TeamConverter.Team);
			scripting?.UnregisterModule(PlayersModule.Module);
			scripting?.UnregisterModule(TeamsModule.Module);
			scripting?.UnregisterModule(NetworkModule.Module);
			scripting?.UnregisterModule(GizmoModule.Module);
			scripting?.UnregisterModule(PhysicsModule.Module);

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
			if (Has(session.Id))
				return;
			_sessions.Add(session);
			OnSessionAdded.Invoke(session);
			CoreAPI.EventAPI.Emit("session_added", session);
		}

		public void Remove(ISession session) {
			if (!Has(session.Id))
				return;

			// If the session being unregistered is the current one,
			// switch to another session if available, or set to null.
			if (Current == session.Id) {
				if (_sessions.Count > 1) {
					var otherSession = _sessions.First(s => s.Id != session.Id);
					SetCurrent(otherSession.Id).Forget();
				} else
					SetCurrent(null).Forget();
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

		public async UniTask SetCurrent(string id, CancellationToken token = default) {
			token.ThrowIfCancellationRequested();

			if (Current == id)
				return;

			ISession nSession = null;
			if (id != null && !TryGet(id, out nSession))
				return;

			ISession oSession = null;
			if (Current != null)
				TryGet(Current, out oSession);

			// Pause the simulation while switching sessions.
			Physics.simulationMode = SimulationMode.Script;

			try {
				Current = id;

				if (oSession != null)
					await oSession.OnDeselect(nSession);

				if (nSession != null)
					await nSession.OnSelect(oSession);

				// The new current session drives the physics (simulation + gravity).
				if (nSession is IPhysicalSession physical) {
					Physics.simulationMode = physical.Simulation
						? SimulationMode.Update
						: SimulationMode.Script;
					Physics.gravity = physical.Gravity;
				} else
					Physics.simulationMode = SimulationMode.Update;

				OnCurrentChanged.Invoke(oSession, nSession);
				CoreAPI.EventAPI.Emit("session_current_changed", oSession, nSession);

				if (oSession?.IsDisposeOnChange() ?? false) {
					await oSession.Dispose();
					Remove(oSession);
				}
			} finally {
				// Never leave the simulation frozen when the current session does not
				// drive the physics environment.
				if (GetCurrentSession() is not IPhysicalSession)
					Physics.simulationMode = SimulationMode.Update;
			}
		}

		/// <summary>
		/// Disposes the session with the given ID and unregisters it.
		/// Follows the same current-session switching logic as <see cref="SetCurrent"/>,
		/// except the session is always disposed: <c>dispose-on-change</c> is ignored.
		/// </summary>
		public async UniTask Close(string id, CancellationToken token = default) {
			token.ThrowIfCancellationRequested();

			if (!TryGet(id, out var session))
				return;

			// Do not dispose the session up front: let SetCurrent handle the current
			// session (deselect, then dispose + unregister through dispose-on-change).
			if (Current == id) {
				await SetCurrent(null, token);
				if (!Has(id))
					return;
			}

			// Non-current session (e.g. connection in progress), or dispose-on-change
			// disabled: dispose it explicitly.
			await session.Dispose();
			Remove(session);
		}

		public UnityEvent<ISession> OnSessionAdded { get; } = new();

		public UnityEvent<ISession> OnSessionRemoved { get; } = new();

		public UnityEvent<ISession, ISession> OnCurrentChanged { get; } = new();

		public UnityEvent<ISessionRegister> OnSessionRegisterAdded { get; } = new();

		public UnityEvent<ISessionRegister> OnSessionRegisterRemoved { get; } = new();

		private readonly HashSet<ISessionRegister> _registers = new();

		public void Register(ISessionRegister register) {
			if (!_registers.Add(register))
				return;
			OnSessionRegisterAdded.Invoke(register);
			CoreAPI.EventAPI.Emit("session_register_added", register);
		}

		public void Unregister(ISessionRegister register) {
			if (!_registers.Remove(register))
				return;
			OnSessionRegisterRemoved.Invoke(register);
			CoreAPI.EventAPI.Emit("session_register_removed", register);
		}

		public bool TryMake(string name, Dictionary<string, object> options, out ISession session) {
			session = null;

			foreach (var register in _registers) {
				if (!register.TryMakeSession(name, options, out session))
					continue;
				CoreAPI.EventAPI.Emit("session_created", session);
				Add(session);
				return true;
			}

			return false;
		}
	}
}