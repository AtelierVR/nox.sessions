using System;
using Nox.CCK.Scripting;
using Nox.Entities;
using Nox.Scripting;
using UnityEngine;

namespace Nox.Sessions.Runtime.Converters {
	/// <summary>
	/// Type converter for <see cref="IEntity"/> — the shared, safe subset every session entity
	/// exposes (identity, payload access, transform, parts, vitals, team) without letting the backend
	/// walk the session graph.
	/// <para>
	/// It targets the base <see cref="IEntity"/> so it is merged onto any entity (props, players, ...);
	/// the type-specific converters (e.g. <see cref="PlayerConverter"/>) layer their own bindings on top.
	/// The optional members are resolved through the entity interfaces (moving / living / team / parts)
	/// and are null-safe: a missing capability simply reads a default value and ignores the write.
	/// </para>
	/// <para>
	/// The payload is read/written through the entity properties (the same pipeline as any other
	/// entity property), looked up by name: <c>entity.get("label")</c>, <c>entity.set("label", "hi")</c>.
	/// </para>
	/// </summary>
	public static class EntityConverter {
		private static float Float(IScriptingContext ctx, object value)
			=> (float)ctx.FromScript(value, typeof(float));

		private static bool Bool(IScriptingContext ctx, object value)
			=> (bool)ctx.FromScript(value, typeof(bool));

		private static Vector3 Position(IEntity entity)
			=> entity is IMovingEntity moving 
				? moving.Position 
				: Vector3.zero;

		private static Quaternion Rotation(IEntity entity)
			=> entity is IMovingEntity moving 
				? moving.Rotation 
				: Quaternion.identity;

		private static Vector3 Scale(IEntity entity)
			=> entity is IMovingEntity moving 
				? moving.Scale 
				: Vector3.one;

		private static int Parts(IEntity entity)
			=> entity is IMultiPartEntity<IPart> multi ? multi.GetParts().Length : 0;

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

		public static readonly IScriptingTypeConverter Entity =
			ScriptingTypeConverterBuilder<IEntity>.Create()
				// ── IEntity ──
				.AddProperty("id",    e => e.Id)
				.AddProperty("parts", e => Parts(e))

				// ── Payload access (entity properties, the same pipeline as any other entity property) ──
				.AddMethod("get", (e, args) => args.Length > 0 ? FindProperty(e, args[0])?.Value : null)
				.AddMethod("has", (e, args) => args.Length > 0 && FindProperty(e, args[0]) != null)
				.AddMethod("set", (e, args) => {
					if (args.Length >= 2 && FindProperty(e, args[0]) is { } property) {
						property.Value   = args[1];
						property.IsDirty = true;
					}

					return null;
				})
				.AddMethod("delete", (e, args) => {
					if (args.Length > 0 && FindProperty(e, args[0]) is { } property) {
						property.Value   = null;
						property.IsDirty = true;
					}

					return null;
				})

				// ── IMovingEntity / transform ──
				.AddProperty("position", e => Position(e))
				.AddProperty("rotation", e => Rotation(e))
				.AddProperty("scale",    e => Scale(e))
				.AddProperty("velocity", e => e is IMovingEntity vel ? vel.Velocity : Vector3.zero)
				.AddProperty("angular",  e => e is IMovingEntity ang ? ang.Angular : Vector3.zero)
				.AddProperty("walkSpeed",
					getter: (ctx, e) => e is IMovingEntity m ? m.WalkSpeed : 0f,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.WalkSpeed = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("moveAcceleration",
					getter: (ctx, e) => e is IMovingEntity m ? m.MoveAcceleration : 0f,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.MoveAcceleration = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("jumpForce",
					getter: (ctx, e) => e is IMovingEntity m ? m.JumpForce : 0f,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.JumpForce = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("sprintMultiplier",
					getter: (ctx, e) => e is IMovingEntity m ? m.SprintMultiplier : 0f,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.SprintMultiplier = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("airControl",
					getter: (ctx, e) => e is IMovingEntity m ? m.AirControl : 0f,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.AirControl = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("flySpeed",
					getter: (ctx, e) => e is IMovingEntity m ? m.FlySpeed : 0f,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.FlySpeed = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("mayFly",
					getter: (ctx, e) => e is IMovingEntity m && m.MayFly,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.MayFly = Bool(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("immobilized",
					getter: (ctx, e) => e is IMovingEntity m && m.IsImmobilized,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.IsImmobilized = Bool(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("flying",
					getter: (ctx, e) => e is IMovingEntity m && m.IsFlying,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.IsFlying = Bool(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("crouching",
					getter: (ctx, e) => e is IMovingEntity m && m.IsCrouching,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.IsCrouching = Bool(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("sprinting",
					getter: (ctx, e) => e is IMovingEntity m && m.IsSprinting,
					setter: (ctx, e, val) => { if (e is IMovingEntity m) m.IsSprinting = Bool(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)

				// ── ILivingEntity ──
				.AddProperty("health",
					getter: (ctx, e) => e is ILivingEntity l ? l.Health : 0f,
					setter: (ctx, e, val) => { if (e is ILivingEntity l) l.Health = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("maxHealth",
					getter: (ctx, e) => e is ILivingEntity l ? l.MaxHealth : 0f,
					setter: (ctx, e, val) => { if (e is ILivingEntity l) l.MaxHealth = Float(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("invisible",
					getter: (ctx, e) => e is ILivingEntity l && l.IsInvisible,
					setter: (ctx, e, val) => { if (e is ILivingEntity l) l.IsInvisible = Bool(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)

				// ── ITeamEntity ──
				.AddProperty("team",
					getter: (ctx, e) => (e as ITeamEntity)?.Team,
					setter: (ctx, e, val) => { if (e is ITeamEntity t) t.Team = Team(ctx, val); },
					flags: ScriptingTypePropertyFlags.InspectGetter)

				.AddMethod("toString", e => e.ToString())
				.SetDefault((IEntity)null)
				.Build();

		/// <summary>Find a payload property of an entity by its (case-insensitive) name.</summary>
		private static IProperty FindProperty(IEntity entity, object name) {
			if (entity == null || name == null)
				return null;

			var wanted = name.ToString();
			foreach (var property in entity.GetProperties()) {
				if (property.Name != null && string.Equals(property.Name, wanted, StringComparison.OrdinalIgnoreCase))
					return property;
			}

			return null;
		}
	}
}
