using Nox.CCK.Utils;

namespace Nox.CCK.Sessions {
	public static class Settings {
		public const float RenderEntityDistanceDefault = 100f;
		public const int ClearPhysicalAfterSecondsDefault = 15;

		public const string RenderEntityDistanceKey = "settings.sessions.render_entity";
		public const string ClearPhysicalAfterSecondsKey = "settings.sessions.clear_physical";

		public static float RenderEntityDistance {
			get => Config.Load().Get(RenderEntityDistanceKey, RenderEntityDistanceDefault);
			set {
				var config = Config.Load();
				config.Set(RenderEntityDistanceKey, value);
				config.Save();
			}
		}

		public static int ClearPhysicalAfterSeconds {
			get => Config.Load().Get(ClearPhysicalAfterSecondsKey, ClearPhysicalAfterSecondsDefault);
			set {
				var config = Config.Load();
				config.Set(ClearPhysicalAfterSecondsKey, value);
				config.Save();
			}
		}
	}
}