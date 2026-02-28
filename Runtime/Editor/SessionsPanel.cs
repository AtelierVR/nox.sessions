#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using Nox.Sessions;
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
		private Button _refresh;

		public SessionsInstance(SessionsPanel panel, IWindow window, Dictionary<string, object> data) {
			_panel = panel;
			_window = window;
			
			// Subscribe to session events for auto-update
			var sessionAPI = _panel.API.ModAPI.GetMod("session")?.GetInstance<ISessionAPI>();
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
			// Unsubscribe from events
			var sessionAPI = _panel.API.ModAPI.GetMod("session")?.GetInstance<ISessionAPI>();
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
			_refresh = root.Q<Button>("refresh");

			_refresh.RegisterCallback<ClickEvent>(OnRefreshClicked);

			RefreshSessions();

			return _content = root;
		}

		private void OnRefreshClicked(ClickEvent evt)
			=> RefreshSessions();

		private void RefreshSessions() {
			_list?.Clear();

			var sessionAPI = _panel.API.ModAPI.GetMod("session")?.GetInstance<ISessionAPI>();
			var sessions = sessionAPI?.GetSessions()?.ToList() ?? new List<ISession>();

			if (sessions.Count == 0) {
				if (_empty != null) _empty.style.display = DisplayStyle.Flex;
				if (_list != null) _list.style.display = DisplayStyle.None;
				return;
			}

			if (_empty != null) _empty.style.display = DisplayStyle.None;
			if (_list != null) _list.style.display = DisplayStyle.Flex;

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/session-item.uxml");

			foreach (var session in sessions) {
				var item = itemAsset.CloneTree();
				
				item.Q<Label>("title").text = $"Session: {session.Id}";
				item.Q<Label>("state").text = $"State: {session.State?.Status.ToString() ?? "Unknown"}";
				
				var netSession = session as INetSession;
				item.Q<Label>("connection").text = netSession?.IsConnected == true ? "Connected" : "Disconnected";

				var viewButton = item.Q<Button>("view");
				viewButton.RegisterCallback<ClickEvent>(_ => OpenDetails(session));

				_list.Add(item);
			}
		}

		private void OpenDetails(ISession session) {
			var data = new Dictionary<string, object> { { "session", session } };
			
			if (!Main.PanelAPI.TryGetPanel(new ResourceIdentifier(null, new[] { "session", "details" }), out var panel)) {
				Logger.LogError("SessionDetailsPanel not found");
				return;
			}

			_window.SetActive(panel, data);
		}
	}
}
#endif