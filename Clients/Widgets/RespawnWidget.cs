using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.Sessions.Runtime;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.Sessions.Clients.Widgets {
	public class RespawnWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "respawn";

		private GameObject _content;

		public static void OnRespawn(EventData context)
			=> OnRespawn();

		private static void OnRespawn()
			=> SessionUtils.Respawn();

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 100;

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			var prefab = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/grid_item.prefab");
			var instance = Instantiate(prefab, parent);
			var component = instance.AddComponent<RespawnWidget>();
			var button = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(OnRespawn);
			instance.name = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values = (instance, component);
			prefab = Client.API.AssetAPI.GetAsset<GameObject>("ui:prefabs/widget.prefab");
			component._content = Instantiate(prefab, Reference.GetComponent<RectTransform>("content", instance));
			component.UpdateIcon().Forget();
			return true;
		}

		private async UniTask UpdateIcon() {
			var icon = await Client.API.AssetAPI.GetAssetAsync<Sprite>("ui:icons/flag.png");
			var labelIcon = Reference.GetComponent<Image>("icon", _content);
			labelIcon.sprite = icon;
		}
	}
}