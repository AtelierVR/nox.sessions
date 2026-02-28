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

		public void OnDestroy()
			=> _panel.Instance = null;

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

			return _content = root;
		}

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

			if (_player is not IEntity entity) {
				_propertiesEmpty.style.display = DisplayStyle.Flex;
				_propertiesList.style.display = DisplayStyle.None;
				return;
			}

			var properties = entity.GetProperties();

			if (properties == null || properties.Length == 0) {
				_propertiesEmpty.style.display = DisplayStyle.Flex;
				_propertiesList.style.display = DisplayStyle.None;
				return;
			}

			_propertiesEmpty.style.display = DisplayStyle.None;
			_propertiesList.style.display = DisplayStyle.Flex;

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/property-item.uxml");

			foreach (var property in properties) {
				var item = itemAsset.CloneTree();
				
				item.Q<Label>("key").text = property.Name ?? $"Key: {property.Key}";
				item.Q<Label>("value").text = property.Value?.ToString() ?? "null";
				item.Q<Label>("flags").text = $"Flags: {property.Flags}";

				_propertiesList.Add(item);
			}
		}
	}
}
#endif