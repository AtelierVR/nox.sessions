#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Nox.Avatars.Players;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
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
		public Label        ValueLabel;
		public Label        FlagsLabel;
		public DateTime     LastUpdatedAt;
		public DateTime     ChangedAt;
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
		private VisualElement _propertiesList;
		private VisualElement _propertiesEmpty;

		// Auto-update state
		private IVisualElementScheduledItem _schedule;
		private Dictionary<int, PropertyRow> _rows = new();

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

			_back = root.Q<Button>("back");
			_title = root.Q<Label>("title");
			_playerId = root.Q<Label>("player-id");
			_playerDisplay = root.Q<Label>("player-display");
			_playerLocal = root.Q<Label>("player-local");
			_playerMaster = root.Q<Label>("player-master");
			_avatarId = root.Q<Label>("avatar-id");
			_propertiesList = root.Q<VisualElement>("properties-list");
			_propertiesEmpty = root.Q<VisualElement>("properties-empty");

			_back.RegisterCallback<ClickEvent>(OnBackClicked);

			LoadPlayerDetails();

			// Schedule auto-update every TickMs milliseconds
			_schedule = root.schedule
				.Execute(OnTick)
				.Every(TickMs);

			return _content = root;
		}

		//  Scheduler 

		private void OnTick() {
			if (_player is not IEntity entity) return;

			var properties = entity.GetProperties();

			// If the property count changed, do a full rebuild
			if (properties == null || properties.Length != _rows.Count) {
				LoadProperties();
				return;
			}

			var now = DateTime.UtcNow;

			foreach (var property in properties) {
				if (!_rows.TryGetValue(property.Key, out var row)) continue;

				// Detect change by UpdatedAt
				if (property.UpdatedAt != row.LastUpdatedAt) {
					row.ValueLabel.text = property.Value?.ToString() ?? "null";
					row.FlagsLabel.text = $"Flags: {property.Flags}";
					row.ChangedAt      = now;
					row.LastUpdatedAt  = property.UpdatedAt;
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

			_title.text = $"Player: {_player.Display}";
			_playerId.text = _player.Id.ToString();
			_playerDisplay.text = _player.Display;
			_playerLocal.text = _player.IsLocal ? "Yes" : "No";
			_playerMaster.text = _player.IsMaster ? "Yes" : "No";

			if (_player is IPlayerAvatar playerAvatar) {
				var avatar = playerAvatar.GetAvatar();
				_avatarId.text = avatar?.ToString() ?? "No avatar";
			} else {
				_avatarId.text = "N/A";
			}

			LoadProperties();
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

			foreach (var property in properties) {
				var item       = itemAsset.CloneTree();
				var container  = item.Q<VisualElement>();  // root element of the template
				var valueLabel = item.Q<Label>("value");
				var flagsLabel = item.Q<Label>("flags");

				item.Q<Label>("key").text = property.Name ?? $"Key: {property.Key}";
				valueLabel.text           = property.Value?.ToString() ?? "null";
				flagsLabel.text           = $"Flags: {property.Flags}";

				_propertiesList.Add(item);

				_rows[property.Key] = new PropertyRow {
					Container     = container,
					ValueLabel    = valueLabel,
					FlagsLabel    = flagsLabel,
					LastUpdatedAt = property.UpdatedAt,
					ChangedAt     = epoch,
				};
			}
		}
	}
}
#endif

