using System;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.Sessions.Clients.Pages;
using Nox.Sessions.Clients.Widgets;
using Nox.UI;
using UnityEngine;

namespace Nox.Sessions.Clients {
	/// <summary>
	/// Client initializer for the Sessions module.
	/// Handles UI interactions, widgets, and session-related events.
	/// </summary>
	public class Client : IClientModInitializer {
		#region Singleton & API Access

		/// <summary>
		/// Singleton instance of the Client.
		/// </summary>
		static internal Client Instance { get; private set; }

		/// <summary>
		/// Core API access for the client mod.
		/// </summary>
		static internal IClientModCoreAPI API { get; private set; }

		/// <summary>
		/// UI API access helper.
		/// </summary>
		internal static IUiAPI UiAPI
			=> API?.ModAPI
				?.GetMod("ui")
				?.GetInstance<IUiAPI>();

		#endregion

		#region Lifecycle

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		/// <summary>
		/// Initializes the client mod and subscribes to relevant events.
		/// </summary>
		public void OnInitializeClient(IClientModCoreAPI api) {
			Instance = this;
			API = api;

			SubscribeToEvents();
		}

		/// <summary>
		/// Cleans up resources and unsubscribes from events.
		/// </summary>
		public void OnDisposeClient() {
			UnsubscribeFromEvents();

			API = null;
			Instance = null;
		}

		#endregion

		#region Event Management

		/// <summary>
		/// Subscribes to all necessary events.
		/// </summary>
		private void SubscribeToEvents() {
			_events = new[] {
				API.EventAPI.Subscribe("menu_goto", OnGoto),
				API.EventAPI.Subscribe("session_handlers_request", OnHandlerRequest),
				API.EventAPI.Subscribe("widget_request", OnWidgetRequest),
				API.EventAPI.Subscribe("respawn", RespawnWidget.OnRespawn)
			};
		}

		/// <summary>
		/// Unsubscribes from all events.
		/// </summary>
		private void UnsubscribeFromEvents() {
			foreach (var eventSubscription in _events) {
				API?.EventAPI?.Unsubscribe(eventSubscription);
			}
			_events = Array.Empty<EventSubscription>();
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Handles navigation to different menu pages.
		/// </summary>
		private static void OnGoto(EventData context) {
			if (!context.TryGet(0, out int menuId)) return;
			if (!context.TryGet(1, out string pageKey)) return;

			var menu = UiAPI?.Get<IMenu>(menuId);
			if (menu == null) return;

			IPage page = null;
			if (pageKey == SessionPage.GetStaticKey()) {
				page = SessionPage.Create(menu, context.Data[2..]);
			}

			if (page != null) {
				API.EventAPI.Emit("menu_display", menuId, page);
			}
		}

		/// <summary>
		/// Handles session handler requests.
		/// </summary>
		private static void OnHandlerRequest(EventData context) {
			// TODO: Implement handler request logic
			// Example: context.Callback(handlerId, displayName, icon);
		}

		/// <summary>
		/// Handles widget creation requests.
		/// </summary>
		private static void OnWidgetRequest(EventData context) {
			if (!context.TryGet(0, out int menuId)) return;
			if (!context.TryGet(1, out RectTransform parent)) return;

			var menu = UiAPI?.Get<IMenu>(menuId);
			if (menu == null) return;

			// Create and register widgets
			if (RespawnWidget.TryMake(menu, parent, out var widget)) {
				context.Callback(widget.Item2, widget.Item1);
			}
		}

		#endregion
	}
}