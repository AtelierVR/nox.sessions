using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Sessions.Clients.Pages;
using Nox.Sessions.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.Sessions.Clients.Components {
	/// <summary>
	/// Component for displaying and managing session information in the UI.
	/// </summary>
	public class SessionComponent : MonoBehaviour {
		#region Fields

		public SessionPage Page;

		// Header elements
		public Image labelIcon;
		public TextLanguage label;
		public GameObject header;
		public Button sessionDropdownButton;
		public TextLanguage sessionDropdownButtonText;

		// Session details
		public TextLanguage title;
		public TextLanguage shortName;
		public Image thumbnail;
		public GameObject withThumbnail;
		public GameObject withoutThumbnail;

		// Content containers
		public RectTransform content;
		public RectTransform navigation;
		public GameObject leftContainer;

		#endregion

		#region Static Factory

		/// <summary>
		/// Creates a new SessionComponent instance with all UI elements.
		/// </summary>
		public static (GameObject, SessionComponent) Create(SessionPage page, RectTransform parent) {
			// Load prefabs
			var iconAsset = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
			var labelAsset = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
			var dropdownButtonAsset = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/header_dropdown.prefab");
			var withTitleAsset = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/with_title.prefab");
			var listAsset = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/list.prefab");
			var scrollAsset = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
			var containerAsset = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/container.prefab");

			// Create main content
			var content = Instantiate(Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/split.prefab"), parent);
			var component = content.AddComponent<SessionComponent>();
			component.Page = page;
			content.name = $"[{page.GetKey()}_{content.GetInstanceID()}]";

			var splitContent = Reference.GetComponent<RectTransform>("content", content);

			// === Left Container (Profile & Navigation) ===
			component.leftContainer = Instantiate(containerAsset, splitContent);
			var profile = Instantiate(
				Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/profile.prefab"),
				Reference.GetComponent<RectTransform>("content", component.leftContainer)
			);

			component.title = Reference.GetComponent<TextLanguage>("title", profile);
			component.shortName = Reference.GetComponent<TextLanguage>("identifier", profile);
			component.thumbnail = Reference.GetComponent<Image>("thumbnail", profile);
			component.withThumbnail = Reference.GetReference("with_thumbnail", profile);
			component.withoutThumbnail = Reference.GetReference("without_thumbnail", profile);

			var navigation = Instantiate(scrollAsset, Reference.GetComponent<RectTransform>("content", profile));
			component.navigation = Reference.GetComponent<RectTransform>(
				"content",
				Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", navigation))
			);

			// === Right Container (Header & Content) ===
			var container = Instantiate(Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/container_full.prefab"), splitContent);
			var withTitle = Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));

			component.header = Reference.GetReference("header", withTitle);

			// Header: Icon
			var icon = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", component.header));
			component.labelIcon = Reference.GetComponent<Image>("image", icon);
			component.labelIcon.sprite = Client.API.AssetAPI.GetAsset<Sprite>("ui:icons/globe.png");

			// Header: Label
			var headerLabel = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", component.header));
			component.label = Reference.GetComponent<TextLanguage>("text", headerLabel);
			component.label.UpdateText("sessions.page.session.title");

			// Header: Dropdown Button
			var dropdownButton = Instantiate(dropdownButtonAsset, Reference.GetComponent<RectTransform>("after", component.header));
			component.sessionDropdownButton = Reference.GetComponent<Button>("button", dropdownButton);
			component.sessionDropdownButtonText = Reference.GetComponent<TextLanguage>("text", dropdownButton);
			component.sessionDropdownButton.onClick.AddListener(component.OnDropdownButtonClick);

			// Content area
			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			var scroll = Instantiate(scrollAsset, contentDash);
			var list = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);

			return (content, component);
		}

		#endregion

		#region UI Updates

		/// <summary>
		/// Refreshes all UI elements.
		/// </summary>
		public void Refresh() {
			UpdateSessionInfo();
			UpdateDropdownButton();
			UpdateThumbnail().Forget();
			UpdateNavigation().Forget();
		}

		/// <summary>
		/// Updates the session title and identifier.
		/// </summary>
		private void UpdateSessionInfo() {
			var session = Page.GetSession();
			if (session == null) {
				title.UpdateText("sessions.no_session");
				return;
			}

			// Update title
			var t = session.GetTitle() ?? session.Id;
			title.UpdateText("value", new[] { t });

			// Update identifier
			var s = session.GetShortName() ?? session.Id;
			shortName.UpdateText("value", new[] { s });
		}

		/// <summary>
		/// Updates the dropdown button text with the current session name.
		/// </summary>
		private void UpdateDropdownButton() {
			var session = Page.GetSession();
			if (session == null) {
				sessionDropdownButtonText.UpdateText("sessions.no_session");
				return;
			}

			var s = session.GetTitle() ?? session.Id;
			sessionDropdownButtonText.UpdateText("value", new[] { s });
		}

		/// <summary>
		/// Updates the session thumbnail.
		/// </summary>
		private async UniTask UpdateThumbnail() {
			var session = Page.GetSession();

			var t = await session.GetThumbnail();
			if (t) {
				thumbnail.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
				withThumbnail.SetActive(true);
				withoutThumbnail.SetActive(false);
				return;
			}

			withThumbnail.SetActive(false);
			withoutThumbnail.SetActive(true);
		}

		/// <summary>
		/// Updates the navigation content.
		/// </summary>
		private async UniTask UpdateNavigation() {
			// Clear existing navigation items
			foreach (Transform child in navigation) {
				Destroy(child.gameObject);
			}

			await UniTask.Yield();
			// TODO: Add navigation items based on session handlers
			// This would be similar to the handlers in api.nox.session
		}

		#endregion

		#region Dropdown Modal

		/// <summary>
		/// Handles the dropdown button click to show the session selection modal.
		/// </summary>
		private void OnDropdownButtonClick() {
			var menu = Page.GetMenu();
			if (menu == null) return;

			var builder = Client.UiAPI.MakeModal(menu);
			if (builder == null) return;

			// Prepare options for the modal
			var sessions = Main.Instance.GetSessions();
			var options = new Dictionary<string, string[]>();

			foreach (var session in sessions) {
				var t = session.GetTitle() ?? session.Id;
				options.Add(session.Id, new[] { "value", t });
			}

			// Build and show the modal
			builder.SetTitle("sessions.page.session.select_title");
			builder.SetClosable(true);
			builder.SetOptions(OnSessionSelected, options);
			builder.SetContent("empty");

			var modal = builder.Build();
			modal.OnClose.AddListener(() => modal.Dispose());
			modal.Show();
		}

		/// <summary>
		/// Callback when a session is selected from the modal.
		/// </summary>
		private void OnSessionSelected(string sessionId) {
			if (Main.Instance.TryGet(sessionId, out var session)) 
				Page.SetSession(session);
		}

		#endregion
	}
}