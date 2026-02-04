using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.Players;
using Nox.Sessions.Runtime;
using Nox.Terminal;

namespace Nox.Session.Runtime.Commands {
	public class PlayerList : ICommand, IHelper {
		public string GetName()
			=> "players";

		public string GetDescription()
			=> LanguageManager.Get($"terminal.command.{GetName()}.description");

		public string GetShort()
			=> LanguageManager.Get($"terminal.command.{GetName()}.short");

		public string GetUsage()
			=> $"{CommandWithPrefix}";

		private string CommandWithPrefix
			=> $"{Commands.TerminalAPI.GetPrefix()}{GetName()}";

		public string[] AutoComplete(string input, IContext context = null)
			=> CommandWithPrefix.StartsWith(input.ToLower())
				? new[] { CommandWithPrefix }
				: Array.Empty<string>();

		public UniTask<bool> Execute(string input, IContext context = null)
			=> UniTask.FromResult(ExecuteInternal(input, context));

		private bool ExecuteInternal(string input, IContext context = null) {
			if (string.IsNullOrWhiteSpace(input) || context == null)
				return false;

			var parts = input.Trim().Split(' ', 1, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 1 || !parts[0].Equals(CommandWithPrefix, StringComparison.OrdinalIgnoreCase))
				return false;

			var session = Main.Instance.GetCurrentSession();
			if (session == null) {
				context.PrintLn(LanguageManager.Get("terminal.command.players.session_not_found"));
				return true;
			}

			var players = session.Entities.GetEntities<IPlayer>();

			if (players.Length == 0) {
				context.PrintLn(LanguageManager.Get("terminal.command.players.no_players"));
				return true;
			}

			context.PrintLn(
				LanguageManager.Get(
					"terminal.command.players.header",
					players.Length
				)
			);

			for (var i = 0; i < players.Length; i++) {
				var player = players[i];
				if (player == null) continue;
				var pos = player.Position;

				var indicator = new List<string>();
				if (player.IsLocal) indicator.Add(LanguageManager.Get("terminal.command.players.local"));
				if (player.IsMaster) indicator.Add(LanguageManager.Get("terminal.command.players.master"));

				context.PrintLn(
					LanguageManager.Get(
						"terminal.command.players.entry",
						i + 1,
						player.Display,
						pos.x,
						pos.y,
						pos.z,
						string.Join(" ", indicator)
					)
				);
			}

			return true;
		}
	}
}