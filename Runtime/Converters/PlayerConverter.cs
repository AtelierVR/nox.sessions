using Nox.CCK.Scripting;
using Nox.Players;
using Nox.Scripting;
using UnityEngine;

namespace Nox.Sessions.Runtime.Converters {
	/// <summary>
	/// Type converter for <see cref="IPlayer"/> — exposes only the player-specific,
	/// flat subset needed by world scripts.
	/// <para>
	/// The entity-level members shared with every other entity (identity, transform, vitals,
	/// team, ...) are supplied by <see cref="EntityConverter"/> and merged onto the player value;
	/// this converter only adds what is specific to a player (display name, identifier, role and
	/// the teleport/respawn operations).
	/// </para>
	/// </summary>
	public static class PlayerConverter {
		public static readonly IScriptingTypeConverter Player =
			ScriptingTypeConverterBuilder<IPlayer>.Create()
				.AddProperty("display",
					getter: p => p.Display,
					setter: (p, val) => p.Display = val?.ToString() ?? "",
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("identifier",
					getter: p => p.Identifier.ToString())
				.AddProperty("isMaster", p => p.IsMaster)
				.AddProperty("isLocal",  p => p.IsLocal)

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
