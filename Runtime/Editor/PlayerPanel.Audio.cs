using Nox.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.Sessions.Runtime.Editor {
	public partial class EntityInstance {
		private VisualElement _audioSection;
		private Label _audioClip;
		private Label _audioLength;
		private Label _audioFrequency;
		private Label _audioChannels;

		private void BindAudio(VisualElement root) {
			_audioSection = root.Q<VisualElement>("audio-section");
			_audioClip = root.Q<Label>("audio-clip");
			_audioLength = root.Q<Label>("audio-length");
			_audioFrequency = root.Q<Label>("audio-frequency");
			_audioChannels = root.Q<Label>("audio-channels");
		}

		/// <summary>Audio clip of the entity, shown only when it carries the audio capability.</summary>
		private void RefreshAudio() {
			var audio = _entity as IAudioEntity;
			SetVisible(_audioSection, audio != null);

			if (audio == null)
				return;

			AudioClip clip = null;

			// A world entity may answer before its clip is ready: the read is not allowed to break the page.
			try {
				clip = audio.GetAudio();
			} catch {
				clip = null;
			}

			Set(_audioClip, clip != null ? clip.name : "None");
			SetTooltip(_audioClip, clip != null ? clip.name : null);
			Set(_audioLength, clip != null ? FormatFloat(clip.length) : "-");
			Set(_audioFrequency, clip != null ? clip.frequency.ToString() : "-");
			Set(_audioChannels, clip != null ? clip.channels.ToString() : "-");
		}
	}
}
