using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Players;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Entities;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using Nox.Entities;
using Nox.Players;
using UnityEngine;
using UnityEngine.UIElements;
using IPanel = Nox.Editor.Panel.IPanel;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Sessions.Runtime.Editor {
	public class PlayerPanel : IEditorModInitializer, IPanel {
		private static readonly string[] PanelPath = { "session", "player" };
		internal IEditorModCoreAPI API;

		public void OnInitializeEditor(IEditorModCoreAPI api)
			=> API = api;

		public void OnDisposeEditor()
			=> API = null;

		public string[] GetPath()
			=> PanelPath;

		internal PlayerInstance Instance;

		public IInstance[] GetInstances()
			=> Instance != null
				? new IInstance[] { Instance }
				: Array.Empty<IInstance>();

		public string GetLabel()
			=> "Session/Player";

		/// <summary>
		/// Opened from the session details, never from the panel menu: it shows one player given as
		/// data, so it is not a browsable page.
		/// </summary>
		public bool IsVisible()
			=> false;

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data) {
			if (Instance != null)
				throw new InvalidOperationException("PlayerPanel only supports a single instance.");
			return Instance = new PlayerInstance(this, window, data);
		}
	}

	internal struct PropertyRow {
		public VisualElement Container;
		public Label ValueLabel;
		public Label FlagsLabel;
		public string Group;
		/// <summary>Last rendered value, so a live property is caught even when it does not stamp.</summary>
		public string Value;
		public DateTime LastUpdatedAt;
		public DateTime ChangedAt;
	}

	/// <summary>
	/// A section of the properties list.
	/// </summary>
	internal readonly struct PropertyGroup {
		public readonly int Order;
		public readonly string Label;
		public readonly string Description;

		/// <summary>
		/// Whether the group is split into subsections, one per parameter category (the avatar parameters).
		/// </summary>
		public readonly bool Categorized;

		/// <summary>
		/// Category of the avatar parameter (snake_case), or <c>null</c> when it declares none: the
		/// parameters without a category go to the « general » subsection.
		/// </summary>
		public readonly string Category;

		public PropertyGroup(int order, string label, string description, bool categorized = false, string category = null) {
			Order       = order;
			Label       = label;
			Description = description;
			Categorized = categorized;
			Category    = category;
		}
	}

	internal struct PartRow {
		public VisualElement Container;
		public Label PosLabel;
		public Label RotLabel;
		public DateTime LastUpdated;
		public DateTime ChangedAt;
	}

	public class PlayerInstance : IInstance {
		private readonly PlayerPanel _panel;
		private readonly IWindow _window;
		private readonly ISession _session;
		private readonly IPlayer _player;

		private VisualElement _content;
		private Button _back;
		private Label _title;
		private Label _playerId;
		private Label _playerDisplay;
		private Label _playerLocal;
		private Label _playerMaster;
		private Label _avatarId;
		private VisualElement _teamColor;
		private Label _teamName;
		private Label _teamId;
		private Label _health;
		private Label _maxHealth;
		private Label _invisible;
		private Label _walkSpeed;
		private Label _moveAcceleration;
		private Label _jumpForce;
		private Label _sprintMultiplier;
		private Label _airControl;
		private Label _flySpeed;
		private Label _mayFly;
		private Label _immobilized;
		private Label _flying;
		private Label _crouching;
		private Label _sprinting;
		private Label _nameplateVisible;
		private Label _healthbarVisible;
		/// <summary>Team colour last applied, so the swatch/name style is not touched every tick.</summary>
		private int _teamColorId = int.MinValue;
		private VisualElement _propertiesList;
		private VisualElement _propertiesEmpty;
		private VisualElement _partsList;
		private VisualElement _partsEmpty;

		// Auto-update state
		private IVisualElementScheduledItem _schedule;
		private Dictionary<int, PropertyRow> _rows = new();
		private Dictionary<ushort, PartRow> _partRows = new();

		// Fade duration: background alpha goes from 0.5 to 0 over this many seconds
		private const float FadeDuration = 0.5f;
		// Scheduler tick rate in ms
		private const int TickMs = 50;

		public PlayerInstance(PlayerPanel panel, IWindow window, Dictionary<string, object> data) {
			_panel = panel;
			_window = window;

			if (data != null) {
				if (data.TryGetValue("session", out var sessionObj))
					_session = sessionObj as ISession;
				if (data.TryGetValue("player", out var playerObj))
					_player = playerObj as IPlayer;
			}

			_session?.OnPlayerLeft.AddListener(OnPlayerLeft);
			_session?.Entities?.OnEntityRemoved.AddListener(OnEntityRemoved);

			// The teams have their own events: a player switching team, or the team it uses being removed, is
			// not a player event.
			if (_session is ITeamSession teamSession) {
				teamSession.OnTeamChanged.AddListener(OnTeamChanged);
				teamSession.OnTeamRemoved.AddListener(OnTeamRemoved);
			}

			Main.Instance?.OnSessionRemoved.AddListener(OnSessionRemoved);
		}

		public IPanel GetPanel()
			=> _panel;

		public IWindow GetWindow()
			=> _window;

		public string GetTitle()
			=> $"Player: {_player?.Display ?? "Unknown"}";

		public void OnDestroy() {
			_schedule?.Pause();
			_schedule = null;
			_rows.Clear();

			_session?.OnPlayerLeft.RemoveListener(OnPlayerLeft);
			_session?.Entities?.OnEntityRemoved.RemoveListener(OnEntityRemoved);

			if (_session is ITeamSession teamSession) {
				teamSession.OnTeamChanged.RemoveListener(OnTeamChanged);
				teamSession.OnTeamRemoved.RemoveListener(OnTeamRemoved);
			}

			Main.Instance?.OnSessionRemoved.RemoveListener(OnSessionRemoved);

			_panel.Instance = null;
		}

		/// <summary>The displayed player left the session: back to the session page.</summary>
		private void OnPlayerLeft(IPlayer player) {
			if (_player == null || player == null || player.Id != _player.Id)
				return;

			LeavePage(GoBackToSession);
		}

		/// <summary>The displayed player joined, left or changed team: its team row follows it.</summary>
		private void OnTeamChanged(ITeamEntity entity) {
			if (_player == null || entity == null || entity.Id != _player.Id)
				return;

			RefreshStats();
		}

		/// <summary>
		/// The team shown on this page was removed: the registry already dropped it, so the player now resolves
		/// to no team and the row has to be rewritten.
		/// </summary>
		private void OnTeamRemoved(ITeam team) {
			if (_player == null || team == null || _teamColorId != team.Id)
				return;

			RefreshStats();
		}

		/// <summary>The displayed player was unregistered from the entities: back to the session page.</summary>
		private void OnEntityRemoved(IEntity entity) {
			if (_player == null || entity == null || entity.Id != _player.Id)
				return;

			LeavePage(GoBackToSession);
		}

		/// <summary>The session was unregistered: even the session page is gone, back to the list.</summary>
		private void OnSessionRemoved(ISession session) {
			if (!ReferenceEquals(session, _session))
				return;

			LeavePage(GoBackToSessions);
		}

		/// <summary>Stops the auto-update before navigating away (the page is destroyed by the window).</summary>
		private void LeavePage(Action navigate) {
			_schedule?.Pause();
			_schedule = null;
			navigate?.Invoke();
		}

		public VisualElement GetContent() {
			if (_content != null)
				return _content;

			var root = _panel.API.AssetAPI
				.GetAsset<VisualTreeAsset>("panels/player.uxml")
				.CloneTree();

			_back = root.Q<Button>("back");
			_title = root.Q<Label>("title");
			_playerId = root.Q<Label>("player-id");
			_playerDisplay = root.Q<Label>("player-display");
			_playerLocal = root.Q<Label>("player-local");
			_playerMaster = root.Q<Label>("player-master");
			_avatarId = root.Q<Label>("avatar-id");
			_teamColor = root.Q<VisualElement>("team-color");
			_teamName = root.Q<Label>("team-name");
			_teamId = root.Q<Label>("team-id");
			_health = root.Q<Label>("health");
			_maxHealth = root.Q<Label>("max-health");
			_invisible = root.Q<Label>("invisible");
			_walkSpeed = root.Q<Label>("walk-speed");
			_moveAcceleration = root.Q<Label>("move-acceleration");
			_jumpForce = root.Q<Label>("jump-force");
			_sprintMultiplier = root.Q<Label>("sprint-multiplier");
			_airControl = root.Q<Label>("air-control");
			_flySpeed = root.Q<Label>("fly-speed");
			_mayFly = root.Q<Label>("may-fly");
			_immobilized = root.Q<Label>("immobilized");
			_flying = root.Q<Label>("flying");
			_crouching = root.Q<Label>("crouching");
			_sprinting = root.Q<Label>("sprinting");
			_nameplateVisible = root.Q<Label>("nameplate-visible");
			_healthbarVisible = root.Q<Label>("healthbar-visible");
			_propertiesList = root.Q<VisualElement>("properties-list");
			_propertiesEmpty = root.Q<VisualElement>("properties-empty");
			_partsList = root.Q<VisualElement>("parts-list");
			_partsEmpty = root.Q<VisualElement>("parts-empty");

			_back.RegisterCallback<ClickEvent>(OnBackClicked);

			LoadPlayerDetails();

			// Schedule auto-update every TickMs milliseconds
			_schedule = root.schedule
				.Execute(OnTick)
				.Every(TickMs);

			return _content = root;
		}

		//  Scheduler

		private static string FormatVec3(Vector3 v)
			=> $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
		private static string FormatEuler(Quaternion q) { var e = q.eulerAngles; return $"({e.x:F1}\u00b0, {e.y:F1}\u00b0, {e.z:F1}\u00b0)"; }

		private static string FormatValue(object v) {
			if (v == null) return "null";
			if (v is not byte[] b) return v.ToString();
			var count = Math.Min(b.Length, 10);
			var hex = BitConverter.ToString(b, 0, count).Replace('-', ' ');
			var ellipsis = b.Length > 10 ? "..." : "";
			return $"Buffer ({b.Length}) {hex}{ellipsis}";
		}

		/// <summary>Above this length a label is likely elided, so its tooltip carries the full text.</summary>
		private const int TooltipThreshold = 64;

		/// <summary>Maximum length of a path-like value before its leading segments are dropped.</summary>
		private const int PathMaxLength = 96;

		/// <summary>
		/// Shortens a path-like value by dropping whole leading segments, so the cut falls on a separator
		/// instead of in the middle of a segment: <c>/a/b/c/d</c> becomes <c>.../c/d</c>. The tail is kept,
		/// like the left ellipsis of the labels. A value without separator is returned as-is and left to
		/// the label ellipsis.
		/// </summary>
		private static string ShortenPath(string value) {
			if (string.IsNullOrEmpty(value) || value.Length <= PathMaxLength || !value.Contains('/'))
				return value;

			var segments = value.Split('/');
			var result = segments[segments.Length - 1];

			for (var i = segments.Length - 2; i >= 0; i--) {
				var candidate = segments[i] + "/" + result;
				if (candidate.Length + 2 > PathMaxLength) // +2 for the leading ".../"
					break;
				result = candidate;
			}

			return ".../" + result;
		}

		/// <summary>
		/// Gives a label the HTML <c>title</c> behaviour: its tooltip carries the full text, but only when
		/// the text is long enough to be elided (the actual elision depends on the panel width, which is
		/// not known here, so the length is used as the hint).
		/// </summary>
		private static void SetTooltip(Label label, string full) {
			if (label != null)
				label.tooltip = full != null && full.Length > TooltipThreshold ? full : null;
		}

		private void OnTick() {
			if (_player is not IEntity entity)
				return;

			RefreshStats();

			// Mise à jour des parts
			var parts = _player.GetParts();
			if (parts.Length != _partRows.Count)
				LoadParts();
			else {
				var now2 = DateTime.UtcNow;
				foreach (var part in parts)
					if (_partRows.TryGetValue(part.Id, out var partRow)) {
						// An offline part reads its live transform from the controller and only bumps
						// `Updated` when something writes it, so the rendered values are compared as
						// well: otherwise the row would never move.
						var position = FormatVec3(part.Position);
						var rotation = FormatEuler(part.Rotation);
						var changed = part.Updated != partRow.LastUpdated
									|| partRow.PosLabel.text != position
									|| partRow.RotLabel.text != rotation;

						if (changed) {
							partRow.PosLabel.text = position;
							partRow.RotLabel.text = rotation;
							partRow.ChangedAt = now2;
							partRow.LastUpdated = part.Updated;
							_partRows[part.Id] = partRow;
						}

						var elapsed2 = (float)(now2 - partRow.ChangedAt).TotalSeconds;
						var alpha2 = Mathf.Max(0f, 0.5f - elapsed2 / FadeDuration * 0.5f);
						SetHighlight(partRow.Container, alpha2);
					}
			}

			var properties = entity.GetProperties();

			// If the property count changed, do a full rebuild
			if (properties == null || properties.Length != _rows.Count) {
				LoadProperties();
				return;
			}

			// A property can also change group without the count changing (an UnassignedProperty gets
			// bound to an avatar parameter, or released back): rebuild so it moves to the right section.
			foreach (var property in properties)
				if (_rows.TryGetValue(property.Key, out var existing) && existing.Group != GroupLabel(GetPropertyGroup(property))) {
					LoadProperties();
					return;
				}

			var now = DateTime.UtcNow;

			foreach (var property in properties) {
				if (!_rows.TryGetValue(property.Key, out var row))
					continue;

				// The value is read first on purpose: a live property (a parameter bound to the avatar)
				// only stamps its UpdatedAt when it is read, so comparing beforehand would never
				// report a change.
				var value = FormatValue(property.Value);
				var changed = property.UpdatedAt != row.LastUpdatedAt || row.Value != value;

				if (changed) {
					var flags = property.Flags.ToString();

					row.ValueLabel.text = ShortenPath(value);
					row.FlagsLabel.text = flags;
					SetTooltip(row.ValueLabel, value);
					SetTooltip(row.FlagsLabel, flags);

					row.Value = value;
					row.LastUpdatedAt = property.UpdatedAt;
					row.ChangedAt = now;
					_rows[property.Key] = row;
				}

				// Fade background: alpha from 0.5 to 0 over FadeDuration seconds
				var elapsed = (float)(now - row.ChangedAt).TotalSeconds;
				var alpha = Mathf.Max(0f, 0.5f - elapsed / FadeDuration * 0.5f);
				SetHighlight(row.Container, alpha);
			}
		}

		//  Navigation

		private void OnBackClicked(ClickEvent evt)
			=> GoBackToSession();

		/// <summary>Returns to the session page (the parent page of this one).</summary>
		private void GoBackToSession() {
			if (_session == null) {
				Logger.LogWarning("No session to go back to");
				GoBackToSessions();
				return;
			}

			var data = new Dictionary<string, object> { { "session", _session } };

			if (!SessionPanels.API.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "details" }), out var panel)) {
				Logger.LogError("SessionDetailsPanel not found");
				return;
			}

			_window.SetActive(panel, data);
		}

		/// <summary>Returns to the sessions list (the root page of the session panels).</summary>
		private void GoBackToSessions() {
			if (!SessionPanels.API.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "sessions" }), out var panel)) {
				Logger.LogError("SessionsPanel not found");
				return;
			}

			_window.SetActive(panel);
		}

		//  Display

		private void LoadPlayerDetails() {
			if (_player == null) {
				Logger.LogWarning("No player to display");
				return;
			}

			_title.text = $"Player: {_player.Display}";
			_playerId.text = _player.Id.ToString();
			_playerLocal.text = _player.IsLocal ? "Yes" : "No";
			_playerMaster.text = _player.IsMaster ? "Yes" : "No";

			var display = _player.Display;
			_playerDisplay.text = display;
			SetTooltip(_playerDisplay, display);

			var avatar = _player is IPlayerAvatar playerAvatar ? playerAvatar.GetAvatar().ToString() : null;
			_avatarId.text = ShortenPath(avatar ?? "N/A");
			SetTooltip(_avatarId, avatar);

			LoadParts();
			LoadProperties();
			RefreshStats();
		}

		//  Entity data

		/// <summary>
		/// Refreshes the entity data of the new model (team, vitals, movement, nameplate). Cheap reads:
		/// a label is only touched when its text changes, so the periodic refresh does not dirty the
		/// layout for nothing.
		/// </summary>
		private void RefreshStats() {
			var player = _player;
			if (player == null)
				return;

			var team = player.Team;

			Set(_teamName, team?.Name ?? "No team");
			Set(_teamId, team != null ? team.Id.ToString() : "-");

			// The team name carries the colour of the team (the swatch next to it as well).
			var teamColorId = team?.Id ?? -1;
			if (teamColorId != _teamColorId) {
				_teamColorId = teamColorId;

				SetVisible(_teamColor, team != null);
				SetTint(_teamColor, team?.Color, background: true);
				SetTint(_teamName, team?.Color);
			}

			Set(_health, FormatFloat(player.Health));
			Set(_maxHealth, FormatFloat(player.MaxHealth));
			Set(_invisible, FormatBool(player.IsInvisible));

			Set(_walkSpeed, FormatFloat(player.WalkSpeed));
			Set(_moveAcceleration, FormatFloat(player.MoveAcceleration));
			Set(_jumpForce, FormatFloat(player.JumpForce));
			Set(_sprintMultiplier, FormatFloat(player.SprintMultiplier));
			Set(_airControl, FormatFloat(player.AirControl));
			Set(_flySpeed, FormatFloat(player.FlySpeed));
			Set(_mayFly, FormatBool(player.MayFly));
			Set(_immobilized, FormatBool(player.IsImmobilized));
			Set(_flying, FormatBool(player.IsFlying));
			Set(_crouching, FormatBool(player.IsCrouching));
			Set(_sprinting, FormatBool(player.IsSprinting));

			Set(_nameplateVisible, FormatFlag(ReadFlag(player, "NameplateVisible")));
			Set(_healthbarVisible, FormatFlag(ReadFlag(player, "HealthbarVisible")));
		}

		private static void Set(Label label, string value) {
			if (label != null && label.text != value)
				label.text = value;
		}

		private static string FormatFloat(float value) => value.ToString("0.###");
		private static string FormatBool(bool value) => value ? "Yes" : "No";

		private static string FormatFlag(bool? value)
			=> value switch {
				true => "Yes",
				false => "No",
				_ => "N/A",
			};

		/// <summary>
		/// Reads a value from an interface this editor assembly does not reference: the nameplate flags
		/// live in <c>Nox.Nameplate</c>, which the session assembly does not depend on. Same approach as
		/// <see cref="GetPropertyGroup"/>, which matches the concrete type names on purpose.
		/// </summary>
		private static bool? ReadFlag(object target, string property) {
			var value = target?.GetType().GetProperty(property)?.GetValue(target);
			return value is bool flag ? flag : null;
		}

		private void LoadParts() {
			_partsList?.Clear();
			_partRows.Clear();

			var parts = _player?.GetParts();
			if (parts == null || parts.Length == 0) {
				SetVisible(_partsList, false);
				SetVisible(_partsEmpty, true);
				return;
			}

			SetVisible(_partsList, true);
			SetVisible(_partsEmpty, false);

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/part-item.uxml");
			var epoch = DateTime.MinValue;
			foreach (var part in parts) {
				var item = itemAsset.CloneTree();
				var container = item.Q<VisualElement>();
				var posLabel = item.Q<Label>("position");
				var rotLabel = item.Q<Label>("rotation");

				var nameLabel = item.Q<Label>("name");
				var name = part.Id.ToEntityRig().ToString();
				nameLabel.text = name;
				SetTooltip(nameLabel, name);
				posLabel.text = FormatVec3(part.Position);
				rotLabel.text = FormatEuler(part.Rotation);

				_partsList.Add(item);
				_partRows[part.Id] = new PartRow {
					Container = container,
					PosLabel = posLabel,
					RotLabel = rotLabel,
					LastUpdated = part.Updated,
					ChangedAt = epoch,
				};
			}
		}

		/// <summary>
		/// Maps a property to its section. Matching is done on the concrete type <b>name</b> rather
		/// than on a type reference on purpose: this editor assembly does not reference
		/// Nox.Relay.Runtime, where the implementations live.
		/// </summary>
		private static PropertyGroup GetPropertyGroup(IProperty property) {
			switch (property.GetType().Name) {
				case "AvatarParameterProperty":
					return new PropertyGroup(
						0, "Avatar Parameters",
						"Bound to a live avatar parameter: read once per tick and sent according to its sync flags.",
						categorized: true,
						category: ParameterCategoryLanguage.Normalize(property.Category)
					);
				case "UnassignedProperty":
					return new PropertyGroup(1, "Unassigned (key never declared)",
						"A value arrived for a key this entity has no property for, so Nox.Relay stored it as-is. " +
						"If this section keeps growing with new keys, the other client is sending keys we never registered.");
				case "Property":
					return new PropertyGroup(2, "Raw",
						"Plain key/value property declared by the entity itself.");
				case "DataProperty":
					return new PropertyGroup(2, "Data",
						"Value of the entity data container: the state an offline session keeps for its player.");
				default:
					return new PropertyGroup(3, property.GetType().Name,
						"Custom IProperty implementation.");
			}
		}

		/// <summary>
		/// Key of the section a property belongs to, used to detect a property that moved to another group
		/// or another parameter category without the property count changing.
		/// </summary>
		private static string GroupLabel(PropertyGroup group)
			=> group.Categorized
				? group.Label + "/" + ParameterCategoryLanguage.NormalizedOrDefault(group.Category)
				: group.Label;

		/// <summary>
		/// Shows or hides an element through the <c>hidden</c> class of <c>nox.style</c>: the panels
		/// never set inline styles.
		/// </summary>
		private static void SetVisible(VisualElement element, bool visible)
			=> element?.EnableInClassList("hidden", !visible);

		/// <summary>
		/// Applies a runtime colour (team colour) to an element. This is one of the few places that
		/// write an inline style: a USS class cannot carry a value known only at runtime.
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
		/// Paints the "just changed" highlight of a row: the alpha decays every tick, so it cannot be
		/// expressed as a USS class.
		/// </summary>
		private static void SetHighlight(VisualElement element, float alpha) {
			if (element != null)
				element.style.backgroundColor = new Color(0.2f, 0.6f, 1f, alpha);
		}

		private static VisualElement CreateGroupHeader(PropertyGroup group, int count) {
			var header = new Label($"{group.Label}  ({count})");
			header.tooltip = group.Description;
			header.AddToClassList("group-header");

			return header;
		}

		/// <summary>
		/// Header of a parameter category inside the avatar parameters: its label entry
		/// (<c>avatar.parameters.categories.&lt;category&gt;.label</c>), with the description entry as tooltip.
		/// </summary>
		private static VisualElement CreateCategoryHeader(string category, int count) {
			var header = new Label($"{ParameterCategoryLanguage.GetLabel(category)}  ({count})") {
				tooltip = ParameterCategoryLanguage.GetDescription(category)
			};
			header.AddToClassList("group-header");

			return header;
		}

		private void LoadProperties() {
			_propertiesList?.Clear();
			_rows.Clear();

			if (_player is not IEntity entity) {
				SetVisible(_propertiesList, false);
				SetVisible(_propertiesEmpty, true);
				return;
			}

			var properties = entity.GetProperties();

			if (properties == null || properties.Length == 0) {
				SetVisible(_propertiesList, false);
				SetVisible(_propertiesEmpty, true);
				return;
			}

			SetVisible(_propertiesList, true);
			SetVisible(_propertiesEmpty, false);

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/property-item.uxml");

			// Group by concrete implementation instead of listing everything flat: that is what tells a
			// parameter bound to the avatar apart from an incoming key this entity never declared.
			var groups = properties
				.Select(p => (Property: p, Group: GetPropertyGroup(p)))
				.GroupBy(x => (x.Group.Order, x.Group.Label))
				.OrderBy(g => g.Key.Order)
				.ThenBy(g => g.Key.Label, StringComparer.Ordinal);

			foreach (var group in groups)
				AddPropertyGroup(itemAsset, group.ToList());
		}

		/// <summary>
		/// Adds a top-level group. The avatar parameters are split into one subsection per category, the
		/// parameters without a category first in the « general » subsection; the other groups stay flat.
		/// The subsections are ordered by category, not by their label (which depends on the language).
		/// </summary>
		private void AddPropertyGroup(VisualTreeAsset itemAsset, List<(IProperty Property, PropertyGroup Group)> items) {
			var group = items[0].Group;

			_propertiesList.Add(CreateGroupHeader(group, items.Count));

			if (!group.Categorized) {
				AddPropertyRows(itemAsset, items);
				return;
			}

			var sections = items
				.GroupBy(x => ParameterCategoryLanguage.Normalize(x.Group.Category))
				.OrderBy(s => s.Key == null ? 0 : 1)
				.ThenBy(s => s.Key ?? string.Empty, StringComparer.Ordinal);

			foreach (var section in sections) {
				_propertiesList.Add(CreateCategoryHeader(section.Key, section.Count()));
				AddPropertyRows(itemAsset, section.ToList());
			}
		}

		/// <summary>
		/// Adds the rows of a section: named properties first (alphabetical), then the raw keys ascending —
		/// so an unexplained key is easy to pick out and stays in the same place between rebuilds.
		/// </summary>
		private void AddPropertyRows(VisualTreeAsset itemAsset, List<(IProperty Property, PropertyGroup Group)> items) {
			var epoch = DateTime.MinValue;

			var ordered = items
				.OrderBy(x => x.Property.Name == null ? 1 : 0)
				.ThenBy(x => x.Property.Name ?? string.Empty, StringComparer.Ordinal)
				.ThenBy(x => x.Property.Key);

			foreach (var (property, propertyGroup) in ordered) {
				var item = itemAsset.CloneTree();
				var container = item.Q<VisualElement>(); // root element of the template
				var valueLabel = item.Q<Label>("value");
				var flagsLabel = item.Q<Label>("flags");

				var keyLabel = item.Q<Label>("key");
				var key = property.Name ?? $"Key: {property.Key}";
				var value = FormatValue(property.Value);
				var flags = property.Flags.ToString();

				keyLabel.text = ShortenPath(key);
				valueLabel.text = ShortenPath(value);
				flagsLabel.text = flags;

				SetTooltip(keyLabel, key);
				SetTooltip(valueLabel, value);
				SetTooltip(flagsLabel, flags);

				_propertiesList.Add(item);

				_rows[property.Key] = new PropertyRow {
					Container = container,
					ValueLabel = valueLabel,
					FlagsLabel = flagsLabel,
					Group = GroupLabel(propertyGroup),
					Value = value,
					LastUpdatedAt = property.UpdatedAt,
					ChangedAt = epoch,
				};
			}
		}
	}
}
