using System;
using System.Linq;
using Nox.CCK;
using Nox.CCK.Scripting;
using Nox.Entities;
using Nox.Scripting;
using Nox.Sessions.Runtime.Converters;
using UnityEngine;

namespace Nox.Sessions.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"teams"</c> — the teams of the current session and the relations between them.
	/// <code>
	/// import teams from 'teams';
	///
	/// teams.all
	/// teams.create('Rouge', Color.red)
	/// </code>
	/// <para>
	/// The values (<c>all</c>, <c>count</c>) are live: read them from the namespace, a named import
	/// copies them when the script is loaded. The scripts only start once the session is ready, so
	/// <c>all</c> is already registered when they run. A player's team is exposed as <c>player.team</c>
	/// (an <c>ITeam</c>, or <c>null</c>): read it to know the team, write it with an <c>ITeam</c> or a
	/// team id to assign one (<c>0</c> clears it). The assignment is local — a world script that must
	/// agree across clients has to replicate the choice itself (see
	/// <c>Assets/Samples/Worlds/scripts/team.js</c>).
	/// </para>
	/// </summary>
	public static class TeamsModule {
		/// <summary>The team registry of the current session, or <c>null</c> outside a session.</summary>
		private static ITeamSession Teams(IScriptingContext ctx)
			=> ctx.Session as ITeamSession;

		private static ITeam At(ITeamSession teams, object id)
			=> teams == null || id == null ? null : teams.GetTeam(id.ToInt());

		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("teams")
				.WithTags("session")
				.AddType("Team", TeamConverter.Team)
				.AddVariable("all",   ctx => Teams(ctx)?.GetTeams() ?? Array.Empty<ITeam>())
				.AddVariable("count", ctx => (object)(Teams(ctx)?.GetTeams().Length ?? 0))
				.AddMethod("at", (ctx, args) => {
					var teams = Teams(ctx)?.GetTeams();
					return teams == null || args.Length == 0
						? null
						: teams.ElementAtOrDefault(args[0].ToInt());
				})
				.AddMethod("get", (ctx, args) => At(Teams(ctx), args.Length > 0 ? args[0] : null))
				.AddMethod("create", (ctx, args) => {
					var teams = Teams(ctx);
					if (teams == null || args.Length == 0)
						return null;
					var color = args.Length > 1
						? (Color)ctx.FromScript(args[1], typeof(Color))
						: Color.white;
					return teams.CreateTeam(args[0]?.ToString() ?? "", color);
				})
				.AddMethod("remove", (ctx, args) => {
					var teams = Teams(ctx);
					return teams != null && args.Length > 0 && teams.RemoveTeam(args[0].ToInt());
				})
				.AddMethod("relation", (ctx, args) => {
					var teams = Teams(ctx);
					if (teams == null || args.Length < 2)
						return null;
					return teams.GetRelation(At(teams, args[0]), At(teams, args[1])).ToString();
				})
				.AddMethod("setRelation", (ctx, args) => {
					var teams = Teams(ctx);
					if (teams == null || args.Length < 3)
						return null;
					var name = args[2]?.ToString();
					teams.SetRelation(
						At(teams, args[0]),
						At(teams, args[1]),
						Enum.TryParse<TeamRelation>(name, true, out var relation) ? relation : TeamRelation.Neutral);
					return null;
				})
				.Build();
	}
}
