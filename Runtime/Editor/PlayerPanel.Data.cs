using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	public partial class EntityInstance {
		private VisualElement _dataSection;
		private Label _dataType;
		private Label _dataCount;
		private Label _dataKeys;

		/// <summary>Maximum number of keys rendered before the list is cut.</summary>
		private const int DataKeysMax = 12;

		private void BindData(VisualElement root) {
			_dataSection = root.Q<VisualElement>("data-section");
			_dataType = root.Q<Label>("data-type");
			_dataCount = root.Q<Label>("data-count");
			_dataKeys = root.Q<Label>("data-keys");
		}

		/// <summary>
		/// Data container of the entity. The key enumeration is not part of <c>IDataContainer</c>, so it is
		/// read from the concrete container (the same approach as <see cref="ReadFlag"/>): an entity that
		/// exposes neither count nor keys simply hides the section.
		/// </summary>
		private void RefreshData() {
			var data = _entity?.Data;

			var type = data?.GetType();

			// Only a container that can tell what it holds is worth a section.
			var count = type?.GetProperty("Count")?.GetValue(data) as int?;
			var keys = type?.GetProperty("Keys")?.GetValue(data) as IEnumerable<string>;

			SetVisible(_dataSection, count != null || keys != null);

			if (count == null && keys == null)
				return;

			Set(_dataType, type.Name);
			SetTooltip(_dataType, type.FullName);
			Set(_dataCount, count != null ? count.Value.ToString() : "-");

			if (keys == null) {
				Set(_dataKeys, "-");
				SetTooltip(_dataKeys, null);
				return;
			}

			var list = new List<string>();

			foreach (var key in keys) {
				if (list.Count >= DataKeysMax) {
					list.Add("...");
					break;
				}

				list.Add(key);
			}

			var joined = list.Count == 0 ? "-" : string.Join(", ", list);

			Set(_dataKeys, ShortenPath(joined));
			SetTooltip(_dataKeys, joined);
		}
	}
}
