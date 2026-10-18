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
	/// import network from 'network';
	/// import { emit } from 'network';
	/// import { crc64 } from 'hashing';
	///
	/// network.connected      // transport connected
	/// network.reconnecting   // reconnection loop running
	/// network.reconnectAttempt
	/// network.ping           // ms
	/// network.tickrate       // ticks per second
	/// network.payloadSize    // max event payload bytes
	/// network.time           // server time
	/// await emit(crc64("myEvent"), buffer);
	/// </code>
	/// </summary>
	public static class NetworkModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("network")
				.WithTags("session")
				.AddVariable("time",             ctx => (ctx.Session as INetSession)?.Time ?? DateTime.UtcNow)
				.AddVariable("connected",        ctx => (ctx.Session as INetSession)?.IsConnected ?? false)
				.AddVariable("reconnecting",     ctx => (ctx.Session as INetSession)?.IsReconnecting ?? false)
				.AddVariable("reconnectAttempt", ctx => (ctx.Session as INetSession)?.ReconnectAttempt ?? 0)
				.AddVariable("ping",             ctx => (ctx.Session as INetSession)?.Ping ?? -1)
				.AddVariable("payloadSize",      ctx => (ctx.Session as INetSession)?.EventPayloadSize ?? 0)
				.AddVariable("tickrate",         ctx => {
					var net = ctx.Session as INetSession;
					if (net != null)
						return net.TickRate;
					var fps = Application.targetFrameRate;
					return fps > 0 ? fps : 60;
				})
				.AddAsyncMethod("emit", async (ctx, args) => {
					if (ctx.Session is not INetSession net)
						return false;
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
					return await net.EmitEvent(eventId, raw);
				})
				.Build();
	}
}
