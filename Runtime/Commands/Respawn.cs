using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.Sessions.Runtime;
using Nox.Terminal;

namespace Nox.Session.Runtime.Commands {
	public class Respawn : ICommand, IHelper {
		public string GetName()
			=> "respawn";

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
			var player  = session?.LocalPlayer;
			if (player == null) {
				context.PrintLn(LanguageManager.Get("terminal.command.respawn.player_not_found"));
				return true;
			}

			player.Respawn();
			var pos = player.Position;
			context.PrintLn(
				LanguageManager.Get(
					"terminal.command.respawn.success",
					new object[] {
						player.Display,
						pos.x,
						pos.y,
						pos.z
					}
				)
			);

			return true;
		}
	}
}