using Game.Managers;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Gameplay
{
    public class HostOrClient : MonoBehaviour
    {
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
    }
}
