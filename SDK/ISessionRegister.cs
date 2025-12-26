using System.Collections.Generic;
using Nox.CCK.Mods.Initializers;

namespace Nox.Sessions {
	public interface ISessionRegister : IMainModInitializer {
		public bool TryMakeSession(string name, Dictionary<string, object> options, out ISession session);
	}
}