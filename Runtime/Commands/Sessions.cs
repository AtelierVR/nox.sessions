using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Sessions;
using Nox.Sessions.Runtime;
using Nox.Terminal;

namespace Nox.Session.Runtime.Commands {
	public class Sessions : ICommand, IHelper {
		public string GetName()
			=> "sessions";

		public string GetDescription()
			=> LanguageManager.Get($"terminal.command.{GetName()}.description");

		public string GetShort()
			=> LanguageManager.Get($"terminal.command.{GetName()}.short");

		public string GetUsage()
			=> $"{CommandWithPrefix} [list|change <id>|leave [id]]";

		private string CommandWithPrefix
			=> $"{Commands.TerminalAPI.GetPrefix()}{GetName()}";

		public string[] AutoComplete(string input, IContext context = null) {
			var inputLower = input.ToLower();
			
			if (CommandWithPrefix.StartsWith(inputLower))
				return new[] { CommandWithPrefix };
			
			if (!inputLower.StartsWith(CommandWithPrefix.ToLower()))
				return Array.Empty<string>();
			
			var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 1)
				return new[] { $"{CommandWithPrefix} list", $"{CommandWithPrefix} change", $"{CommandWithPrefix} leave" };
			
			if (parts.Length == 2) {
				var subCommand = parts[1].ToLower();
				if ("list".StartsWith(subCommand))
					return new[] { $"{CommandWithPrefix} list" };
				if ("change".StartsWith(subCommand))
					return new[] { $"{CommandWithPrefix} change" };
				if ("leave".StartsWith(subCommand))
					return new[] { $"{CommandWithPrefix} leave" };
			}
			
			// Autocomplétion pour "change <id>"
			if (parts.Length == 2 && parts[1].Equals("change", StringComparison.OrdinalIgnoreCase)) {
				var sessions = Main.Instance.GetSessions().ToArray();
				return sessions.Select(s => $"{CommandWithPrefix} change {s.Id}").ToArray();
			}
			
			if (parts.Length == 3 && parts[1].Equals("change", StringComparison.OrdinalIgnoreCase)) {
				var sessionIdPartial = parts[2].ToLower();
				var sessions = Main.Instance.GetSessions()
					.Where(s => s.Id.ToLower().StartsWith(sessionIdPartial))
					.ToArray();
				return sessions.Select(s => $"{CommandWithPrefix} change {s.Id}").ToArray();
			}
			
			// Autocomplétion pour "leave [id]"
			if (parts.Length == 2 && parts[1].Equals("leave", StringComparison.OrdinalIgnoreCase)) {
				var sessions = Main.Instance.GetSessions().ToArray();
				return sessions.Select(s => $"{CommandWithPrefix} leave {s.Id}").ToArray();
			}
			
			if (parts.Length == 3 && parts[1].Equals("leave", StringComparison.OrdinalIgnoreCase)) {
				var sessionIdPartial = parts[2].ToLower();
				var sessions = Main.Instance.GetSessions()
					.Where(s => s.Id.ToLower().StartsWith(sessionIdPartial))
					.ToArray();
				return sessions.Select(s => $"{CommandWithPrefix} leave {s.Id}").ToArray();
			}
			
			return Array.Empty<string>();
		}

		public UniTask<bool> Execute(string input, IContext context = null)
			=> UniTask.FromResult(ExecuteInternal(input, context));

		private bool ExecuteInternal(string input, IContext context = null) {
			if (string.IsNullOrWhiteSpace(input) || context == null)
				return false;

			var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0 || !parts[0].Equals(CommandWithPrefix, StringComparison.OrdinalIgnoreCase))
				return false;

			// Commande sans sous-commande : afficher la liste par défaut
			if (parts.Length == 1) {
				ListSessions(context);
				return true;
			}

			var subCommand = parts[1].ToLower();
			
