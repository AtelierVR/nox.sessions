using Nox.CCK.Network;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Sessions.Runtime.Settings {
	public sealed class ClearPhysical : RangeHandler {
		public override string[] GetPath()
			=> new[] { "sessions", "visual", "clear_physical" };

		public override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab");

		public ClearPhysical() {
			SetRange(0f, 30f);
			SetStep(1f);
			SetValue(CCK.Sessions.Settings.ClearPhysicalAfterSeconds);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.seconds");
		}

		public override void OnValueChanged(float value)
			=> CCK.Sessions.Settings.ClearPhysicalAfterSeconds = value.ToInt();
	}
}