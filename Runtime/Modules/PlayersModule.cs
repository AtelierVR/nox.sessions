using System;
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
	/// players.local    // the local player, or null while the session is not ready
	/// players.all      // always an array, empty while the session is not ready
	/// players.count
	/// players.at(0)
	/// </code>
	/// <para>
	/// The values are live: read them from the namespace (a named import copies the value when the
	/// script is loaded, before any player joins).
	/// </para>
	/// </summary>
	public static class PlayersModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("players")
				.WithTags("session")
				.AddVariable("local",  ctx => ctx.Session?.LocalPlayer)
				.AddVariable("master", ctx => ctx.Session?.MasterPlayer)
				.AddVariable("all",    ctx => ctx.Session?.Entities.GetEntities<IPlayer>() ?? Array.Empty<IPlayer>())
				.AddVariable("count",  ctx => ctx.Session?.Entities.GetCount<IPlayer>() ?? 0)
				.AddMethod("at", (ctx, args) => {
					if (ctx.Session == null || args.Length == 0)
						return null;
					var players = ctx.Session.Entities.GetEntities<IPlayer>();
					return players.ElementAtOrDefault(args[0].ToInt());
				})
				.Build();
	}
}
