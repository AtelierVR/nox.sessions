using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.Sessions.Clients.Components;
using Nox.Sessions.Runtime;
using Nox.UI;
using UnityEngine;

namespace Nox.Sessions.Clients.Pages {
	/// <summary>
	/// Main session management page.
	/// Displays session information and allows navigation between different sessions.
	/// </summary>
	public class SessionPage : IPage {
		#region Static Methods

		/// <summary>
		/// Gets the static key identifier for this page.
		/// </summary>
		public static string GetStaticKey() => "session";

		/// <summary>
		/// Creates a new SessionPage instance.
		/// </summary>
		public static IPage Create(IMenu menu, object[] context) {
			var sessionId = TryGet(context, 0, out string id) ? id : null;
			return new SessionPage {
				_menuId = menu.GetId(),
				_context = context,
				_crtId = sessionId
			};
		}

		private static bool TryGet<T>(object[] array, int index, out T value) {
			if (array.Length > index && array[index] is T t) {
				value = t;
				return true;
			}
			value = default;
			return false;
		}

		#endregion

		#region Fields

		private int _menuId;
		private object[] _context;
		private string _crtId;
		private GameObject _content;
		private SessionComponent _component;

		#endregion

		#region IPage Implementation

		public string GetKey() => GetStaticKey();

		public object[] GetContext() => _context;

		public IMenu GetMenu() => Client.UiAPI?.Get<IMenu>(_menuId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = SessionComponent.Create(this, parent);
			return _content;
		}

		public void OnDisplay(IPage lastPage) => Refresh();

		public void OnOpen(IPage lastPage) {
			SubscribeToEvents();
			Refresh();
		}

		public void OnRemove() {
			UnsubscribeFromEvents();
		}

		public void OnRefresh() => Refresh();

		#endregion

		#region Session Management

		/// <summary>
		/// Gets the currently selected session.
		/// </summary>
		public ISession GetSession() {
			if (!string.IsNullOrEmpty(_crtId) && Main.Instance.TryGet(_crtId, out var session))
				return session;
			return SessionUtils.Current ?? Main.Instance.GetSessions().FirstOrDefault();
		}

		/// <summary>
		/// Sets the currently selected session.
		/// </summary>
		public void SetSession(ISession session) {
			_crtId = session?.Id;
			Refresh();
		}

		#endregion

		#region Event Management

		private void SubscribeToEvents() {
			Main.Instance.OnSessionAdded.AddListener(OnSessionChanged);
			Main.Instance.OnSessionRemoved.AddListener(OnSessionChanged);
			Main.Instance.OnCurrentChanged.AddListener(OnCurrentChanged);
		}

		private void UnsubscribeFromEvents() {
			Main.Instance.OnSessionAdded.RemoveListener(OnSessionChanged);
			Main.Instance.OnSessionRemoved.RemoveListener(OnSessionChanged);
			Main.Instance.OnCurrentChanged.RemoveListener(OnCurrentChanged);
		}

		private void OnSessionChanged(ISession session)
			=> Refresh();

		private void OnCurrentChanged(ISession newSession, ISession oldSession)
			=> Refresh();

		#endregion

		#region Refresh

		private void Refresh() {
			if (!_component) return;
			_component.Refresh();
		}

		#endregion
	}
}