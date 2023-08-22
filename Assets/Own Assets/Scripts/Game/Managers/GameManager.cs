using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game.Managers
{
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Starts the host or client and sets the relay data.
        /// </summary>
        private void Start()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
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
        }

        /// <summary>
        /// Connection approval callback. This makes sure that the connection is approved and that a playerobject should always spawn.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="respone"></param>
        private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse respone)
        {
            respone.Approved = true;
            respone.CreatePlayerObject = true;
            respone.Pending = false;
        }
    }
}