			switch (subCommand) {
				case "list":
					ListSessions(context);
					return true;
				
				case "change":
					if (parts.Length < 3) {
						context.PrintLn(LanguageManager.Get("terminal.command.sessions.change.missing_id"));
						context.PrintLn(LanguageManager.Get("terminal.command.sessions.change.usage", CommandWithPrefix));
						return true;
					}
					ChangeSession(parts[2], context);
					return true;
				
				case "leave":
					// Si un ID est fourni, l'utiliser, sinon utiliser la session courante
					var sessionIdToLeave = parts.Length >= 3 ? parts[2] : null;
					LeaveSession(sessionIdToLeave, context);
					return true;
				
				default:
					context.PrintLn(LanguageManager.Get("terminal.command.sessions.unknown_subcommand", subCommand));
					context.PrintLn(GetUsage());
					return true;
			}
		}

		private void ListSessions(IContext context) {
			var sessions = Main.Instance.GetSessions().ToArray();
			var currentSessionId = Main.Instance.Current;

			if (sessions.Length == 0) {
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.no_sessions"));
				return;
			}

			context.PrintLn(
				LanguageManager.Get(
					"terminal.command.sessions.header",
					sessions.Length
				)
			);

			for (var i = 0; i < sessions.Length; i++) {
				var session = sessions[i];
				if (session == null) continue;

				var isCurrent = session.Id == currentSessionId;
				var indicator = isCurrent 
					? LanguageManager.Get("terminal.command.sessions.current") 
					: "";

				var displayName = session.GetShortName() ?? session.Id;
				var statusKey = $"terminal.command.sessions.status.{session.State.Status.ToString().ToSnakeCase()}";
				var statusText = LanguageManager.Get(statusKey);

				// Vérifier si c'est une session réseau
				if (session is INetSession netSession) {
					var connectionStatus = netSession.IsConnected 
						? LanguageManager.Get("terminal.command.sessions.net.connected")
						: LanguageManager.Get("terminal.command.sessions.net.disconnected");
					
					var pingText = netSession.IsConnected && netSession.Ping >= 0
						? LanguageManager.Get("terminal.command.sessions.net.ping", netSession.Ping)
						: "";

					context.PrintLn(
						LanguageManager.Get(
							"terminal.command.sessions.entry_net",
							i + 1,
							displayName,
							statusText,
							connectionStatus,
							pingText,
							indicator
						)
					);
				}
				else {
					context.PrintLn(
						LanguageManager.Get(
							"terminal.command.sessions.entry",
							i + 1,
							displayName,
							statusText,
							indicator
						)
					);
				}
			}
		}

		private async void ChangeSession(string sessionId, IContext context) {
			if (string.IsNullOrWhiteSpace(sessionId)) {
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.change.missing_id"));
				return;
			}

			if (!Main.Instance.TryGet(sessionId, out var session)) {
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.change.not_found", sessionId));
				return;
			}

			if (Main.Instance.Current == sessionId) {
				var displayName = session.GetShortName() ?? sessionId;
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.change.already_current", displayName));
				return;
			}

			try {
				await Main.Instance.SetCurrent(sessionId);
				var displayName = session.GetShortName() ?? sessionId;
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.change.success", displayName));
			}
			catch (Exception ex) {
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.change.error", ex.Message));
			}
		}

		private async void LeaveSession(string sessionId, IContext context) {
			// Si aucun ID n'est fourni, utiliser la session courante
			if (string.IsNullOrWhiteSpace(sessionId)) {
				sessionId = Main.Instance.Current;
				
				if (string.IsNullOrWhiteSpace(sessionId)) {
					context.PrintLn(LanguageManager.Get("terminal.command.sessions.leave.no_current"));
					return;
				}
			}

			if (!Main.Instance.TryGet(sessionId, out var session)) {
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.leave.not_found", sessionId));
				return;
			}

			var displayName = session.GetShortName() ?? sessionId;
			var isCurrent = Main.Instance.Current == sessionId;

			try {
				// Si c'est la session courante, la désélectionner d'abord
				if (isCurrent) {
					await Main.Instance.SetCurrent(null);
				}
				
				await session.Dispose();
				Main.Instance.Remove(session);
				
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.leave.success", displayName));
			}
			catch (Exception ex) {
				context.PrintLn(LanguageManager.Get("terminal.command.sessions.leave.error", ex.Message));
			}
		}
	}
}
