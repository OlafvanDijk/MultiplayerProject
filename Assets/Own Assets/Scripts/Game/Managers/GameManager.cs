using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Netcode.Transports.UTP;

namespace Game
{
    public class GameManager : MonoBehaviour
    {


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

        private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse respone)
        {
            respone.Approved = true;
            respone.CreatePlayerObject = true;
            respone.Pending = false;
        }
    }
}
