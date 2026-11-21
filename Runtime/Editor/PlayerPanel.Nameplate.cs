using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	public partial class EntityInstance {
		private VisualElement _nameplateSection;
		private Label _nameplateVisible;
		private Label _healthbarVisible;

		private void BindNameplate(VisualElement root) {
			_nameplateSection = root.Q<VisualElement>("nameplate-section");
			_nameplateVisible = root.Q<Label>("nameplate-visible");
			_healthbarVisible = root.Q<Label>("healthbar-visible");
		}

		/// <summary>
		/// Nameplate flags of the entity. They are read by name rather than through
		/// <c>INameplateEntity</c>: this editor assembly does not reference <c>Nox.Nameplate</c>.
		/// </summary>
		private void RefreshNameplate() {
			var visible = ReadFlag(_entity, "NameplateVisible");
			var healthbar = ReadFlag(_entity, "HealthbarVisible");

			SetVisible(_nameplateSection, visible != null || healthbar != null);

			if (visible == null && healthbar == null)
				return;

			Set(_nameplateVisible, FormatFlag(visible));
			Set(_healthbarVisible, FormatFlag(healthbar));
		}
	}
}
