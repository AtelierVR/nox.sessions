using System;
using System.Linq;
using Cysharp.Threading.Tasks;
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
	/// await entities.create(type)    // spawn a replicated entity, returns it (or null)
	/// await entities.remove(entity)  // remove one
	/// </code>
	/// <para>
	/// Entities are read from the session entity manager; the values are live, read them from the
	/// namespace (a named import copies the value when the script is loaded). The creation and the
	/// removal go through the session itself.
	/// </para>
	/// </summary>
	public static class EntityModule {
		private static IEntity[] All(IScriptingContext ctx)
			=> ctx.Session.Entities.GetEntities();

		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("entities")
				.WithTags("session")
				.AddVariable("all", ctx => All(ctx))
				.AddVariable("count", ctx => ctx.Session.Entities.GetCount())
				.AddMethod("at", (ctx, args) => args.Length == 0
					? null
					: All(ctx).ElementAtOrDefault(Convert.ToInt32(args[0])))
				.AddMethod("get", (ctx, args) => args.Length == 0
					? null
					: ctx.Session.Entities.GetEntity(Convert.ToUInt32(args[0])))
				.AddAsyncMethod("create", (ctx, args) => Create(ctx, args))
				.AddAsyncMethod("remove", (ctx, args) => Remove(ctx, args))
				.Build();

		/// <summary>Spawn a replicated entity of the given type.</summary>
		private static async UniTask<object> Create(IScriptingContext ctx, object[] args)
			=> args.Length == 0 || ctx.Session is not ISessionSpawner spawner
				? null
				: await spawner.CreateEntity(Convert.ToUInt32(args[0]));

		/// <summary>Remove an entity of the session.</summary>
		private static async UniTask<object> Remove(IScriptingContext ctx, object[] args) {
			var entity = args.Length == 0
				? null
				: args[0] as IEntity
				?? ctx.Session.Entities.GetEntity(Convert.ToUInt32(args[0]));

			return ctx.Session is ISessionSpawner spawner
				&& await spawner.RemoveEntity(entity);
		}
	}
}
