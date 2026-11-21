using System;
using System.Collections.Generic;
using Nox.CCK.Entities;
using Nox.Entities;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	internal struct PartRow {
		public VisualElement Container;
		public Label PosLabel;
		public Label RotLabel;
		public DateTime LastUpdated;
		public DateTime ChangedAt;
	}

	public partial class EntityInstance {
		private VisualElement _partsSection;
		private VisualElement _partsList;
		private VisualElement _partsEmpty;
		private readonly Dictionary<ushort, PartRow> _partRows = new();

		private void BindParts(VisualElement root) {
			_partsSection = root.Q<VisualElement>("parts-section");
			_partsList = root.Q<VisualElement>("parts-list");
			_partsEmpty = root.Q<VisualElement>("parts-empty");
		}

		/// <summary>Parts of the displayed entity (a player rig, the sub-elements of a prop, ...).</summary>
		private IPart[] GetParts()
			=> _entity is IMultiPartEntity<IPart> multi
				? multi.GetParts()
				: Array.Empty<IPart>();

		/// <summary>
		/// Rows of the parts section. The whole section is hidden for an entity that is not multi-part:
		/// only the entity information is always shown.
		/// </summary>
		private void RefreshParts() {
			if (_partsSection == null)
				return;

			var supported = _entity is IMultiPartEntity<IPart>;
			SetVisible(_partsSection, supported);

			if (!supported) {
				if (_partRows.Count != 0) {
					_partsList.Clear();
					_partRows.Clear();
				}

				return;
			}

			var parts = GetParts();

			if (parts.Length == 0) {
				if (_partRows.Count != 0) {
					_partsList.Clear();
					_partRows.Clear();
				}

				SetVisible(_partsList, false);
				SetVisible(_partsEmpty, true);

				return;
			}

			if (parts.Length != _partRows.Count) {
				LoadParts();
				return;
			}

			var now = DateTime.UtcNow;

			foreach (var part in parts) {
				if (!_partRows.TryGetValue(part.Id, out var row))
					continue;

				// An offline part reads its live transform from the controller and only bumps
				// `Updated` when something writes it, so the rendered values are compared as well:
				// otherwise the row would never move.
				var position = FormatVec3(part.Position);
				var rotation = FormatEuler(part.Rotation);

				if (part.Updated != row.LastUpdated || row.PosLabel.text != position || row.RotLabel.text != rotation) {
					row.PosLabel.text = position;
					row.RotLabel.text = rotation;
					row.ChangedAt = now;
					row.LastUpdated = part.Updated;
					_partRows[part.Id] = row;
				}

				SetHighlight(row.Container, Fade(row.ChangedAt, now));
			}
		}

		private void LoadParts() {
			_partsList.Clear();
			_partRows.Clear();

			var parts = GetParts();

			if (parts.Length == 0) {
				SetVisible(_partsList, false);
				SetVisible(_partsEmpty, true);
				return;
			}

			SetVisible(_partsList, true);
			SetVisible(_partsEmpty, false);

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/part-item.uxml");
			var epoch = DateTime.MinValue;

			foreach (var part in parts) {
				var item = itemAsset.CloneTree();
				var container = item.Q<VisualElement>();
				var posLabel = item.Q<Label>("position");
				var rotLabel = item.Q<Label>("rotation");

				var nameLabel = item.Q<Label>("name");
				var name = part.Id.ToEntityRig().ToString();

				nameLabel.text = name;
				SetTooltip(nameLabel, name);
				posLabel.text = FormatVec3(part.Position);
				rotLabel.text = FormatEuler(part.Rotation);

				_partsList.Add(item);
				_partRows[part.Id] = new PartRow {
					Container = container,
					PosLabel = posLabel,
					RotLabel = rotLabel,
					LastUpdated = part.Updated,
					ChangedAt = epoch,
				};
			}
		}
	}
}
