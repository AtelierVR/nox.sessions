#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars.Players;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Players;
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
		public DateTime LastUpdatedAt;
		public DateTime ChangedAt;
	}

	/// <summary>
	/// A section of the properties list.
	/// </summary>
	internal readonly struct PropertyGroup {
		public readonly int    Order;
		public readonly string Label;
		public readonly string Description;

		public PropertyGroup(int order, string label, string description) {
			Order       = order;
			Label       = label;
			Description = description;
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
			_panel  = panel;
			_window = window;

			if (data != null) {
				if (data.TryGetValue("session", out var sessionObj))
					_session = sessionObj as ISession;
				if (data.TryGetValue("player", out var playerObj))
					_player = playerObj as IPlayer;
			}
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
			_panel.Instance = null;
		}

		public VisualElement GetContent() {
			if (_content != null)
				return _content;

			var root = _panel.API.AssetAPI
				.GetAsset<VisualTreeAsset>("panels/player.uxml")
				.CloneTree();

			_back            = root.Q<Button>("back");
			_title           = root.Q<Label>("title");
			_playerId        = root.Q<Label>("player-id");
			_playerDisplay   = root.Q<Label>("player-display");
			_playerLocal     = root.Q<Label>("player-local");
			_playerMaster    = root.Q<Label>("player-master");
			_avatarId        = root.Q<Label>("avatar-id");
			_teamColor       = root.Q<VisualElement>("team-color");
			_teamName        = root.Q<Label>("team-name");
			_teamId          = root.Q<Label>("team-id");
			_health          = root.Q<Label>("health");
			_maxHealth       = root.Q<Label>("max-health");
			_invisible       = root.Q<Label>("invisible");
			_walkSpeed       = root.Q<Label>("walk-speed");
			_moveAcceleration = root.Q<Label>("move-acceleration");
			_jumpForce       = root.Q<Label>("jump-force");
			_sprintMultiplier = root.Q<Label>("sprint-multiplier");
			_airControl      = root.Q<Label>("air-control");
			_flySpeed        = root.Q<Label>("fly-speed");
			_mayFly          = root.Q<Label>("may-fly");
			_immobilized     = root.Q<Label>("immobilized");
			_flying          = root.Q<Label>("flying");
			_crouching       = root.Q<Label>("crouching");
			_sprinting       = root.Q<Label>("sprinting");
			_nameplateVisible = root.Q<Label>("nameplate-visible");
			_healthbarVisible = root.Q<Label>("healthbar-visible");
			_propertiesList  = root.Q<VisualElement>("properties-list");
			_propertiesEmpty = root.Q<VisualElement>("properties-empty");
			_partsList       = root.Q<VisualElement>("parts-list");
			_partsEmpty      = root.Q<VisualElement>("parts-empty");

			_back.RegisterCallback<ClickEvent>(OnBackClicked);

			LoadPlayerDetails();

			// Schedule auto-update every TickMs milliseconds
			_schedule = root.schedule
				.Execute(OnTick)
				.Every(TickMs);

			return _content = root;
		}

		//  Scheduler 

		private static string FormatVec3(Vector3 v)    => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
		private static string FormatEuler(Quaternion q) { var e = q.eulerAngles; return $"({e.x:F1}\u00b0, {e.y:F1}\u00b0, {e.z:F1}\u00b0)"; }

		private static string FormatValue(object v) {
			if (v == null)         return "null";
			if (v is not byte[] b) return v.ToString();
			var count   = Math.Min(b.Length, 10);
			var hex     = BitConverter.ToString(b, 0, count).Replace('-', ' ');
			var ellipsis = b.Length > 10 ? "..." : "";
			return $"Buffer ({b.Length}) {hex}{ellipsis}";
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
						if (part.Updated != partRow.LastUpdated) {
							partRow.PosLabel.text = FormatVec3(part.Position);
							partRow.RotLabel.text = FormatEuler(part.Rotation);
							partRow.ChangedAt     = now2;
							partRow.LastUpdated   = part.Updated;
							_partRows[part.Id]    = partRow;
						}
						var elapsed2 = (float)(now2 - partRow.ChangedAt).TotalSeconds;
						var alpha2   = Mathf.Max(0f, 0.5f - elapsed2 / FadeDuration * 0.5f);
						partRow.Container.style.backgroundColor = new Color(0.2f, 0.6f, 1f, alpha2);
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
				if (_rows.TryGetValue(property.Key, out var existing) && existing.Group != GetPropertyGroup(property).Label) {
					LoadProperties();
					return;
				}

			var now = DateTime.UtcNow;

			foreach (var property in properties) {
				if (!_rows.TryGetValue(property.Key, out var row))
					continue;

				// Detect change by UpdatedAt
				if (property.UpdatedAt != row.LastUpdatedAt) {
					row.ValueLabel.text = FormatValue(property.Value);
					row.FlagsLabel.text = property.Flags.ToString();
					row.ValueLabel.tooltip = row.ValueLabel.text;
					row.FlagsLabel.tooltip = row.FlagsLabel.text;
					row.ChangedAt       = now;
					row.LastUpdatedAt   = property.UpdatedAt;
					_rows[property.Key] = row;
				}

				// Fade background: alpha from 0.5 to 0 over FadeDuration seconds
				var elapsed = (float)(now - row.ChangedAt).TotalSeconds;
				var alpha   = Mathf.Max(0f, 0.5f - elapsed / FadeDuration * 0.5f);
				row.Container.style.backgroundColor = new Color(0.2f, 0.6f, 1f, alpha);
			}
		}

		//  Navigation 

		private void OnBackClicked(ClickEvent evt) {
			if (_session == null) {
				Logger.LogWarning("No session to go back to");
				return;
			}

			var data = new Dictionary<string, object> { { "session", _session } };

			if (!Main.PanelAPI.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "details" }), out var panel)) {
				Logger.LogError("SessionDetailsPanel not found");
				return;
			}

			_window.SetActive(panel, data);
		}

		//  Display 

		private void LoadPlayerDetails() {
			if (_player == null) {
				Logger.LogWarning("No player to display");
				return;
			}

			_title.text         = $"Player: {_player.Display}";
			_playerId.text      = _player.Id.ToString();
			_playerDisplay.text = _player.Display;
			_playerLocal.text   = _player.IsLocal ? "Yes" : "No";
			_playerMaster.text  = _player.IsMaster ? "Yes" : "No";

			if (_player is IPlayerAvatar playerAvatar) {
				var avatar = playerAvatar.GetAvatar();
				_avatarId.text = avatar.ToString();
			} else
				_avatarId.text = "N/A";

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
			Set(_teamId,   team != null ? team.Id.ToString() : "-");

			// The team name carries the colour of the team (the swatch next to it as well).
			var teamColorId = team?.Id ?? -1;
			if (teamColorId != _teamColorId) {
				_teamColorId = teamColorId;

				if (_teamColor != null) {
					_teamColor.style.display = team != null ? DisplayStyle.Flex : DisplayStyle.None;
					if (team != null)
						_teamColor.style.backgroundColor = team.Color;
				}

				if (_teamName != null)
					_teamName.style.color = team != null ? team.Color : StyleKeyword.Null;
			}

			Set(_health,    FormatFloat(player.Health));
			Set(_maxHealth, FormatFloat(player.MaxHealth));
			Set(_invisible, FormatBool(player.IsInvisible));

			Set(_walkSpeed,        FormatFloat(player.WalkSpeed));
			Set(_moveAcceleration, FormatFloat(player.MoveAcceleration));
			Set(_jumpForce,        FormatFloat(player.JumpForce));
			Set(_sprintMultiplier, FormatFloat(player.SprintMultiplier));
			Set(_airControl,       FormatFloat(player.AirControl));
			Set(_flySpeed,         FormatFloat(player.FlySpeed));
			Set(_mayFly,           FormatBool(player.MayFly));
			Set(_immobilized,      FormatBool(player.IsImmobilized));
			Set(_flying,           FormatBool(player.IsFlying));
			Set(_crouching,        FormatBool(player.IsCrouching));
			Set(_sprinting,        FormatBool(player.IsSprinting));

			Set(_nameplateVisible, FormatFlag(ReadFlag(player, "NameplateVisible")));
			Set(_healthbarVisible, FormatFlag(ReadFlag(player, "HealthbarVisible")));
		}

		private static void Set(Label label, string value) {
			if (label != null && label.text != value)
				label.text = value;
		}

		private static string FormatFloat(float value) => value.ToString("0.###");
		private static string FormatBool(bool value)   => value ? "Yes" : "No";

		private static string FormatFlag(bool? value)
			=> value switch {
				true  => "Yes",
				false => "No",
				_     => "N/A",
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
				if (_partsEmpty != null) _partsEmpty.style.display = DisplayStyle.Flex;
				if (_partsList  != null) _partsList.style.display  = DisplayStyle.None;
				return;
			}

			if (_partsEmpty != null) _partsEmpty.style.display = DisplayStyle.None;
			if (_partsList  != null) _partsList.style.display  = DisplayStyle.Flex;

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/part-item.uxml");
			var epoch     = DateTime.MinValue;
			foreach (var part in parts) {
				var item      = itemAsset.CloneTree();
				var container = item.Q<VisualElement>();
				var posLabel  = item.Q<Label>("position");
				var rotLabel  = item.Q<Label>("rotation");

				var nameLabel = item.Q<Label>("name");
				nameLabel.text = part.Id.ToPlayerRig().ToString();
				nameLabel.tooltip = nameLabel.text;
				posLabel.text = FormatVec3(part.Position);
				rotLabel.text = FormatEuler(part.Rotation);

				_partsList.Add(item);
				_partRows[part.Id] = new PartRow {
					Container   = container,
					PosLabel    = posLabel,
					RotLabel    = rotLabel,
					LastUpdated = part.Updated,
					ChangedAt   = epoch,
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
					return new PropertyGroup(0, "Avatar Parameters",
						"Bound to a live avatar parameter: read once per tick and sent according to its sync flags.");
				case "UnassignedProperty":
					return new PropertyGroup(1, "Unassigned (key never declared)",
						"A value arrived for a key this entity has no property for, so Nox.Relay stored it as-is. " +
						"If this section keeps growing with new keys, the other client is sending keys we never registered.");
				case "Property":
					return new PropertyGroup(2, "Raw",
						"Plain key/value property declared by the entity itself.");
				default:
					return new PropertyGroup(3, property.GetType().Name,
						"Custom IProperty implementation.");
			}
		}

		private static VisualElement CreateGroupHeader(PropertyGroup group, int count) {
			var header = new Label($"{group.Label}  ({count})");
			header.tooltip = group.Description;

			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.color                   = new Color(0.75f, 0.85f, 1f);
			header.style.marginTop               = 10;
			header.style.marginBottom            = 2;
			header.style.marginLeft              = 2;
			header.style.paddingLeft             = 6;
			header.style.borderLeftWidth         = 2;
			header.style.borderLeftColor         = new Color(0.35f, 0.55f, 0.9f);

			return header;
		}

		private void LoadProperties() {
			_propertiesList?.Clear();
			_rows.Clear();

			if (_player is not IEntity entity) {
				_propertiesEmpty.style.display = DisplayStyle.Flex;
				_propertiesList.style.display  = DisplayStyle.None;
				return;
			}

			var properties = entity.GetProperties();

			if (properties == null || properties.Length == 0) {
				_propertiesEmpty.style.display = DisplayStyle.Flex;
				_propertiesList.style.display  = DisplayStyle.None;
				return;
			}

			_propertiesEmpty.style.display = DisplayStyle.None;
			_propertiesList.style.display  = DisplayStyle.Flex;

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/property-item.uxml");
			var epoch     = DateTime.MinValue;

			// Group by concrete implementation instead of listing everything flat: that is what tells a
			// parameter bound to the avatar apart from an incoming key this entity never declared.
			var groups = properties
				.Select(p => (Property: p, Group: GetPropertyGroup(p)))
				.GroupBy(x => x.Group.Label)
				.OrderBy(g => g.Min(x => x.Group.Order))
				.ThenBy(g => g.Key, StringComparer.Ordinal);

			foreach (var group in groups) {
				_propertiesList.Add(CreateGroupHeader(group.First().Group, group.Count()));

				// Named properties first (alphabetical), then the raw keys ascending — so an
				// unexplained key is easy to pick out and stays in the same place between rebuilds.
				var ordered = group
					.OrderBy(x => x.Property.Name == null ? 1 : 0)
					.ThenBy(x => x.Property.Name ?? string.Empty, StringComparer.Ordinal)
					.ThenBy(x => x.Property.Key);

				foreach (var (property, propertyGroup) in ordered) {
					var item       = itemAsset.CloneTree();
					var container  = item.Q<VisualElement>(); // root element of the template
					var valueLabel = item.Q<Label>("value");
					var flagsLabel = item.Q<Label>("flags");

					var keyLabel = item.Q<Label>("key");
					keyLabel.text = property.Name ?? $"Key: {property.Key}";
					valueLabel.text = FormatValue(property.Value);
					flagsLabel.text = property.Flags.ToString();

					// The row is ellipsized to fit the panel: the tooltip keeps the full content readable.
					keyLabel.tooltip   = keyLabel.text;
					valueLabel.tooltip = valueLabel.text;
					flagsLabel.tooltip = flagsLabel.text;

					_propertiesList.Add(item);

					_rows[property.Key] = new PropertyRow {
						Container     = container,
						ValueLabel    = valueLabel,
						FlagsLabel    = flagsLabel,
						Group         = propertyGroup.Label,
						LastUpdatedAt = property.UpdatedAt,
						ChangedAt     = epoch,
					};
				}
			}
		}
	}
}
#endif