using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using Nox.Entities;
using Nox.Players;
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

		/// <summary>
		/// Opened from the sessions list, never from the panel menu: it shows one session given as
		/// data, so it is not a browsable page.
		/// </summary>
		public bool IsVisible()
			=> false;

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

		/// <summary>Session API of this mod: the runtime entry point itself, no mod lookup needed.</summary>
		private static ISessionAPI SessionAPI
			=> Main.Instance;

		private VisualElement _content;
		private Button _back;
		private Label _title;
		private Label _sessionId;
		private Label _sessionState;
		private Label _sessionConnected;
		private Label _sessionPing;
		private Label _sessionTickRate;
		private Label _sessionTime;
		private Label _sessionPayload;
		private Label _worldId;
		private VisualElement _playersList;
		private VisualElement _playersEmpty;
		private VisualElement _entitiesList;
		private VisualElement _entitiesEmpty;
		private VisualElement _teamsList;
		private VisualElement _teamsEmpty;

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
			_session.OnEntityRegistered.AddListener(OnEntityChanged);
			_session.OnEntityUnregistered.AddListener(OnEntityChanged);

			if (_session is INetworkSession netSession) {
				netSession.OnConnected.AddListener(OnConnectionChanged);
				netSession.OnDisconnected.AddListener(OnDisconnectionChanged);
				netSession.OnPingChanged.AddListener(OnPingChanged);
				netSession.OnTickRateChanged.AddListener(OnTickRateChanged);
			}

			// The teams have their own events: a team created or removed, and a player switching team, are not
			// player join/leave events.
			if (_session is ITeamSession teamSession) {
				teamSession.OnTeamCreated.AddListener(OnTeamCreated);
				teamSession.OnTeamRemoved.AddListener(OnTeamRemoved);
				teamSession.OnTeamChanged.AddListener(OnTeamChanged);
			}
		}

		SessionAPI?.OnSessionRemoved.AddListener(OnSessionRemoved);
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
				_session.OnEntityRegistered.RemoveListener(OnEntityChanged);
				_session.OnEntityUnregistered.RemoveListener(OnEntityChanged);

				if (_session is INetworkSession netSession) {
					netSession.OnConnected.RemoveListener(OnConnectionChanged);
					netSession.OnDisconnected.RemoveListener(OnDisconnectionChanged);
					netSession.OnPingChanged.RemoveListener(OnPingChanged);
					netSession.OnTickRateChanged.RemoveListener(OnTickRateChanged);
				}

				if (_session is ITeamSession teamSession) {
					teamSession.OnTeamCreated.RemoveListener(OnTeamCreated);
					teamSession.OnTeamRemoved.RemoveListener(OnTeamRemoved);
					teamSession.OnTeamChanged.RemoveListener(OnTeamChanged);
				}
			}

			SessionAPI?.OnSessionRemoved.RemoveListener(OnSessionRemoved);

			_panel.Instance = null;
		}

		/// <summary>
		/// The displayed session was unregistered: the details page has nothing left to show, so it
		/// falls back to the sessions list.
		/// </summary>
		private void OnSessionRemoved(ISession session) {
			if (!ReferenceEquals(session, _session))
				return;

			GoBackToList();
		}

		// Event handlers for auto-update
		private void OnPlayerChanged(IPlayer player) {
			RefreshPlayers();
			RefreshTeams();
		}

		/// <summary>An entity of the session was registered or unregistered: the entities list follows.</summary>
		private void OnEntityChanged(IEntity entity)
			=> RefreshEntities();

		/// <summary>A team was created: it appears in the list, with no member yet.</summary>
		private void OnTeamCreated(ITeam team)
			=> RefreshTeamsAndPlayers();

		/// <summary>
		/// A team was removed: it leaves the list, and the players that kept it are detached from it, so the
		/// player rows have to be rebuilt as well.
		/// </summary>
		private void OnTeamRemoved(ITeam team)
			=> RefreshTeamsAndPlayers();

		/// <summary>
		/// A player joined, left or changed team: its colour, its tag and the member counts of the teams move.
		/// </summary>
		private void OnTeamChanged(ITeamEntity entity)
			=> RefreshTeamsAndPlayers();

		/// <summary>The two lists show the same data (the team of a player is shown on its row): rebuild both.</summary>
		private void RefreshTeamsAndPlayers() {
			RefreshTeams();
			RefreshPlayers();
		}

		private void OnStateChanged(IState state) {
			if (_sessionState != null)
				_sessionState.text = state?.Status.ToString() ?? "Unknown";
		}

		private void OnConnectionChanged() {
			if (_sessionConnected != null)
				_sessionConnected.text = "Yes";
			if (_session is INetworkSession netSession) {
				if (_sessionPing != null)
					_sessionPing.text = $"{netSession.Ping:F0} ms";
				if (_sessionTickRate != null)
					_sessionTickRate.text = $"{netSession.TickRate} t/s";
				if (_sessionTime != null)
					_sessionTime.text = $"{netSession.Time:HH:mm:ss}";
				if (_sessionPayload != null)
					_sessionPayload.text = $"{netSession.EventPayloadSize} B";
			}
		}

		private void OnDisconnectionChanged(string reason) {
			if (_sessionConnected != null)
				_sessionConnected.text = "No";
			if (_sessionPing != null)
				_sessionPing.text = "N/A";
			if (_sessionTickRate != null)
				_sessionTickRate.text = "N/A";
			if (_sessionTime != null)
				_sessionTime.text = "N/A";
			if (_sessionPayload != null)
				_sessionPayload.text = "N/A";
		}

		private void OnPingChanged(double ping) {
			if (_sessionPing != null)
				_sessionPing.text = $"{ping:F0} ms";
		}

		private void OnTickRateChanged(int tickRate) {
			if (_sessionTickRate != null)
				_sessionTickRate.text = $"{tickRate} t/s";
		}

		public VisualElement GetContent() {
			if (_content != null)
				return _content;

			var root = _panel.API.AssetAPI
				.GetAsset<VisualTreeAsset>("panels/details.uxml")
				.CloneTree();

			_back = root.Q<Button>("back");
			_title = root.Q<Label>("title");
			_sessionId = root.Q<Label>("session-id");
			_sessionState = root.Q<Label>("session-state");
			_sessionConnected = root.Q<Label>("session-connected");
			_sessionPing = root.Q<Label>("session-ping");
			_sessionTickRate = root.Q<Label>("session-tickrate");
			_sessionTime = root.Q<Label>("session-time");
			_sessionPayload = root.Q<Label>("session-payload");
			_worldId = root.Q<Label>("world-id");
			_playersList = root.Q<VisualElement>("players-list");
			_playersEmpty = root.Q<VisualElement>("players-empty");
			_entitiesList = root.Q<VisualElement>("entities-list");
			_entitiesEmpty = root.Q<VisualElement>("entities-empty");
			_teamsList = root.Q<VisualElement>("teams-list");
			_teamsEmpty = root.Q<VisualElement>("teams-empty");

			_back.RegisterCallback<ClickEvent>(OnBackClicked);

			// No refresh button: the page follows the session events (state, connection, players, teams).
			RefreshDetails();

			return _content = root;
		}

	private void OnBackClicked(ClickEvent evt)
		=> GoBackToList();

	/// <summary>Returns to the sessions list (the parent page of this one).</summary>
	private void GoBackToList() {
			if (!SessionPanels.API.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "sessions" }), out var panel)) {
			Logger.LogError("SessionsPanel not found");
			return;
		}
		_window.SetActive(panel);
	}

		private void RefreshDetails() {
			if (_session == null) {
				Logger.LogWarning("No session to display");
				return;
			}

			_title.text = $"Session: {_session.Id}";
			_sessionId.text = _session.Id;
			_sessionState.text = _session.State?.Status.ToString() ?? "Unknown";

			if (_session is INetworkSession netSession) {
				_sessionConnected.text = netSession.IsConnected ? "Yes" : "No";
				_sessionPing.text = netSession.IsConnected ? $"{netSession.Ping:F0} ms" : "N/A";
				_sessionTickRate.text = netSession.IsConnected ? $"{netSession.TickRate} t/s" : "N/A";
				_sessionTime.text = netSession.IsConnected ? $"{netSession.Time:HH:mm:ss}" : "N/A";
				_sessionPayload.text = netSession.IsConnected ? $"{netSession.EventPayloadSize} B" : "N/A";
			} else {
				_sessionConnected.text = "N/A";
				_sessionPing.text = "N/A";
				_sessionTickRate.text = "N/A";
				_sessionTime.text = "N/A";
				_sessionPayload.text = "N/A";
			}

			_worldId.text = _session.Dimensions?.Identifier.ToString() ?? "No world loaded";

			RefreshTeams();
			RefreshPlayers();
			RefreshEntities();
		}

		/// <summary>
		/// Lists the entities of the session that are not players (props, world objects, networked
		/// behaviours): the players have their own list above.
		/// </summary>
		private void RefreshEntities() {
			_entitiesList?.Clear();

			var registered = _session?.Entities?.GetEntities() ?? Array.Empty<IEntity>();

			var entities = new List<(IEntity Entity, bool Local)>();

			foreach (var entity in registered)
				if (entity is not IPlayer)
					entities.Add((entity, false));

			foreach (var entity in FindLocalEntities(registered))
				entities.Add((entity, true));

			if (entities.Count == 0) {
				SetVisible(_entitiesList, false);
				SetVisible(_entitiesEmpty, true);
				return;
			}

			SetVisible(_entitiesList, true);
			SetVisible(_entitiesEmpty, false);

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/entity-item.uxml");

			foreach (var (entity, local) in entities) {
				var item = itemAsset.CloneTree();

				var display = item.Q<Label>("display");
				display.text = entity.GetType().Name;
				display.tooltip = display.text;

				item.Q<Label>("id").text = $"ID: {entity.Id}";

				var tags = item.Q<VisualElement>("tags");

				// An entity the session did not register is not networked yet: it is shown so a missing
				// entity (a video player waiting for its spawn, a prop not published) is visible here.
				if (local)
					AddTag(tags, "Local", "badge-muted");

				if (entity is ISessionEntity)
					AddTag(tags, "Session", "badge-info");
				if (entity is IOwnedEntity)
					AddTag(tags, "Owned", "badge-warning");
				if (entity is IPhysicalEntity { PhysicalState: not PhysicalState.None })
					AddTag(tags, "Physical", "badge-muted");

				item.Q<Button>("view").RegisterCallback<ClickEvent>(_ => OpenEntity(entity));

				_entitiesList.Add(item);
			}
		}

		/// <summary>
		/// Entities of the running world the session did not register: the components implementing
		/// <see cref="IEntity"/> that were not wrapped by a session entity (yet).
		/// <para>
		/// The scan is narrowed to the live objects: the prepared copy of the world
		/// (<c>[Reference] …</c>, inactive) and the inactive objects of the editor scenes hold entities
		/// that are not part of the session, and an entity with an id is already listed through the
		/// session registry.
		/// </para>
		/// </summary>
		private static IEntity[] FindLocalEntities(IEntity[] registered) {
			var locals = new List<IEntity>();

			foreach (var behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>()) {
				if (behaviour is not IEntity entity || entity is IPlayer || entity.Id != 0)
					continue;

				if (!behaviour.gameObject.scene.IsValid() || !behaviour.gameObject.activeInHierarchy)
					continue;

				if (behaviour.transform.root.name.StartsWith(ReferenceRoot))
					continue;

				if (Array.IndexOf(registered, entity) >= 0 || locals.Contains(entity))
					continue;

				locals.Add(entity);
			}

			return locals.ToArray();
		}

		/// <summary>Root name of the prepared (inactive) copy of a world scene.</summary>
		private const string ReferenceRoot = "[Reference]";

		private void RefreshPlayers() {
			_playersList?.Clear();

			var players = _session.Entities?.GetEntities<IPlayer>()?.ToList() ?? new List<IPlayer>();

			if (players.Count == 0) {
				SetVisible(_playersList, false);
				SetVisible(_playersEmpty, true);
				return;
			}

			SetVisible(_playersList, true);
			SetVisible(_playersEmpty, false);

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/player-item.uxml");

			foreach (var player in players) {
				var item = itemAsset.CloneTree();

				var display = item.Q<Label>("display");
				display.text = player.Display;

				// Same rule as the nameplates: a player is shown with the colour of its team.
				if (player.Team != null)
					SetTint(display, player.Team.Color);

				item.Q<Label>("id").text = $"ID: {player.Id}";

				var tags = item.Q<VisualElement>("tags");
				if (player.IsLocal)
					AddTag(tags, "Local", "badge-info");
				if (player.IsMaster)
					AddTag(tags, "Master", "badge-warning");
				if (player.Team is { } team)
					AddTag(tags, team.Name, "badge-muted", new Color(team.Color.r, team.Color.g, team.Color.b, 0.25f), team.Color);

				var viewButton = item.Q<Button>("view");
				viewButton.RegisterCallback<ClickEvent>(_ => OpenPlayer(player));

				_playersList.Add(item);
			}
		}

		/// <summary>
		/// Lists the teams of the session (<see cref="ITeamSession"/>) with their colour and how many
		/// players joined them — the colours the players are shown with.
		/// </summary>
		private void RefreshTeams() {
			_teamsList?.Clear();

			var teams = (_session as ITeamSession)?.GetTeams();
			if (teams == null || teams.Length == 0) {
				SetVisible(_teamsList, false);
				SetVisible(_teamsEmpty, true);
				return;
			}

			SetVisible(_teamsList, true);
			SetVisible(_teamsEmpty, false);

			var players = _session.Entities?.GetEntities<IPlayer>() ?? Array.Empty<IPlayer>();

			foreach (var team in teams) {
				var row = new VisualElement();
				row.AddToClassList("row");
				row.AddToClassList("row-sm");

				var swatch = new VisualElement();
				swatch.AddToClassList("swatch");
				row.Add(swatch);

				var name = new Label($"{team.Name}  (id {team.Id})");
				name.AddToClassList("flex-grow");
				name.AddToClassList("text-ellipsis");
				name.tooltip = $"{team.Name} (id {team.Id})";
				row.Add(name);

				// The colour of a team is data, not a style: it can only be applied at runtime.
				SetTint(swatch, team.Color, background: true);
				SetTint(name, team.Color);

				var count   = players.Count(player => player.Team?.Id == team.Id);
				var members = new Label(count > 1 ? $"{count} players" : $"{count} player");
				members.AddToClassList("muted");
				row.Add(members);

				_teamsList.Add(row);
			}
		}

		/// <summary>
		/// Shows or hides an element through the <c>hidden</c> class of <c>nox.style</c>: the panels
		/// never set inline styles.
		/// </summary>
		private static void SetVisible(VisualElement element, bool visible)
			=> element?.EnableInClassList("hidden", !visible);

		/// <summary>
		/// Applies a runtime colour (team colour) to an element. This is the only place of the panel
		/// that writes an inline style: a USS class cannot carry a value known only at runtime.
		/// </summary>
		private static void SetTint(VisualElement element, Color? color, bool background = false) {
			if (element == null)
				return;

			if (color == null) {
				if (background)
					element.style.backgroundColor = StyleKeyword.Null;
				else
					element.style.color = StyleKeyword.Null;

				return;
			}

			if (background)
				element.style.backgroundColor = color.Value;
			else
				element.style.color = color.Value;
		}

		/// <summary>
		/// Adds a tag to a player row, using the badges of <c>nox.style</c>: the shared
		/// <c>badge</c> shape plus a semantic variant (<c>badge-info</c>, <c>badge-warning</c>...).
		/// The colours are only overridden for the team tag, which carries the colour of the team.
		/// </summary>
		private static void AddTag(VisualElement tags, string text, string variant, Color? background = null, Color? foreground = null) {
			if (tags == null || string.IsNullOrEmpty(text))
				return;

			var tag = new Label(text);
			tag.AddToClassList("badge");
			tag.AddToClassList("badge-sm");

			if (!string.IsNullOrEmpty(variant))
				tag.AddToClassList(variant);

			SetTint(tag, background, background: true);
			SetTint(tag, foreground);

			tags.Add(tag);
		}

		private void OpenPlayer(IPlayer player)
			=> OpenEntity(player);

		/// <summary>Opens the details page of an entity: the same page as a player, fed with an entity.</summary>
		private void OpenEntity(IEntity entity) {
			var data = new Dictionary<string, object> {
				{ "session", _session },
				{ "entity", entity }
			};

			if (!SessionPanels.API.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "player" }), out var panel)) {
				Logger.LogError("EntityPanel not found");
				return;
			}

			_window.SetActive(panel, data);
		}
	}
}
