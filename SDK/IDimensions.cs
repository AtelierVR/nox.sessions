using Nox.Worlds;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.Sessions {
	public interface IDimensions {
		/// <summary>
		/// The world associated with this dimension.
		/// </summary>
		public IWorldIdentifier Identifier { get; }

		/// <summary>
		/// Indicates whether the scene is fully loaded
		/// and ready for interaction.
		/// </summary>
		public bool IsLoaded(int index);

		/// <summary>
		/// The Unity Scene associated with this scene entry.
		/// A scene can have multiple scene entries of different sessions.
		/// </summary>
		public Scene GetScene(int index);

		/// <summary>
		/// The root GameObject of the scene entry.
		/// This GameObject serves as the parent for all objects
		/// associated with this scene entry.
		/// </summary>
		public GameObject GetAnchor(int index);

		/// <summary>
		/// The world descriptor associated with this scene entry.
		/// This descriptor provides modules about the <see cref="GetAnchor"/> and its contents.
		/// </summary>
		public IWorldDescriptor GetDescriptor(int index);
	}
}