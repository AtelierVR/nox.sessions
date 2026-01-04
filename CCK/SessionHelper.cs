using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Properties;
using Nox.Instances;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;

namespace Nox.CCK.Sessions {
	public static class SessionHelper {
		private const string DISPOSE_ON_CHANGE = "dispose_change";
		private const string TITLE = "title";
		private const string SHORT_NAME = "short_name";
		private const string THUMBNAIL = "thumbnail";
		private const string INSTANCE = "instance";
		private const string WORLD = "world";

		public static bool IsDisposeOnChange(this ISession session)
			=> session.TryGetProperty<bool>(PropertyHelper.StringToKey(DISPOSE_ON_CHANGE), out var value) && value;

		public static void SetDisposeOnChange(this ISession session, bool value) {
			if (session is not IEditablePropertyObject epo) return;
			epo.SetProperty(PropertyHelper.StringToKey(DISPOSE_ON_CHANGE), value);
		}

		public static string GetTitle(this ISession session) {
			if (!session.TryGetProperty<object>(PropertyHelper.StringToKey(TITLE), out var value))
				return string.Empty;
			return value switch {
				string str        => str,
				Func<string> func => func(),
				_                 => value?.ToString()
			};
		}

		public static void SetTitle(this ISession session, string title) {
			if (session is not IEditablePropertyObject epo) return;
			epo.SetProperty(PropertyHelper.StringToKey(TITLE), title);
		}

		public static async UniTask<Texture2D> GetThumbnail(this ISession session) {
			if (!session.TryGetProperty<object>(PropertyHelper.StringToKey(THUMBNAIL), out var value))
				return null;
			return value switch {
				Texture2D tex                 => tex,
				Func<UniTask<Texture2D>> func => await func(),
				UniTask<Texture2D> task       => await task,
				_                             => null
			};
		}

		public static void SetThumbnail(this ISession session, Texture2D thumbnail) {
			if (session is not IEditablePropertyObject epo) return;
			epo.SetProperty(PropertyHelper.StringToKey(THUMBNAIL), thumbnail);
		}

		public static bool IsCurrent(ISessionAPI api, ISession session)
			=> api.Current == session.Id;

		public static bool Match(this ISession session, IWorldIdentifier world)
			=> session.Dimensions.Identifier?.Equals(world) ?? false;

		public static IInstanceIdentifier GetInstance(this ISession session) {
			if (!session.TryGetProperty<object>(PropertyHelper.StringToKey(INSTANCE), out var value))
				return null;
			return value switch {
				IInstanceIdentifier identifier => identifier,
				Func<IInstanceIdentifier> func => func(),
				_                              => null
			};
		}

		public static void SetInstance(this ISession session, IInstanceIdentifier instance) {
			if (session is not IEditablePropertyObject epo) return;
			epo.SetProperty(PropertyHelper.StringToKey(INSTANCE), instance);
		}

		public static IWorldIdentifier GetWorld(this ISession session) {
			if (!session.TryGetProperty<object>(PropertyHelper.StringToKey(WORLD), out var value))
				return null;
			return value switch {
				IWorldIdentifier identifier => identifier,
				Func<IWorldIdentifier> func => func(),
				_                           => null
			};
		}

		public static void SetWorld(this ISession session, IWorldIdentifier world) {
			if (session is not IEditablePropertyObject epo) return;
			epo.SetProperty(PropertyHelper.StringToKey(WORLD), world);
		}

		public static string GetShortName(this ISession session) {
			if (!session.TryGetProperty<object>(PropertyHelper.StringToKey(SHORT_NAME), out var value))
				return null;
			return value switch {
				string str        => str,
				Func<string> func => func(),
				_                 => value?.ToString()
			};
		}

		public static void SetShortName(this ISession session, string shortName) {
			if (session is not IEditablePropertyObject epo) return;
			epo.SetProperty(PropertyHelper.StringToKey(SHORT_NAME), shortName);
		}

		public static bool IsFinished(this IState state)
			=> state.IsReady() || state.IsError();

		public static bool IsReady(this IState state)
			=> state.Status == Status.Ready;

		public static bool IsError(this IState state)
			=> state.Status == Status.Error;

		public static UniTask<bool> WhenFinished(this ISession session) {
			if (session.State.IsFinished())
				return UniTask.FromResult(session.State.IsReady());

			var tcs = new UniTaskCompletionSource<bool>();

			session.OnStateChanged.AddListener(Handler);
			Handler(session.State);
			return tcs.Task;

			void Handler(IState state) {
				if (!state.IsFinished()) return;
				session.OnStateChanged.RemoveListener(Handler);
				tcs.TrySetResult(state.IsReady());
			}
		}
	}
}