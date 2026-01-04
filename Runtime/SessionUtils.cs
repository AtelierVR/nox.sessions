using Cysharp.Threading.Tasks;
namespace Nox.Sessions.Runtime {
	public static class SessionUtils {
		public static void Respawn() {
			if (!Main.Instance.TryGet(Main.Instance.Current, out var session)) return;
			session.LocalPlayer.Respawn();
		}

		public static ISession Current {
			get => Main.Instance.TryGet(Main.Instance.Current, out var session) ? session : null;
			set => Main.Instance.SetCurrent(value?.Id).Forget();
		}
	}
}