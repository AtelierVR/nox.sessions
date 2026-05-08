using Nox.CCK;
using Nox.CCK.Scripting;
using Nox.CCK.Utils;
using Nox.Scripting;
using UnityEngine;
using NoxGizmos = Nox.CCK.Development.Gizmos;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"gizmo"</c> — Unity Gizmos drawing helpers (editor-only).
	/// <code>
	/// import { color, line, wireSphere, wireCube, ray } from 'gizmo';
	/// import { Vector3 } from 'unity';
	///
	/// export function onGizmo() {
	///     color = [1, 0, 0];
	///     wireSphere(Vector3.from(0, 1, 0), 5);
	/// }
	/// </code>
	/// </summary>
	public static class GizmoModule {
		/// <summary>
		/// Whether gizmo drawing is enabled.
		/// Always <c>true</c> in the editor; in builds, reads <c>gizmo.enabled</c> from the user config
		/// (defaults to <c>true</c> when the key is absent).
		/// </summary>
		public static bool IsEnabled {
			get {
#if UNITY_EDITOR
				return true;
#else
				return Config.Load().Get<bool>("gizmo.enabled", true);
#endif
			}
			set {
#if !UNITY_EDITOR
				Config.Load().Set("gizmo.enabled", value);
#endif
			}
		}

		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("gizmo")
				// ── IsEnabled ────────────────────────────────────────────────
				.AddVariable(
					"isEnabled",
					getter: ctx => (object)IsEnabled,
					setter: (ctx, val) => { if (val is bool b) IsEnabled = b; }
				)
				// ── Color ────────────────────────────────────────────────────
				.AddVariable(
					"color",
					getter: ctx => (object)NoxGizmos.color,
					setter: (ctx, val) => {
						if (val is Color c)
							NoxGizmos.color = c;
						else if (val is object[] arr && arr.Length >= 3)
							NoxGizmos.color = new Color(arr[0].ToFloat(), arr[1].ToFloat(), arr[2].ToFloat(),
								arr.Length >= 4 ? arr[3].ToFloat() : 1f);
					}
				)
				// ── Lines & rays ──────────────────────────────────────────
				.AddMethod("line", (ctx, args) => {
					NoxGizmos.DrawLine(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.zero);
					return null;
				})
				.AddMethod("ray", (ctx, args) => {
					NoxGizmos.DrawRay(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.forward);
					return null;
				})
				// ── Spheres ───────────────────────────────────────────────
				.AddMethod("sphere", (ctx, args) => {
					NoxGizmos.DrawSphere(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? args[1].ToFloat() : 1f);
					return null;
				})
				.AddMethod("wireSphere", (ctx, args) => {
					NoxGizmos.DrawWireSphere(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? args[1].ToFloat() : 1f);
					return null;
				})
				// ── Cubes ─────────────────────────────────────────────────
				.AddMethod("cube", (ctx, args) => {
					NoxGizmos.DrawCube(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.one);
					return null;
				})
				.AddMethod("wireCube", (ctx, args) => {
					NoxGizmos.DrawWireCube(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.one);
					return null;
				})
				// ── Capsule (editor-only) ─────────────────────────────────
				.AddMethod("wireCapsule", (ctx, args) => {
					NoxGizmos.DrawWireCapsule(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.up,
						args.Length > 2 ? args[2].ToFloat() : 0.5f);
					return null;
				})
				// ── Disc ─────────────────────────────────────────────────
				.AddMethod("wireDisc", (ctx, args) => {
					NoxGizmos.DrawWireDisc(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.up,
						args.Length > 2 ? args[2].ToFloat() : 1f);
					return null;
				})				.AddMethod("solidDisc", (ctx, args) => {
					NoxGizmos.DrawSolidDisc(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.up,
						args.Length > 2 ? args[2].ToFloat() : 1f);
					return null;
				})
				// ── Arcs ──────────────────────────────────────────────────────────────
				.AddMethod("wireArc", (ctx, args) => {
					NoxGizmos.DrawWireArc(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.up,
						args.Length > 2 ? (Vector3)ctx.FromScript(args[2], typeof(Vector3)) : Vector3.forward,
						args.Length > 3 ? args[3].ToFloat() : 90f,
						args.Length > 4 ? args[4].ToFloat() : 1f);
					return null;
				})
				.AddMethod("solidArc", (ctx, args) => {
					NoxGizmos.DrawSolidArc(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.up,
						args.Length > 2 ? (Vector3)ctx.FromScript(args[2], typeof(Vector3)) : Vector3.forward,
						args.Length > 3 ? args[3].ToFloat() : 90f,
						args.Length > 4 ? args[4].ToFloat() : 1f);
					return null;
				})
				// ── Dotted / Poly lines ───────────────────────────────────────────────
				.AddMethod("dottedLine", (ctx, args) => {
					NoxGizmos.DrawDottedLine(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.zero,
						args.Length > 2 ? args[2].ToFloat() : 4f);
					return null;
				})
				.AddMethod("polyLine", (ctx, args) => {
					var pts = new Vector3[args.Length];
					for (int i = 0; i < args.Length; i++)
						pts[i] = (Vector3)ctx.FromScript(args[i], typeof(Vector3));
					NoxGizmos.DrawPolyLine(pts);
					return null;
				})
				// ── Bezier ────────────────────────────────────────────────────────────
				.AddMethod("bezier", (ctx, args) => {
					NoxGizmos.DrawBezier(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.forward,
						args.Length > 2 ? (Vector3)ctx.FromScript(args[2], typeof(Vector3)) : Vector3.up,
						args.Length > 3 ? (Vector3)ctx.FromScript(args[3], typeof(Vector3)) : Vector3.up,
						args.Length > 4 ? args[4].ToFloat() : 2f);
					return null;
				})
				// ── Arrow ─────────────────────────────────────────────────────────────
				.AddMethod("arrow", (ctx, args) => {
					NoxGizmos.DrawArrow(
						args.Length > 0 ? (Vector3)ctx.FromScript(args[0], typeof(Vector3)) : Vector3.zero,
						args.Length > 1 ? (Vector3)ctx.FromScript(args[1], typeof(Vector3)) : Vector3.forward,
						args.Length > 2 ? args[2].ToFloat() : 1f);
					return null;
				})
				// ── Text / Labels ─────────────────────────────────────────────────────
				.AddMethod("label", (ctx, args) => {
					if (args.Length < 2) return null;
					var pos  = (Vector3)ctx.FromScript(args[0], typeof(Vector3));
					var text = args[1]?.ToString() ?? "";
					if (args.Length >= 5)
						NoxGizmos.DrawLabel(pos, text,
							(int)args[2].ToFloat(),
							(Color)ctx.FromScript(args[3], typeof(Color)));
					else
						NoxGizmos.DrawLabel(pos, text);
					return null;
				})				
                .Build();
	}
}
