using Cysharp.Threading.Tasks;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Servers;
using Nox.Sessions;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Sessions.Runtime
{
    /// <summary>
    /// Manages the WebSocket connection lifecycle for the session module.
    /// - Listens for socket connect/disconnect via the server API.
    /// - Sends set_location when the current session changes.
    /// </summary>
    internal class Socket
    {

        internal void Initialize()
        {
            if (ServerAPI == null)
            {
                Logger.LogWarning("Server API not available at initialize time.", tag: nameof(Socket));
                return;
            }

            ServerAPI.OnSocketConnected.AddListener(OnSocketConnected);
            Main.Instance.OnCurrentChanged.AddListener(OnCurrentSessionChanged);

            // If already connected, register immediately
            var existing = ServerAPI.Current;
            if (existing != null && existing.IsConnected())
                OnSocketConnected();
        }

        internal void Dispose()
        {
            ServerAPI?.OnSocketConnected.RemoveListener(OnSocketConnected);
            Main.Instance?.OnCurrentChanged.RemoveListener(OnCurrentSessionChanged);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static IServerAPI ServerAPI
            => Main.CoreAPI?.ModAPI
                ?.GetMod("server")
                ?.GetInstance<IServerAPI>();

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnSocketConnected()
            => SendLocation(Main.Instance?.GetCurrentSession());

        private void OnCurrentSessionChanged(ISession previous, ISession next)
            => SendLocation(next);

        // ── Packet helpers ────────────────────────────────────────────────────────

        private void SendLocation(ISession current)
            => SendLocation(current?.GetInstance() ?? Identifier.Invalid);

        private void SendLocation(Identifier instance)
            => SendLocationAsync(instance).Forget();

        private async UniTask SendLocationAsync(Identifier instance)
        {
            var serverAPI = ServerAPI;
            if (serverAPI == null)
            {
                Logger.LogWarning("Cannot send location, server API not available.", tag: nameof(Socket));
                return;
            }

            var socket = serverAPI.Current;
            if (socket == null || !socket.IsConnected())
            {
                Logger.LogWarning("Cannot send location, no active socket connection.", tag: nameof(Socket));
                return;
            }

            var packet = new SocketPacket<string>
            {
                type = "set_location",
                payload = instance.IsValid()
                    ? instance.ToShortString(true)
                    : null
            };

            await socket.Emit(packet);
            Logger.LogDebug($"Sent set_location => {packet.payload ?? "null"}", tag: nameof(Socket));
        }
    }
}
