using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Avatars.Parameters;
using Nox.Entities;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	internal struct PropertyRow {
		public VisualElement Container;
		public Label ValueLabel;
		public Label FlagsLabel;
		public string Group;
		/// <summary>Last rendered value, so a live property is caught even when it does not stamp.</summary>
		public string Value;
		public DateTime LastUpdatedAt;
		public DateTime ChangedAt;
	}

	/// <summary>
	/// A section of the properties list.
	/// </summary>
	internal readonly struct PropertyGroup {
		public readonly int Order;
		public readonly string Label;
		public readonly string Description;

		/// <summary>
		/// Whether the group is split into subsections, one per parameter category (the avatar parameters).
		/// </summary>
		public readonly bool Categorized;

		/// <summary>
		/// Category of the avatar parameter (snake_case), or <c>null</c> when it declares none: the
		/// parameters without a category go to the « general » subsection.
		/// </summary>
		public readonly string Category;

		public PropertyGroup(int order, string label, string description, bool categorized = false, string category = null) {
			Order       = order;
			Label       = label;
			Description = description;
			Categorized = categorized;
			Category    = category;
		}
	}

	public partial class EntityInstance {
		private VisualElement _propertiesSection;
		private VisualElement _propertiesList;
		private VisualElement _propertiesEmpty;
		private readonly Dictionary<int, PropertyRow> _rows = new();

		private void BindProperties(VisualElement root) {
			_propertiesSection = root.Q<VisualElement>("properties-section");
			_propertiesList = root.Q<VisualElement>("properties-list");
			_propertiesEmpty = root.Q<VisualElement>("properties-empty");
		}

		/// <summary>
		/// Rows of the properties section: rebuilt when the set of keys changes, refreshed in place
		/// otherwise (the value is read live, so a property that updates itself is caught). The whole
		/// section is hidden for an entity that has no properties capability.
		/// </summary>
		private void RefreshProperties() {
			if (_propertiesSection == null)
				return;

			var supported = _entity is IPropertiesEntity;
			SetVisible(_propertiesSection, supported);

			if (!supported) {
				if (_rows.Count != 0) {
					_propertiesList.Clear();
					_rows.Clear();
				}

				return;
			}

			var properties = (_entity as IPropertiesEntity)?.Properties;

			if (properties == null || properties.Length == 0) {
				if (_rows.Count != 0) {
					_propertiesList.Clear();
					_rows.Clear();
				}

				SetVisible(_propertiesList, false);
				SetVisible(_propertiesEmpty, true);

				return;
			}

			if (properties.Length != _rows.Count) {
				LoadProperties();
				return;
			}

			// A property can also change group without the count changing (an UnassignedProperty gets
			// bound to an avatar parameter, or released back): rebuild so it moves to the right section.
			foreach (var property in properties)
				if (_rows.TryGetValue(property.Key, out var existing) && existing.Group != GroupLabel(GetPropertyGroup(property))) {
					LoadProperties();
					return;
				}

			var now = DateTime.UtcNow;

			foreach (var property in properties) {
				if (!_rows.TryGetValue(property.Key, out var row))
					continue;

				// The value is read first on purpose: a live property (a parameter bound to the avatar)
				// only stamps its UpdatedAt when it is read, so comparing beforehand would never
				// report a change.
				var value = FormatValue(property.Value);
				var changed = property.UpdatedAt != row.LastUpdatedAt || row.Value != value;

				if (changed) {
					var flags = property.Flags.ToString();

					row.ValueLabel.text = ShortenPath(value);
					row.FlagsLabel.text = flags;
					SetTooltip(row.ValueLabel, value);
					SetTooltip(row.FlagsLabel, flags);

					row.Value = value;
					row.LastUpdatedAt = property.UpdatedAt;
					row.ChangedAt = now;
					_rows[property.Key] = row;
				}

				SetHighlight(row.Container, Fade(row.ChangedAt, now));
			}
		}

		private void LoadProperties() {
			_propertiesList.Clear();
			_rows.Clear();

			var properties = (_entity as IPropertiesEntity)?.Properties;

			if (properties == null || properties.Length == 0) {
				SetVisible(_propertiesList, false);
				SetVisible(_propertiesEmpty, true);
				return;
			}

			SetVisible(_propertiesList, true);
			SetVisible(_propertiesEmpty, false);

			var itemAsset = _panel.API.AssetAPI.GetAsset<VisualTreeAsset>("panels/property-item.uxml");

			// Group by concrete implementation instead of listing everything flat: that is what tells a
			// parameter bound to the avatar apart from an incoming key this entity never declared.
			var groups = properties
				.Select(p => (Property: p, Group: GetPropertyGroup(p)))
				.GroupBy(x => (x.Group.Order, x.Group.Label))
				.OrderBy(g => g.Key.Order)
				.ThenBy(g => g.Key.Label, StringComparer.Ordinal);

			foreach (var group in groups)
				AddPropertyGroup(itemAsset, group.ToList());
		}

		/// <summary>
		/// Adds a top-level group. The avatar parameters are split into one subsection per category, the
		/// parameters without a category first in the « general » subsection; the other groups stay flat.
		/// The subsections are ordered by category, not by their label (which depends on the language).
		/// </summary>
		private void AddPropertyGroup(VisualTreeAsset itemAsset, List<(IProperty Property, PropertyGroup Group)> items) {
			var group = items[0].Group;

			_propertiesList.Add(CreateGroupHeader(group, items.Count));

			if (!group.Categorized) {
				AddPropertyRows(itemAsset, items);
				return;
			}

			var sections = items
				.GroupBy(x => ParameterCategoryLanguage.Normalize(x.Group.Category))
				.OrderBy(s => s.Key == null ? 0 : 1)
				.ThenBy(s => s.Key ?? string.Empty, StringComparer.Ordinal);

			foreach (var section in sections) {
				_propertiesList.Add(CreateCategoryHeader(section.Key, section.Count()));
				AddPropertyRows(itemAsset, section.ToList());
			}
		}

		/// <summary>
		/// Adds the rows of a section: named properties first (alphabetical), then the raw keys ascending —
		/// so an unexplained key is easy to pick out and stays in the same place between rebuilds.
		/// </summary>
		private void AddPropertyRows(VisualTreeAsset itemAsset, List<(IProperty Property, PropertyGroup Group)> items) {
			var epoch = DateTime.MinValue;

			var ordered = items
				.OrderBy(x => x.Property.Name == null ? 1 : 0)
				.ThenBy(x => x.Property.Name ?? string.Empty, StringComparer.Ordinal)
				.ThenBy(x => x.Property.Key);

			foreach (var (property, propertyGroup) in ordered) {
				var item = itemAsset.CloneTree();
				var container = item.Q<VisualElement>();
				var valueLabel = item.Q<Label>("value");
				var flagsLabel = item.Q<Label>("flags");

				var keyLabel = item.Q<Label>("key");
				var key = property.Name ?? $"Key: {property.Key}";
				var value = FormatValue(property.Value);
				var flags = property.Flags.ToString();

				keyLabel.text = ShortenPath(key);
				valueLabel.text = ShortenPath(value);
				flagsLabel.text = flags;

				SetTooltip(keyLabel, key);
				SetTooltip(valueLabel, value);
				SetTooltip(flagsLabel, flags);

				_propertiesList.Add(item);

				_rows[property.Key] = new PropertyRow {
					Container = container,
					ValueLabel = valueLabel,
					FlagsLabel = flagsLabel,
					Group = GroupLabel(propertyGroup),
					Value = value,
					LastUpdatedAt = property.UpdatedAt,
					ChangedAt = epoch,
				};
			}
		}

		/// <summary>
		/// Maps a property to its section. Matching is done on the concrete type <b>name</b> rather
		/// than on a type reference on purpose: this editor assembly does not reference
		/// Nox.Relay.Runtime, where the implementations live.
		/// </summary>
		private static PropertyGroup GetPropertyGroup(IProperty property) {
			switch (property.GetType().Name) {
				case "AvatarParameterProperty":
					return new PropertyGroup(
						0, "Avatar Parameters",
						"Bound to a live avatar parameter: read once per tick and sent according to its sync flags.",
						categorized: true,
						category: ParameterCategoryLanguage.Normalize(property.Category)
					);
				case "UnassignedProperty":
					return new PropertyGroup(1, "Unassigned (key never declared)",
						"A value arrived for a key this entity has no property for, so Nox.Relay stored it as-is. " +
						"If this section keeps growing with new keys, the other client is sending keys we never registered.");
				case "Property":
					return new PropertyGroup(2, "Raw",
						"Plain key/value property declared by the entity itself.");
				case "DataProperty":
					return new PropertyGroup(2, "Data",
						"Value of the entity data container: the state an offline session keeps for its player.");
				default:
					return new PropertyGroup(3, property.GetType().Name,
						"Custom IProperty implementation.");
			}
		}

		/// <summary>
		/// Key of the section a property belongs to, used to detect a property that moved to another group
		/// or another parameter category without the property count changing.
		/// </summary>
		private static string GroupLabel(PropertyGroup group)
			=> group.Categorized
				? group.Label + "/" + ParameterCategoryLanguage.NormalizedOrDefault(group.Category)
				: group.Label;

		private static VisualElement CreateGroupHeader(PropertyGroup group, int count) {
			var header = new Label($"{group.Label}  ({count})") {
				tooltip = group.Description
			};
			header.AddToClassList("group-header");

			return header;
		}

		/// <summary>
		/// Header of a parameter category inside the avatar parameters: its label entry
		/// (<c>avatar.parameters.categories.&lt;category&gt;.label</c>), with the description entry as tooltip.
		/// </summary>
		private static VisualElement CreateCategoryHeader(string category, int count) {
			var header = new Label($"{ParameterCategoryLanguage.GetLabel(category)}  ({count})") {
				tooltip = ParameterCategoryLanguage.GetDescription(category)
			};
			header.AddToClassList("group-header");

			return header;
		}
	}
}
