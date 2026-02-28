#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Players;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using Nox.Players;
using Nox.Sessions;
using UnityEngine;
using UnityEngine.UIElements;
using IPanel = Nox.Editor.Panel.IPanel;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Sessions.Runtime.Editor {
	public class SessionDetailsPanel : IEditorModInitializer, IPanel {
		private static readonly string[] PanelPath = { "session", "details" };
		internal IEditorModCoreAPI API;

		public void OnInitializeEditor(IEditorModCoreAPI api)
			=> API = api;

		public void OnDisposeEditor()
			=> API = null;

		public string[] GetPath()
			=> PanelPath;

		internal SessionDetailsInstance Instance;

		public IInstance[] GetInstances()
			=> Instance != null
				? new IInstance[] { Instance }
				: Array.Empty<IInstance>();

		public string GetLabel()
			=> "Session/Details";

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data) {
			if (Instance != null)
				throw new InvalidOperationException("SessionDetailsPanel only supports a single instance.");
			return Instance = new SessionDetailsInstance(this, window, data);
		}
	}

	public class SessionDetailsInstance : IInstance {
		private readonly SessionDetailsPanel _panel;
		private readonly IWindow _window;
		private readonly ISession _session;

		private VisualElement _content;
		private Button _back;
		private Button _refresh;
		private Label _title;
		private Label _sessionId;
		private Label _sessionState;
		private Label _sessionConnected;
		private Label _sessionPing;
		private Label _worldId;
		private VisualElement _playersList;
		private VisualElement _playersEmpty;

	public SessionDetailsInstance(SessionDetailsPanel panel, IWindow window, Dictionary<string, object> data) {
		_panel = panel;
		_window = window;
		
		if (data != null && data.TryGetValue("session", out var sessionObj))
			_session = sessionObj as ISession;
		
		// Subscribe to session events for auto-update
		if (_session != null) {
			_session.OnPlayerJoined.AddListener(OnPlayerChanged);
			_session.OnPlayerLeft.AddListener(OnPlayerChanged);
			_session.OnStateChanged.AddListener(OnStateChanged);
			
			if (_session is INetSession netSession) {
				netSession.OnConnected.AddListener(OnConnectionChanged);
				netSession.OnDisconnected.AddListener(OnDisconnectionChanged);
				netSession.OnPingChanged.AddListener(OnPingChanged);
			}
		}
	}

		public IPanel GetPanel()
			=> _panel;

		public IWindow GetWindow()
			=> _window;

		public string GetTitle()
			=> $"Session: {_session?.Id ?? "Unknown"}";

		public void OnDestroy() {
			// Unsubscribe from events
			if (_session != null) {
				_session.OnPlayerJoined.RemoveListener(OnPlayerChanged);
				_session.OnPlayerLeft.RemoveListener(OnPlayerChanged);
				_session.OnStateChanged.RemoveListener(OnStateChanged);
				
				if (_session is INetSession netSession) {
					netSession.OnConnected.RemoveListener(OnConnectionChanged);
					netSession.OnDisconnected.RemoveListener(OnDisconnectionChanged);
					netSession.OnPingChanged.RemoveListener(OnPingChanged);
				}
			}
			
			_panel.Instance = null;
		}
		
		// Event handlers for auto-update
		private void OnPlayerChanged(IPlayer player) {
			RefreshPlayers();
		}
		
		private void OnStateChanged(IState state) {
			if (_sessionState != null)
				_sessionState.text = state?.Status.ToString() ?? "Unknown";
		}
		
		private void OnConnectionChanged() {
			if (_sessionConnected != null)
				_sessionConnected.text = "Yes";
			if (_sessionPing != null && _session is INetSession netSession)
				_sessionPing.text = $"{netSession.Ping:F0} ms";
		}
		
		private void OnDisconnectionChanged(string reason) {
			if (_sessionConnected != null)
				_sessionConnected.text = "No";
			if (_sessionPing != null)
				_sessionPing.text = "N/A";
		}
		
		private void OnPingChanged(double ping) {
			if (_sessionPing != null)
				_sessionPing.text = $"{ping:F0} ms";
		}

		public VisualElement GetContent() {
			if (_content != null)
				return _content;

			var root = _panel.API.AssetAPI
				.GetAsset<VisualTreeAsset>("panels/details.uxml")
				.CloneTree();

			_back = root.Q<Button>("back");
			_refresh = root.Q<Button>("refresh");
			_title = root.Q<Label>("title");
			_sessionId = root.Q<Label>("session-id");
			_sessionState = root.Q<Label>("session-state");
			_sessionConnected = root.Q<Label>("session-connected");
			_sessionPing = root.Q<Label>("session-ping");
			_worldId = root.Q<Label>("world-id");
			_playersList = root.Q<VisualElement>("players-list");
			_playersEmpty = root.Q<VisualElement>("players-empty");

			_back.RegisterCallback<ClickEvent>(OnBackClicked);
			_refresh.RegisterCallback<ClickEvent>(OnRefreshClicked);

			RefreshDetails();

			return _content = root;
		}

	private void OnBackClicked(ClickEvent evt) {
		if (!Main.PanelAPI.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "sessions" }), out var panel)) {
			Logger.LogError("SessionsPanel not found");
			return;
		}
		_window.SetActive(panel);
	}

		private void OnRefreshClicked(ClickEvent evt)
			=> RefreshDetails();

		private void RefreshDetails() {
			if (_session == null) {
				Logger.LogWarning("No session to display");
				return;
			}

			_title.text = $"Session: {_session.Id}";
			_sessionId.text = _session.Id;
			_sessionState.text = _session.State?.Status.ToString() ?? "Unknown";
			
			if (_session is INetSession netSession) {
				_sessionConnected.text = netSession.IsConnected ? "Yes" : "No";
				_sessionPing.text = netSession.IsConnected ? $"{netSession.Ping:F0} ms" : "N/A";
			} else {
				_sessionConnected.text = "N/A";
				_sessionPing.text = "N/A";
			}

			_worldId.text = _session.Dimensions?.Identifier?.ToString() ?? "No world loaded";

			RefreshPlayers();
		}

		private void RefreshPlayers() {
			_playersList?.Clear();

			var players = _session.Entities?.GetEntities<IPlayer>()?.ToList() ?? new List<IPlayer>();

			if (players.Count == 0) {
				_playersEmpty.style.display = DisplayStyle.Flex;
				_playersList.style.display = DisplayStyle.None;
				return;
			}

			_playersEmpty.style.display = DisplayStyle.None;
			_playersList.style.display = DisplayStyle.Flex;

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/player-item.uxml");

			foreach (var player in players) {
				var item = itemAsset.CloneTree();
				
				item.Q<Label>("display").text = player.Display;
				item.Q<Label>("id").text = $"ID: {player.Id}";

				var tags = item.Q<VisualElement>("tags");
				if (player.IsLocal) {
					var tag = new Label("Local");
					tag.style.backgroundColor = new Color(0.2f, 0.6f, 0.9f, 0.3f);
					tag.style.paddingLeft = 4;
					tag.style.paddingRight = 4;
					tag.style.paddingTop = 2;
					tag.style.paddingBottom = 2;
					tag.style.marginRight = 4;
					tag.style.borderTopLeftRadius = 3;
					tag.style.borderTopRightRadius = 3;
					tag.style.borderBottomLeftRadius = 3;
					tag.style.borderBottomRightRadius = 3;
					tags.Add(tag);
				}
				if (player.IsMaster) {
					var tag = new Label("Master");
					tag.style.backgroundColor = new Color(0.9f, 0.6f, 0.2f, 0.3f);
					tag.style.paddingLeft = 4;
					tag.style.paddingRight = 4;
					tag.style.paddingTop = 2;
					tag.style.paddingBottom = 2;
					tag.style.borderTopLeftRadius = 3;
					tag.style.borderTopRightRadius = 3;
					tag.style.borderBottomLeftRadius = 3;
					tag.style.borderBottomRightRadius = 3;
					tags.Add(tag);
				}

				var viewButton = item.Q<Button>("view");
				viewButton.RegisterCallback<ClickEvent>(_ => OpenPlayer(player));

				_playersList.Add(item);
			}
		}

		private void OpenPlayer(IPlayer player) {
			var data = new Dictionary<string, object> { 
				{ "session", _session },
				{ "player", player }
			};
			
			if (!Main.PanelAPI.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "player" }), out var panel)) {
				Logger.LogError("PlayerPanel not found");
				return;
			}

			_window.SetActive(panel, data);
		}
	}
}
#endif