using Nox.Entities;
using UnityEngine;

namespace Nox.Sessions {
	/// <summary>
	/// Session-level registry of the teams: the teams themselves plus the relations (alliances,
	/// enmities) between them.
	/// <para>
	/// Entities point at their team through <see cref="ITeamEntity.Team"/>; the session owns the
	/// catalogue and the <see cref="TeamRelation"/> matrix.
	/// </para>
	/// </summary>
	public interface ITeamSession {
		/// <summary>All teams registered in the session.</summary>
		ITeam[] GetTeams();

		/// <summary>Get a team by identifier, or <c>null</c> when it does not exist.</summary>
		ITeam GetTeam(int id);

		/// <summary>Create a team with the given name and colour, and return it.</summary>
		ITeam CreateTeam(string name, Color color);

		/// <summary>Remove a team (entities keeping it are detached).</summary>
		bool RemoveTeam(int id);

		/// <summary>Relation from <paramref name="team"/> to <paramref name="other"/>.</summary>
		TeamRelation GetRelation(ITeam team, ITeam other);

		/// <summary>Set the (symmetrical) relation between two teams.</summary>
		void SetRelation(ITeam team, ITeam other, TeamRelation relation);
	}
}
