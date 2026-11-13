using Nox.CCK.Mods.Cores;
using Nox.Editor.Panel;

namespace Nox.Sessions.Runtime.Editor {
	/// <summary>
	/// Editor-side plumbing shared by the session panels.
	/// <para>
	/// The panel API lives behind <c>Nox.Editor.Panel</c>, an editor-only assembly: this whole
	/// assembly is editor-only (see <c>Nox.Sessions.Editor.asmdef</c>), so the runtime assembly no
	/// longer has to reference it nor guard its own code with <c>#if UNITY_EDITOR</c>.
	/// </para>
	/// </summary>
	internal static class SessionPanels {
		/// <summary>Panel API of the <c>editor.panel</c> mod, or null while it is not loaded.</summary>
		internal static IPanelAPI API
			=> Main.CoreAPI?.ModAPI
				?.GetMod("editor.panel")
				?.GetInstance<IPanelAPI>();
	}
}
