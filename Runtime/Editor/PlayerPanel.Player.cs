using Nox.Avatars.Players;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	public partial class EntityInstance {
		private VisualElement _infoSection;
		private Label _infoTitle;
		private Label _displayLabel;
		private Label _playerId;
		private Label _playerDisplay;
		private Label _playerLocal;
		private Label _playerMaster;
		private VisualElement _avatarSection;
		private Label _avatarId;
		private VisualElement _teamSection;
		private VisualElement _teamColor;
		private Label _teamName;
		private Label _teamId;
		private VisualElement _vitalsSection;
		private Label _health;
		private Label _maxHealth;
		private Label _invisible;
		private VisualElement _movementSection;
		private Label _walkSpeed;
		private Label _moveAcceleration;
		private Label _jumpForce;
		private Label _sprintMultiplier;
		private Label _airControl;
		private Label _flySpeed;
		private Label _mayFly;
		private Label _immobilized;
		private Label _flying;
		private Label _crouching;
		private Label _sprinting;

		/// <summary>Team colour last applied, so the swatch/name style is not touched every tick.</summary>
		private int _teamColorId = int.MinValue;

		private void HookPlayer() {
			if (_session is not ITeamSession teamSession)
				return;

			teamSession.OnTeamChanged.AddListener(OnTeamChanged);
			teamSession.OnTeamRemoved.AddListener(OnTeamRemoved);
		}

		private void UnhookPlayer() {
			if (_session is not ITeamSession teamSession)
				return;

			teamSession.OnTeamChanged.RemoveListener(OnTeamChanged);
			teamSession.OnTeamRemoved.RemoveListener(OnTeamRemoved);
		}

		/// <summary>The displayed player left the session: back to the session page.</summary>
		private void OnPlayerLeft(IPlayer player) {
			if (Player == null || player == null || player.Id != Player.Id)
				return;

			LeavePage(GoBackToSession);
		}

		/// <summary>The displayed player joined, left or changed team: its team row follows it.</summary>
		private void OnTeamChanged(ITeamEntity entity) {
			if (Player == null || entity == null || entity.Id != Player.Id)
				return;

			RefreshTeam(entity);
		}

		/// <summary>
		/// The team shown on this page was removed: the registry already dropped it, so the player now
		/// resolves to no team and the row has to be rewritten.
		/// </summary>
		private void OnTeamRemoved(ITeam team) {
			if (Player == null || team == null || _teamColorId != team.Id)
				return;

			RefreshTeam(Player as ITeamEntity);
		}

		private void BindPlayer(VisualElement root) {
			_infoSection = root.Q<VisualElement>("info-section");
			_infoTitle = root.Q<Label>("info-title");
			_displayLabel = root.Q<Label>("display-label");
			_playerId = root.Q<Label>("player-id");
			_playerDisplay = root.Q<Label>("player-display");
			_playerLocal = root.Q<Label>("player-local");
			_playerMaster = root.Q<Label>("player-master");
			_avatarSection = root.Q<VisualElement>("avatar-section");
			_avatarId = root.Q<Label>("avatar-id");
			_teamSection = root.Q<VisualElement>("team-section");
			_teamColor = root.Q<VisualElement>("team-color");
			_teamName = root.Q<Label>("team-name");
			_teamId = root.Q<Label>("team-id");
			_vitalsSection = root.Q<VisualElement>("vitals-section");
			_health = root.Q<Label>("health");
			_maxHealth = root.Q<Label>("max-health");
			_invisible = root.Q<Label>("invisible");
			_movementSection = root.Q<VisualElement>("movement-section");
			_walkSpeed = root.Q<Label>("walk-speed");
			_moveAcceleration = root.Q<Label>("move-acceleration");
			_jumpForce = root.Q<Label>("jump-force");
			_sprintMultiplier = root.Q<Label>("sprint-multiplier");
			_airControl = root.Q<Label>("air-control");
			_flySpeed = root.Q<Label>("fly-speed");
			_mayFly = root.Q<Label>("may-fly");
			_immobilized = root.Q<Label>("immobilized");
			_flying = root.Q<Label>("flying");
			_crouching = root.Q<Label>("crouching");
			_sprinting = root.Q<Label>("sprinting");
		}

		/// <summary>
		/// The player sections: identity first, then one section per capability the entity carries
		/// (living, moving, team). A section without its capability stays hidden.
		/// </summary>
		private void RefreshPlayer() {
			var player = Player;
			var team = _entity as ITeamEntity;
			var living = _entity as ILivingEntity;
			var moving = _entity as IMovingEntity;

			SetVisible(_avatarSection, player is IPlayerAvatar);
			SetVisible(_teamSection, team != null);
			SetVisible(_vitalsSection, living != null);
			SetVisible(_movementSection, moving != null);

			RefreshInfo(player);
			RefreshAvatar(player);
			RefreshTeam(team);
			RefreshVitals(living);
			RefreshMovement(moving);
		}

		private void RefreshInfo(IPlayer player) {
			var display = player?.Display;

			Set(_title, player != null ? $"Player: {display}" : $"Entity: {_entity.Id}");
			Set(_infoTitle, player != null ? "Player Information" : "Entity Information");
			Set(_displayLabel, player != null ? "Display Name" : "Type");
			Set(_playerId, _entity.Id.ToString());
			Set(_playerDisplay, player != null ? display : _entity.GetType().Name);
			SetTooltip(_playerDisplay, _playerDisplay?.text);
			Set(_playerLocal, player != null ? FormatBool(player.IsLocal) : "-");
			Set(_playerMaster, player != null ? FormatBool(player.IsMaster) : "-");
		}

		private void RefreshAvatar(IPlayer player) {
			var avatar = player is IPlayerAvatar playerAvatar ? playerAvatar.GetAvatar().ToString() : null;

			Set(_avatarId, ShortenPath(avatar ?? "N/A"));
			SetTooltip(_avatarId, avatar);
		}

		private void RefreshTeam(ITeamEntity entity) {
			if (entity == null)
				return;

			var team = entity.Team;

			Set(_teamName, team?.Name ?? "No team");
			Set(_teamId, team != null ? team.Id.ToString() : "-");

			// The team name carries the colour of the team (the swatch next to it as well).
			var teamColorId = team?.Id ?? -1;
			if (teamColorId == _teamColorId)
				return;

			_teamColorId = teamColorId;

			SetVisible(_teamColor, team != null);
			SetTint(_teamColor, team?.Color, background: true);
			SetTint(_teamName, team?.Color);
		}

		private void RefreshVitals(ILivingEntity living) {
			if (living == null)
				return;

			Set(_health, FormatFloat(living.Health));
			Set(_maxHealth, FormatFloat(living.MaxHealth));
			Set(_invisible, FormatBool(living.IsInvisible));
		}

		private void RefreshMovement(IMovingEntity moving) {
			if (moving == null)
				return;

			Set(_walkSpeed, FormatFloat(moving.WalkSpeed));
			Set(_moveAcceleration, FormatFloat(moving.MoveAcceleration));
			Set(_jumpForce, FormatFloat(moving.JumpForce));
			Set(_sprintMultiplier, FormatFloat(moving.SprintMultiplier));
			Set(_airControl, FormatFloat(moving.AirControl));
			Set(_flySpeed, FormatFloat(moving.FlySpeed));
			Set(_mayFly, FormatBool(moving.MayFly));
			Set(_immobilized, FormatBool(moving.IsImmobilized));
			Set(_flying, FormatBool(moving.IsFlying));
			Set(_crouching, FormatBool(moving.IsCrouching));
			Set(_sprinting, FormatBool(moving.IsSprinting));
		}
	}
}
