using Nox.CCK.Scripting;
using Nox.Entities;
using Nox.Scripting;
using UnityEngine;

namespace Nox.Sessions.Runtime.Converters {
	/// <summary>
	/// Type converter for <see cref="ITeam"/> — exposes the flat subset scripts need
	/// (id, name, colour) without letting the backend walk the session graph.
	/// </summary>
	public static class TeamConverter {
		public static readonly IScriptingTypeConverter Team =
			ScriptingTypeConverterBuilder<ITeam>.Create()
				.AddProperty("id", t => (object)t.Id)
				.AddProperty("name",
					getter: t => (object)t.Name,
					setter: (t, val) => t.Name = val?.ToString() ?? "",
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("color",
					getter: (ctx, t) => (object)t.Color,
					setter: (ctx, t, val) => t.Color = (Color)ctx.FromScript(val, typeof(Color)),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddMethod("toString", t => t.ToString())
				.SetDefault((ITeam)null)
				.Build();
	}
}
