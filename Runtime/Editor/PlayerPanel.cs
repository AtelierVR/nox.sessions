using System;
using System.Collections.Generic;
using System.Linq;
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

		internal EntityInstance Instance;

		public IInstance[] GetInstances()
			=> Instance != null
				? new IInstance[] { Instance }
				: Array.Empty<IInstance>();

		public string GetLabel()
			=> "Session/Entity";

		/// <summary>
		/// Opened from the session details, never from the panel menu: it shows one player given as
		/// data, so it is not a browsable page.
		/// </summary>
		public bool IsVisible()
			=> false;

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data) {
			if (Instance != null)
				throw new InvalidOperationException("PlayerPanel only supports a single instance.");
			return Instance = new EntityInstance(this, window, data);
		}
	}

	public partial class EntityInstance : IInstance {
		private readonly PlayerPanel _panel;
		private readonly IWindow _window;
		private readonly ISession _session;
		private readonly IEntity _entity;

		private VisualElement _content;
		private Button _back;
		private Label _title;

		// Auto-update state
		private IVisualElementScheduledItem _schedule;

		// Fade duration: background alpha goes from 0.5 to 0 over this many seconds
		private const float FadeDuration = 0.5f;
		// Scheduler tick rate in ms
		private const int TickMs = 50;

		public EntityInstance(PlayerPanel panel, IWindow window, Dictionary<string, object> data) {
			_panel = panel;
			_window = window;

			// The page shows an entity: a player is one, with extra sections.
			if (data != null && data.TryGetValue("entity", out var entityObj))
				_entity = entityObj as IEntity;

			if (_entity == null && data != null && data.TryGetValue("player", out var playerObj))
				_entity = playerObj as IEntity;

			if (data != null && data.TryGetValue("session", out var sessionObj))
				_session = sessionObj as ISession;

			_session?.OnPlayerLeft.AddListener(OnPlayerLeft);
			_session?.Entities?.OnEntityRemoved.AddListener(OnEntityRemoved);
			HookPlayer();
			HookOwnership();

			Main.Instance?.OnSessionRemoved.AddListener(OnSessionRemoved);
		}

		public IPanel GetPanel()
			=> _panel;

		/// <summary>The displayed entity seen as a player, or <c>null</c> when it is not one.</summary>
		private IPlayer Player
			=> _entity as IPlayer;

		/// <summary>Ownership capability of the displayed entity, or <c>null</c> when it has none.</summary>
		private IOwnedEntity Owned
			=> _entity as IOwnedEntity;

		/// <summary>Physical capability of the displayed entity, or <c>null</c> when it has none.</summary>
		private IPhysicalEntity PhysicalEntity
			=> _entity as IPhysicalEntity;

		public IWindow GetWindow()
			=> _window;

		public string GetTitle()
			=> Player != null
				? $"Player: {Player.Display}"
				: $"Entity: {_entity?.Id.ToString() ?? "Unknown"}";

		public void OnDestroy() {
			_schedule?.Pause();
			_schedule = null;
			_rows.Clear();

			_session?.OnPlayerLeft.RemoveListener(OnPlayerLeft);
			_session?.Entities?.OnEntityRemoved.RemoveListener(OnEntityRemoved);
			UnhookPlayer();
			UnhookOwnership();

			Main.Instance?.OnSessionRemoved.RemoveListener(OnSessionRemoved);

			_panel.Instance = null;
		}

		/// <summary>The displayed player was unregistered from the entities: back to the session page.</summary>
		private void OnEntityRemoved(IEntity entity) {
			if (Player == null || entity == null || entity.Id != Player.Id)
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

			BindPlayer(root);
			BindOwnership(root);
			BindPhysical(root);
			BindNameplate(root);
			BindAudio(root);
			BindGrabbable(root);
			BindData(root);
			BindParts(root);
			BindProperties(root);

			_back.RegisterCallback<ClickEvent>(OnBackClicked);

			RefreshAll();

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
			if (_entity == null)
				return;

			RefreshAll();
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

		//  Refresh

		/// <summary>Refreshes every section of the page, in display order.</summary>
		private void RefreshAll() {
			RefreshPlayer();
			RefreshOwnership();
			RefreshPhysical();
			RefreshNameplate();
			RefreshAudio();
			RefreshGrabbable();
			RefreshData();
			RefreshParts();
			RefreshProperties();
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

		/// <summary>
		/// Alpha of the "just changed" highlight of a row at a given time: it decays every tick, so it
		/// cannot be expressed as a USS class.
		/// </summary>
		private static float Fade(DateTime changedAt, DateTime now) {
			var elapsed = (float)(now - changedAt).TotalSeconds;
			return Mathf.Max(0f, 0.5f - elapsed / FadeDuration * 0.5f);
		}
	}
}
