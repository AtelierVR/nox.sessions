using System.Linq;
using Nox.CCK;
using Nox.CCK.Scripting;
using Nox.Players;
using Nox.Scripting;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"players"</c> — access to session players.
	/// <code>
	/// import players from 'players';
	///
	/// players.local    // the local player, or null when the session has none yet
	/// players.all      // always an array, empty while no player is registered
	/// players.count
	/// players.at(0)
	/// </code>
	/// <para>
	/// The module is tagged <c>session</c>: it is only bound in a session backend, where
	/// <c>ctx.Session</c> is always set (hence the direct accesses) and the scripts only start once
	/// the session is ready, so <c>all</c> already holds the local player. The values are live: read
	/// them from the namespace, a named import copies the value when the script is loaded.
	/// </para>
	/// </summary>
	public static class PlayersModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("players")
				.WithTags("session")
				.AddVariable("local",  ctx => ctx.Session.LocalPlayer)
				.AddVariable("master", ctx => ctx.Session.MasterPlayer)
				.AddVariable("all",    ctx => ctx.Session.Entities.GetEntities<IPlayer>())
				.AddVariable("count",  ctx => ctx.Session.Entities.GetCount<IPlayer>())
				.AddMethod("at", (ctx, args) => args.Length == 0
					? null
					: ctx.Session.Entities.GetEntities<IPlayer>()
						.ElementAtOrDefault(args[0].ToInt()))
				.Build();
	}
}
