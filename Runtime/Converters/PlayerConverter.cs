using Nox.CCK.Scripting;
using Nox.Players;
using Nox.Scripting;
using UnityEngine;

namespace Nox.Sessions.Runtime.Converters {
	/// <summary>
	/// Type converter for <see cref="IPlayer"/> — exposes only the safe, flat subset
	/// needed by world scripts. Prevents Jint from traversing the full entity graph
	/// (GetProperties, GetParts, physical components) via raw reflection.
	/// </summary>
	public static class PlayerConverter {
		public static readonly IScriptingTypeConverter Player =
			ScriptingTypeConverterBuilder<IPlayer>.Create()
				.AddProperty("display",
					getter: p => (object)p.Display,
					setter: (p, val) => p.Display = val?.ToString() ?? "",
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("identifier",
					getter: p => (object)p.Identifier.ToString())
				.AddProperty("isMaster", p => (object)p.IsMaster)
				.AddProperty("isLocal",  p => (object)p.IsLocal)
				.AddMethod("teleport", (ctx, p, args) => {
					if (args.Length < 2)
						return null;
					var pos = (Vector3)ctx.FromScript(args[0], typeof(Vector3));
					var rot = (Quaternion)ctx.FromScript(args[1], typeof(Quaternion));
					p.Teleport(pos, rot);
					return null;
				})
				.AddMethod("respawn", p => p.Respawn())
				.SetDefault((IPlayer)null)
				.Build();
	}
}
