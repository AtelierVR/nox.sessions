using Nox.Players;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	public partial class EntityInstance {
		private VisualElement _ownershipSection;
		private Label _ownerDisplay;
		private Label _ownerId;
		private Label _ownerIsOwner;
		private Label _ownerLocal;
		private Label _ownerMaster;

		private void HookOwnership() {
			Owned?.OnOwnershipChanged.AddListener(OnOwnershipChanged);
		}

		private void UnhookOwnership() {
			Owned?.OnOwnershipChanged.RemoveListener(OnOwnershipChanged);
		}

		/// <summary>The ownership of the displayed entity changed: its owner row is rewritten at once.</summary>
		private void OnOwnershipChanged(IPlayer owner, IPlayer previous)
			=> RefreshOwnership();

		private void BindOwnership(VisualElement root) {
			_ownershipSection = root.Q<VisualElement>("ownership-section");
			_ownerDisplay = root.Q<Label>("owner-display");
			_ownerId = root.Q<Label>("owner-id");
			_ownerIsOwner = root.Q<Label>("owner-is-owner");
			_ownerLocal = root.Q<Label>("owner-local");
			_ownerMaster = root.Q<Label>("owner-master");
		}

		/// <summary>Owner of the entity, shown only when the entity carries the ownership capability.</summary>
		private void RefreshOwnership() {
			var owned = Owned;
			SetVisible(_ownershipSection, owned != null);

			if (owned == null)
				return;

			var owner = owned.Owner;
			var display = owner?.Display;

			Set(_ownerDisplay, string.IsNullOrEmpty(display) ? "No owner" : display);
			SetTooltip(_ownerDisplay, display);
			Set(_ownerId, owner != null ? owner.Id.ToString() : "-");
			Set(_ownerIsOwner, FormatBool(owned.IsOwner));
			Set(_ownerLocal, FormatFlag(owner == null ? (bool?)null : owner.IsLocal));
			Set(_ownerMaster, FormatFlag(owner == null ? (bool?)null : owner.IsMaster));
		}
	}
}
