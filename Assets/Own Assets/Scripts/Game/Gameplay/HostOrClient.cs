using Game.Managers;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Gameplay
{
    public class HostOrClient : MonoBehaviour
    {
        private bool _leavingGame;

        /// <summary>
        /// Add listeners.
        /// </summary>
        private void Awake()
        {
            StartCoroutine(SetListeners());
        }

        /// <summary>
        /// Starts the host or client and set the relay data and get the PlayerID.
        /// </summary>
        private void Start()
        {
            if (RelayManager.Instance.IsHost)
            {
                (byte[] allocationID, byte[] key, byte[] connectionData, string ip, int port) = RelayManager.Instance.GetHostConnectionInfo();
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ip, (ushort)port, allocationID, key, connectionData, true);
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                (byte[] allocationID, byte[] key, byte[] connectionData, byte[] hostConnectionData, string ip, int port) = RelayManager.Instance.GetClientConnectionInfo();
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ip, (ushort)port, allocationID, key, connectionData, hostConnectionData, true);
                NetworkManager.Singleton.StartClient();
            }
            PlayerInfoManager.Instance.PlayerID = NetworkManager.Singleton.LocalClientId;
        }

        /// <summary>
        /// Check if the networkmanager is shutting down. If so leave the game.
        /// </summary>
        private async void Update()
        {
            if (!NetworkManager.Singleton)
                return;
            if (!NetworkManager.Singleton.ShutdownInProgress || _leavingGame)
                return;
            _leavingGame = true;
            await GameLobbyManager.Instance.LeaveGame();
        }

        /// <summary>
        /// On getting disconnected by the server leave the game.
        /// This method is called on both the server and on the client that got disconnected.
        /// </summary>
        /// <param name="clientID">ClientID of the client that got disconnected.</param>
        private async void OnClientDisconnected(ulong clientID)
        {
            if (NetworkManager.Singleton.LocalClientId != clientID)
                return;
           await  GameLobbyManager.Instance.LeaveGame();
        }

        /// <summary>
        /// Listen for client disconnection.
        /// </summary>
        /// <returns></returns>
        private IEnumerator SetListeners()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton);
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        /// <summary>
        /// Remove listener.
        /// </summary>
        private void OnDestroy()
        {
            if (!NetworkManager.Singleton)
                return;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
