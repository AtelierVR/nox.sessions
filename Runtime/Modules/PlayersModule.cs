using System;
using System.Linq;
using Nox.CCK.Scripting;
using Nox.Entities;
using Nox.Players;
using Nox.Scripting;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"players"</c> — access to session players.
	/// <code>
	/// import { getLocal, getMaster, getAll, getCount, getAt } from 'players';
	/// </code>
	/// </summary>
	public static class PlayersModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("players")
				.WithTags("session")
				.AddMethod("getLocal",  (ctx, _) => ctx.Session?.LocalPlayer)
				.AddMethod("getMaster", (ctx, _) => ctx.Session?.MasterPlayer)
				.AddMethod("getAll",    (ctx, _) => ctx.Session?.Entities.GetEntities<IPlayer>())
				.AddMethod("getCount",  (ctx, _) => (object)(ctx.Session?.Entities.GetCount<IPlayer>() ?? 0))
				.AddMethod("getAt", (ctx, args) => {
					if (ctx.Session == null || args.Length == 0)
						return null;
					var players = ctx.Session.Entities.GetEntities<IPlayer>();
					return players.ElementAtOrDefault(Convert.ToInt32(args[0]));
				})
				.Build();
	}
}
