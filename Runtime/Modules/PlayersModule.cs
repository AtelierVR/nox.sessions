using System;
using System.Linq;
using Nox.CCK;
using Nox.CCK.Scripting;
using Nox.Entities;
using Nox.Players;
using Nox.Scripting;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"players"</c> — access to session players.
	/// <code>
	/// import { local, master, all, count, at } from 'players';
	/// </code>
	/// </summary>
	public static class PlayersModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("players")
				.WithTags("session")
				.AddVariable("local",  ctx => ctx.Session?.LocalPlayer)
				.AddVariable("master", ctx => ctx.Session?.MasterPlayer)
				.AddVariable("all",    ctx => ctx.Session?.Entities.GetEntities<IPlayer>())
				.AddVariable("count",  ctx => (object)(ctx.Session?.Entities.GetCount<IPlayer>() ?? 0))
				.AddMethod("at", (ctx, args) => {
					if (ctx.Session == null || args.Length == 0)
						return null;
					var players = ctx.Session.Entities.GetEntities<IPlayer>();
					return players.ElementAtOrDefault(args[0].ToInt());
				})
				.Build();
	}
}
