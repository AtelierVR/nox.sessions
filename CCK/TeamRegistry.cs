using System.Collections.Generic;
using Nox.Entities;
using Nox.Sessions;
using UnityEngine;

namespace Nox.CCK.Sessions {
	/// <summary>Default <see cref="ITeam"/>: a named and coloured group of a session.</summary>
	public sealed class Team : ITeam {
		public Team(int id, string name, Color color) {
			Id    = id;
			Name  = name;
			Color = color;
		}

		/// <inheritdoc />
		public int Id { get; }

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public Color Color { get; set; }

		public override string ToString()
			=> $"Team[{Id}, {Name}]";
	}

	/// <summary>
	/// Default <see cref="ITeamSession"/>: in-memory registry of the teams of a session, plus the
	/// symmetrical <see cref="TeamRelation"/> between them.
	/// </summary>
	public sealed class TeamRegistry : ITeamSession {
		private readonly List<ITeam> _teams = new();
		private readonly Dictionary<long, TeamRelation> _relations = new();
		private int _nextId = 1;

		/// <inheritdoc />
		public ITeam[] GetTeams()
			=> _teams.ToArray();

		/// <inheritdoc />
		public ITeam GetTeam(int id) {
			if (id == 0)
				return null;

			foreach (var team in _teams)
				if (team.Id == id)
					return team;

			return null;
		}

		/// <inheritdoc />
		public ITeam CreateTeam(string name, Color color) {
			var team = new Team(_nextId++, name, color);
			_teams.Add(team);
			return team;
		}

		/// <inheritdoc />
		public bool RemoveTeam(int id) {
			var team = GetTeam(id);
			if (team == null)
				return false;

			_teams.Remove(team);

			// Drop the relations involving the removed team
			foreach (var key in new List<long>(_relations.Keys)) {
				var (a, b) = Unpack(key);
				if (a == id || b == id)
					_relations.Remove(key);
			}

			return true;
		}

		/// <inheritdoc />
		public TeamRelation GetRelation(ITeam team, ITeam other)
			=> team == null || other == null
				? TeamRelation.Neutral
				: _relations.TryGetValue(Pack(team.Id, other.Id), out var relation)
					? relation
					: TeamRelation.Neutral;

		/// <inheritdoc />
		public void SetRelation(ITeam team, ITeam other, TeamRelation relation) {
			if (team == null || other == null || team.Id == other.Id)
				return;

			_relations[Pack(team.Id, other.Id)] = relation;
			_relations[Pack(other.Id, team.Id)] = relation;
		}

		private static long Pack(int a, int b)
			=> ((long)a << 32) | (uint)b;

		private static (int, int) Unpack(long key)
			=> ((int)(key >> 32), (int)(key & 0xFFFFFFFF));
	}
}
