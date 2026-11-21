using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using UnityEngine.Events;
using UnityEngine.UIElements;
using IPanel = Nox.Editor.Panel.IPanel;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Sessions.Runtime.Editor {
	public class SessionsPanel : IEditorModInitializer, IPanel {
		private static readonly string[] PanelPath = { "session", "sessions" };
		internal IEditorModCoreAPI API;

		public void OnInitializeEditor(IEditorModCoreAPI api)
			=> API = api;

		public void OnDisposeEditor()
			=> API = null;

		public string[] GetPath()
			=> PanelPath;

		internal SessionsInstance Instance;

		public IInstance[] GetInstances()
			=> Instance != null
				? new IInstance[] { Instance }
				: Array.Empty<IInstance>();

		public string GetLabel()
			=> "Session/Sessions";

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data) {
			if (Instance != null)
				throw new InvalidOperationException("SessionsPanel only supports a single instance.");
			return Instance = new SessionsInstance(this, window, data);
		}
	}

	public class SessionsInstance : IInstance {
		private readonly SessionsPanel _panel;
		private readonly IWindow _window;

		private VisualElement _content;
		private VisualElement _list;
		private VisualElement _empty;

		private readonly Dictionary<string, SessionRow> _rows = new();

		/// <summary>
		/// One line of the list: the session it shows, the labels it keeps in sync and the handlers it
		/// registered on that session (they have to be removed by reference when the list is rebuilt).
		/// </summary>
		private sealed class SessionRow {
			public ISession Session;
			public Label State;
			public Label Connection;
			public VisualElement NetInfo;
			public Label Ping;
			public Label TickRate;
			public Label Time;
			public Label Payload;

			public UnityAction<IState> OnState;
			public UnityAction OnConnected;
			public UnityAction OnReconnected;
			public UnityAction<string> OnDisconnected;
			public UnityAction<int> OnReconnecting;
			public UnityAction<int> OnTickRate;
			public UnityAction<double> OnPing;
		}

		/// <summary>Session API of this mod: the runtime entry point itself, no mod lookup needed.</summary>
		private static ISessionAPI SessionAPI
			=> Main.Instance;

		public SessionsInstance(SessionsPanel panel, IWindow window, Dictionary<string, object> data) {
			_panel = panel;
			_window = window;

			// Subscribe to session events for auto-update
			var sessionAPI = SessionAPI;
			if (sessionAPI != null) {
				sessionAPI.OnSessionAdded.AddListener(OnSessionAddedOrRemoved);
				sessionAPI.OnSessionRemoved.AddListener(OnSessionAddedOrRemoved);
			}
		}

		public IPanel GetPanel()
			=> _panel;

		public IWindow GetWindow()
			=> _window;

		public string GetTitle()
			=> "Sessions";

		public void OnDestroy() {
			ClearRows();

			// Unsubscribe from session list events
			var sessionAPI = SessionAPI;
			if (sessionAPI != null) {
				sessionAPI.OnSessionAdded.RemoveListener(OnSessionAddedOrRemoved);
				sessionAPI.OnSessionRemoved.RemoveListener(OnSessionAddedOrRemoved);
			}

			_panel.Instance = null;
		}

		private void OnSessionAddedOrRemoved(ISession session) {
			// Refresh the list when a session is added or removed
			RefreshSessions();
		}
		public VisualElement GetContent() {
			if (_content != null)
				return _content;

			var root = _panel.API.AssetAPI
				.GetAsset<VisualTreeAsset>("panels/sessions.uxml")
				.CloneTree();

			_list = root.Q<VisualElement>("list");
			_empty = root.Q<VisualElement>("empty");

			RefreshSessions();

			return _content = root;
		}

		/// <summary>Refreshes one row, touching a label only when its text actually changes.</summary>
		private static void ApplyRow(SessionRow row) {
			var session = row?.Session;
			if (session == null)
				return;

			Set(row.State, FormatState(session.State));

			if (session is not INetworkSession net) {
				Set(row.Connection, "N/A");
				SetVisible(row.NetInfo, false);
				return;
			}

			Set(row.Connection, net.IsConnected ? "Connected" : "Disconnected");
			SetVisible(row.NetInfo, true);

			// Networking details: Ping, TickRate, Server Time, Payload Size.
			// Raw values only, the unit already says what they are (the UXML lays the four
			// of them out in equal columns).
			Set(row.Ping, net.IsConnected ? $"{net.Ping:F0} ms" : "N/A");
			Set(row.TickRate, net.IsConnected ? $"{net.TickRate} t/s" : "N/A");
			Set(row.Time, net.IsConnected ? $"{net.Time:HH:mm:ss}" : "N/A");
			Set(row.Payload, net.IsConnected ? $"{net.EventPayloadSize} B" : "N/A");
		}

		/// <summary>
		/// Status, message and progress of the state, without a `Status:` prefix: the status is the
		/// first word of the line, and the message is what tells a session stuck in <c>Pending</c>
		/// apart (loading a world, downloading it, connecting...).
		/// </summary>
		private static string FormatState(IState state) {
			if (state == null)
				return "Unknown";

			var text = state.Status.ToString();

			if (!string.IsNullOrEmpty(state.Message))
				text += $" — {state.Message}";

			if (state.Status == Status.Pending && state.Progress >= 0f)
				text += $" ({state.Progress:P0})";

			return text;
		}

		private static void Set(Label label, string text) {
			if (label != null && label.text != text)
				label.text = text;
		}

		private void RefreshSessions() {
			if (_list == null)
				return;

			ClearRows();
			_list.Clear();

			var sessionAPI = SessionAPI;
			var sessions = sessionAPI?.GetSessions()?.ToList() ?? new List<ISession>();

			if (sessions.Count == 0) {
				SetVisible(_list, false);
				SetVisible(_empty, true);
				return;
			}

			SetVisible(_list, true);
			SetVisible(_empty, false);

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/session-item.uxml");

			foreach (var session in sessions) {
				var item = itemAsset.CloneTree();

				var title = item.Q<Label>("title");
				title.text = session.Id;
				title.tooltip = session.Id;

				var row = new SessionRow {
					Session = session,
					State = item.Q<Label>("state"),
					Connection = item.Q<Label>("connection"),
					NetInfo = item.Q<VisualElement>("net-info"),
				};

				if (row.NetInfo != null) {
					row.Ping = row.NetInfo.Q<Label>("ping");
					row.TickRate = row.NetInfo.Q<Label>("tickrate");
					row.Time = row.NetInfo.Q<Label>("time");
					row.Payload = row.NetInfo.Q<Label>("payload");
				}

				var viewButton = item.Q<Button>("view");
				viewButton.RegisterCallback<ClickEvent>(_ => OpenDetails(session));

				Subscribe(row);
				ApplyRow(row);

				_rows[session.Id] = row;
				_list.Add(item);
			}
		}

		/// <summary>
		/// Follows the session: every value shown on the line is pushed by an event of the session,
		/// there is no polling.
		/// </summary>
		private void Subscribe(SessionRow row) {
			var session = row.Session;

			row.OnState = _ => ApplyRow(row);
			session.OnStateChanged.AddListener(row.OnState);

			if (session is not INetworkSession net)
				return;

			row.OnConnected = () => ApplyRow(row);
			row.OnReconnected = () => ApplyRow(row);
			row.OnDisconnected = _ => ApplyRow(row);
			row.OnReconnecting = _ => ApplyRow(row);
			row.OnTickRate = _ => ApplyRow(row);
			row.OnPing = _ => ApplyRow(row);

			net.OnConnected.AddListener(row.OnConnected);
			net.OnReconnected.AddListener(row.OnReconnected);
			net.OnDisconnected.AddListener(row.OnDisconnected);
			net.OnReconnecting.AddListener(row.OnReconnecting);
			net.OnTickRateChanged.AddListener(row.OnTickRate);
			net.OnPingChanged.AddListener(row.OnPing);
		}

		/// <summary>Releases every handler a row registered on its session.</summary>
		private static void Unsubscribe(SessionRow row) {
			var session = row?.Session;
			if (session == null)
				return;

			if (row.OnState != null)
				session.OnStateChanged.RemoveListener(row.OnState);

			if (session is not INetworkSession net)
				return;

			if (row.OnConnected != null) net.OnConnected.RemoveListener(row.OnConnected);
			if (row.OnReconnected != null) net.OnReconnected.RemoveListener(row.OnReconnected);
			if (row.OnDisconnected != null) net.OnDisconnected.RemoveListener(row.OnDisconnected);
			if (row.OnReconnecting != null) net.OnReconnecting.RemoveListener(row.OnReconnecting);
			if (row.OnTickRate != null) net.OnTickRateChanged.RemoveListener(row.OnTickRate);
			if (row.OnPing != null) net.OnPingChanged.RemoveListener(row.OnPing);
		}

		/// <summary>Unsubscribes and forgets every row of the list.</summary>
		private void ClearRows() {
			foreach (var row in _rows.Values)
				Unsubscribe(row);

			_rows.Clear();
		}

		/// <summary>
		/// Shows or hides an element through the <c>hidden</c> class of <c>nox.style</c>: the panels
		/// never set inline styles.
		/// </summary>
		private static void SetVisible(VisualElement element, bool visible)
			=> element?.EnableInClassList("hidden", !visible);

		private void OpenDetails(ISession session) {
			var data = new Dictionary<string, object> { { "session", session } };

			if (!SessionPanels.API.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "details" }), out var panel)) {
				Logger.LogError("SessionDetailsPanel not found");
				return;
			}

			_window.SetActive(panel, data);
		}
	}
}
