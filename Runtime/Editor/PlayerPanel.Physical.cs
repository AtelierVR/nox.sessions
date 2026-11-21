using Nox.Entities;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	public partial class EntityInstance {
		private VisualElement _physicalSection;
		private Label _physicalState;
		private Label _physicalBody;

		private void BindPhysical(VisualElement root) {
			_physicalSection = root.Q<VisualElement>("physical-section");
			_physicalState = root.Q<Label>("physical-state");
			_physicalBody = root.Q<Label>("physical-body");
		}

		/// <summary>Physical representation of the entity, shown only when it carries that capability.</summary>
		private void RefreshPhysical() {
			var physical = PhysicalEntity;
			SetVisible(_physicalSection, physical != null);

			if (physical == null)
				return;

			Set(_physicalState, physical.PhysicalState.ToString());

			var body = physical.TryGetPhysical<Physical>(out var found) && found != null
				? found.GetType().Name
				: "None";

			Set(_physicalBody, body);
			SetTooltip(_physicalBody, body);
		}
	}
}
