using System;
using System.Linq;
using Nox.CCK.Scripting;
using Nox.Entities;
using Nox.Scripting;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"entities"</c> — every entity of the current session (props, players, ...).
	/// <code>
	/// import entities from 'entities';
	///
	/// entities.all    // every entity, always an array
	/// entities.count
	/// entities.at(0)
	/// entities.get(42)
	/// </code>
	/// <para>
	/// Entities are read from the session entity manager; the values are live, read them from the
	/// namespace (a named import copies the value when the script is loaded).
	/// </para>
	/// </summary>
	public static class EntityModule {
		private static IEntity[] All(IScriptingContext ctx)
			=> ctx.Session.Entities.GetEntities();

		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("entities")
				.WithTags("session")
				.AddVariable("all",   ctx => All(ctx))
				.AddVariable("count", ctx => ctx.Session.Entities.GetCount())
				.AddMethod("at", (ctx, args) => args.Length == 0
					? null
					: All(ctx).ElementAtOrDefault(Convert.ToInt32(args[0])))
				.AddMethod("get", (ctx, args) => args.Length == 0
					? null
					: ctx.Session.Entities.GetEntity(Convert.ToUInt32(args[0])))
				.Build();
	}
}
