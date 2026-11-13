using Nox.Entities;
using UnityEngine;
using UnityEngine.Events;

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
		/// <summary>Invoked when a team is created.</summary>
		UnityEvent<ITeam> OnTeamCreated { get; }

		/// <summary>Invoked when a team is removed (the entities keeping it are detached).</summary>
		UnityEvent<ITeam> OnTeamRemoved { get; }

		/// <summary>Invoked when an entity joined, left or changed team.</summary>
		UnityEvent<ITeamEntity> OnTeamChanged { get; }

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
