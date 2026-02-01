using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Sessions.Runtime.Settings {
	public sealed class RenderEntity : RangeHandler {
		public override string[] GetPath()
			=> new[] { "sessions", "visual", "render_entity" };

		public override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab");

		public RenderEntity() {
			SetRange(5f, 200f);
			SetStep(.1f);
			SetValue(CCK.Sessions.Settings.RenderEntityDistance);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.meters");
		}

		public override void OnValueChanged(float value)
			=> CCK.Sessions.Settings.RenderEntityDistance = value;
	}
}