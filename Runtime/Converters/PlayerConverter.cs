using Nox.CCK.Scripting;
using Nox.Entities;
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
		private static float Float(IScriptingContext ctx, object value)
			=> (float)ctx.FromScript(value, typeof(float));

		private static bool Bool(IScriptingContext ctx, object value)
			=> (bool)ctx.FromScript(value, typeof(bool));

		/// <summary>
		/// Resolves the team given by a script: an <see cref="ITeam"/> (from the <c>teams</c> module or
		/// another entity) or a team id. <c>null</c>/<c>0</c>/unknown ids clear the team.
		/// </summary>
		private static ITeam Team(IScriptingContext ctx, object value) {
			switch (value) {
				case null:
					return null;
				case ITeam team:
					return team;
			}

			var id = value switch {
				int    i => i,
				double d => (int)d,
				float  f => (int)f,
				string s when int.TryParse(s, out var parsed) => parsed,
				_        => 0
			};

			return id <= 0
				? null
				: (ctx.Session as ITeamSession)?.GetTeam(id);
		}

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

				// ── ILivingEntity ──
				.AddProperty("health",
					getter: (ctx, p) => (object)p.Health,
					setter: (ctx, p, val) => p.Health = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("maxHealth",
					getter: (ctx, p) => (object)p.MaxHealth,
					setter: (ctx, p, val) => p.MaxHealth = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("invisible",
					getter: (ctx, p) => (object)p.IsInvisible,
					setter: (ctx, p, val) => p.IsInvisible = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)

				// ── IMovingEntity ──
				.AddProperty("walkSpeed",
					getter: (ctx, p) => (object)p.WalkSpeed,
					setter: (ctx, p, val) => p.WalkSpeed = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("moveAcceleration",
					getter: (ctx, p) => (object)p.MoveAcceleration,
					setter: (ctx, p, val) => p.MoveAcceleration = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("jumpForce",
					getter: (ctx, p) => (object)p.JumpForce,
					setter: (ctx, p, val) => p.JumpForce = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("sprintMultiplier",
					getter: (ctx, p) => (object)p.SprintMultiplier,
					setter: (ctx, p, val) => p.SprintMultiplier = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("airControl",
					getter: (ctx, p) => (object)p.AirControl,
					setter: (ctx, p, val) => p.AirControl = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("flySpeed",
					getter: (ctx, p) => (object)p.FlySpeed,
					setter: (ctx, p, val) => p.FlySpeed = Float(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("mayFly",
					getter: (ctx, p) => (object)p.MayFly,
					setter: (ctx, p, val) => p.MayFly = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("immobilized",
					getter: (ctx, p) => (object)p.IsImmobilized,
					setter: (ctx, p, val) => p.IsImmobilized = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("flying",
					getter: (ctx, p) => (object)p.IsFlying,
					setter: (ctx, p, val) => p.IsFlying = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("crouching",
					getter: (ctx, p) => (object)p.IsCrouching,
					setter: (ctx, p, val) => p.IsCrouching = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("sprinting",
					getter: (ctx, p) => (object)p.IsSprinting,
					setter: (ctx, p, val) => p.IsSprinting = Bool(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)

				// ── ITeamEntity ──
				.AddProperty("team",
					getter: (ctx, p) => (object)p.Team,
					setter: (ctx, p, val) => p.Team = Team(ctx, val),
					flags: ScriptingTypePropertyFlags.InspectGetter)

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
