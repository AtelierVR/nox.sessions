using System;
using Nox.CCK.Scripting;
using Nox.Scripting;
using UnityEngine;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"physics"</c> — the physics environment driven by the current session.
	/// <code>
	/// import physics from 'physics';
	///
	/// physics.simulation = false;      // pause the simulation
	/// physics.gravity = [0, -20, 0];   // change gravity (array, Vector3 or component-wise)
	/// const g = physics.gravity;
	/// </code>
	/// <para>
	/// The values are stored in the session <c>Data</c> container and applied to the engine
	/// (<c>Physics.simulationMode</c> / <c>Physics.gravity</c>) only while this session is the
	/// current one (see <c>ISessionAPI.SetCurrent</c>). Read them through the module namespace to
	/// keep them live.
	/// </para>
	/// </summary>
	public static class PhysicsModule {
		/// <summary>The physical session of the current context, or <c>null</c> outside a session.</summary>
		private static IPhysicalSession Physical(IScriptingContext ctx)
			=> ctx.Session as IPhysicalSession;

		private static bool Simulation(IScriptingContext ctx)
			=> Physical(ctx)?.Simulation ?? true;

		private static Vector3 Gravity(IScriptingContext ctx)
			=> Physical(ctx)?.Gravity ?? Physics.gravity;

		private static Vector3 ToGravity(IScriptingContext ctx, object value)
            => value switch {
                Vector3 v => v,
                object[] arr when arr.Length >= 3 => new Vector3(
                    Convert.ToSingle(arr[0]),
                    Convert.ToSingle(arr[1]),
                    Convert.ToSingle(arr[2])
                ),
                null => Gravity(ctx),
                _ => ctx.FromScript(value, typeof(Vector3)) is Vector3 converted
                    ? converted
                    : Gravity(ctx),
            };

		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("physics")
				.WithTags("session")
				.AddVariable(
					"simulation",
					ctx => (object)Simulation(ctx),
					(ctx, value) => {
						var physical = Physical(ctx);
						if (physical != null && value is bool simulation)
							physical.Simulation = simulation;
					}
				)
				.AddVariable(
					"gravity",
					ctx => (object)Gravity(ctx),
					(ctx, value) => {
						var physical = Physical(ctx);
						if (physical != null)
							physical.Gravity = ToGravity(ctx, value);
					}
				)
				.Build();
	}
}
