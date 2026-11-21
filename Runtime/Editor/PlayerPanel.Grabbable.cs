using System.Linq;
using Nox.Interactions;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	public partial class EntityInstance {
		private VisualElement _grabbableSection;
		private Label _grabbableState;
		private Label _grabbableEnabled;
		private Label _grabbableHeld;
		private Label _grabbableGrabbers;

		private void BindGrabbable(VisualElement root) {
			_grabbableSection = root.Q<VisualElement>("grabbable-section");
			_grabbableState = root.Q<Label>("grabbable-state");
			_grabbableEnabled = root.Q<Label>("grabbable-enabled");
			_grabbableHeld = root.Q<Label>("grabbable-held");
			_grabbableGrabbers = root.Q<Label>("grabbable-grabbers");
		}

		/// <summary>Grab state of the entity, shown only when it carries the grabbable capability.</summary>
		private void RefreshGrabbable() {
			var networked = _entity as INetworkedGrabbable;
			var grabbable = networked?.Grabbable;

			SetVisible(_grabbableSection, grabbable != null);

			if (grabbable == null)
				return;

			Set(_grabbableState, grabbable.State.ToString());
			Set(_grabbableEnabled, FormatBool(grabbable.IsGrabbable));

			var group = grabbable as IGrabGroup;
			Set(_grabbableHeld, group != null ? group.HeldCount.ToString() : "-");

			var grabbers = group?.Grabbers;
			var names = grabbers == null || grabbers.Count == 0
				? "-"
				: string.Join(", ", grabbers.Select(grabber => $"{grabber.Rig} ({grabber.Kind})"));

			Set(_grabbableGrabbers, names);
			SetTooltip(_grabbableGrabbers, names);
		}
	}
}
