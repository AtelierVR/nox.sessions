using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Scripting;
using Nox.CCK.Utils;
using Nox.Scripting;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"network"</c> — session network helpers.
	/// <code>
	/// import { getTime, isConnected, eventToHash, emitEvent } from 'network';
	/// await emitEvent("myEvent", buffer);
	/// </code>
	/// </summary>
	public static class NetworkModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("network")
				.WithTags("session")
				.AddMethod("getTime",     (ctx, _) => (ctx.Session as INetSession)?.Time ?? DateTime.UtcNow)
				.AddMethod("isConnected", (ctx, _) => (object)((ctx.Session as INetSession)?.IsConnected ?? false))
				.AddMethod("eventToHash", (_, args) =>
					args.Length > 0 ? (object)Hash.CRC64(args[0]?.ToString() ?? "") : null)
				.AddAsyncMethod("emitEvent", async (ctx, args) => {
					if (ctx.Session is not INetSession net)
						return (object)false;
					var eventId = args.Length > 0 && args[0] is double d
						? (long)d
						: Hash.CRC64(args[0]?.ToString() ?? "");
					byte[] raw;
					if (args.Length < 2 || args[1] == null)
						raw = Array.Empty<byte>();
					else
						raw = args[1] switch {
							byte[] b     => b,
							object[] arr => arr.Select(x => Convert.ToByte(x)).ToArray(),
							_            => Array.Empty<byte>()
						};
					return (object)await net.EmitEvent(eventId, raw);
				})
				.Build();
	}
}
