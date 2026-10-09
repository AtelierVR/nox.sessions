using Nox.CCK.Settings;
using UnityEngine;

namespace Nox.Sessions.Runtime.Settings {
	public sealed class RenderEntity : RangeHandler {
		public override string[] Path
			=> new[] { "sessions", "visual", "render_entity" };

		public override int Order => 80000;

		override protected GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab");

		public RenderEntity() {
			SetRange(5f, 200f);
			SetStep(.1f);
			SetValue(CCK.Sessions.Settings.RenderEntityDistance);
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			// Pas de 10 cm : l'affichage entier ("settings.range.value.meters") perdait les décimales.
			SetValueKey("settings.range.value.float_meters");
		}

		override protected void OnValueChanged(float value)
			=> CCK.Sessions.Settings.RenderEntityDistance = value;
	}
}