using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Scripting;
using Nox.CCK.Utils;
using Nox.Scripting;
using UnityEngine;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"network"</c> — session network helpers.
	/// <code>
		/// import { time, connected, ping, payloadSize, tickrate, emit } from 'network';
	/// import { crc64 } from 'hashing';
	/// await emit(crc64("myEvent"), buffer);
	/// </code>
	/// </summary>
	public static class NetworkModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("network")
				.WithTags("session")
				.AddVariable("time",        ctx => (ctx.Session as INetSession)?.Time ?? DateTime.UtcNow)
				.AddVariable("connected",   ctx => (object)((ctx.Session as INetSession)?.IsConnected ?? false))
				.AddVariable("ping",        ctx => (object)((ctx.Session as INetSession)?.Ping ?? -1))
				.AddVariable("payloadSize", ctx => (object)((ctx.Session as INetSession)?.EventPayloadSize ?? 0))
				.AddVariable("tickrate",    ctx => {
					var net = ctx.Session as INetSession;
					if (net != null)
						return (object)net.TickRate;
					var fps = Application.targetFrameRate;
					return (object)(fps > 0 ? fps : 60);
				})
				.AddAsyncMethod("emit", async (ctx, args) => {
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
