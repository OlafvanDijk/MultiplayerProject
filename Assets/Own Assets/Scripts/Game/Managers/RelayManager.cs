using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Utility;

namespace Game.Managers
{
    public class RelayManager : Singleton<RelayManager>
    {
        private bool _isHost;
        private string _relayJoinCode;

        private string _ip;
        private int _port;

        private byte[] _key;

        private Guid _allocationID;
        private byte[] _allocationIDBytes;

        private byte[] _hostconnectionData;
        private byte[] _connectionData;

        public string AllocationID => _allocationID.ToString();
        public string ConnectionData => _connectionData.ToString();

        public bool IsHost => _isHost;

        /// <summary>
        /// Create a relay and set the data to be used with NetCode for connecting to the relay endpoint.
        /// </summary>
        /// <param name="maxConnection">Max amount of Players.</param>
        /// <returns>Relay join code.</returns>
        public async Task<string> CreateRelay(int maxConnection)
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnection);
            _relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            _key = allocation.Key;
            _allocationIDBytes = allocation.AllocationIdBytes;
            _allocationID = allocation.AllocationId;
            _connectionData = allocation.ConnectionData;

            RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
            _ip = dtlsEndpoint.Host;
            _port = dtlsEndpoint.Port;

            _isHost = true;
            PlayerInfoManager.Instance.IsHost = true;

            return _relayJoinCode;
        }

        /// <summary>
        /// Join a relay and set the data to be used with NetCode for connecting to the relay endpoint.
        /// </summary>
        /// <param name="maxConnection">Max amount of Players.</param>
        /// <returns>Relay join code.</returns>
        public async Task<bool> JoinRelay(string relayJoinCode)
        {
            _relayJoinCode = relayJoinCode;
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(_relayJoinCode);
            _key = allocation.Key;
            _allocationIDBytes = allocation.AllocationIdBytes;
            _allocationID = allocation.AllocationId;
            _hostconnectionData = allocation.HostConnectionData;
            _connectionData = allocation.ConnectionData;

            RelayServerEndpoint dtlsEndpoint = allocation.ServerEndpoints.First(conn => conn.ConnectionType == "dtls");
            _ip = dtlsEndpoint.Host;
            _port = dtlsEndpoint.Port;
            return true;
        }

        /// <summary>
        /// Leaves relay and cleans up the variables to be used again.
        /// </summary>
        /// <returns></returns>
        public async Task LeaveRelay()
        {
            try
            {
                NetworkManager.Singleton.Shutdown(true);
            }
            catch (Exception e)
            {
                Debug.LogError("Something went wrong leaving the relay.");
                Debug.LogError(e);
            }
            
            _isHost = false;
            _relayJoinCode = default;
            _ip = default;
            _port = default;
            _key = default;
            _allocationID = default;
            _allocationIDBytes = default;
            _hostconnectionData = default;
            _connectionData = default;
            await Task.Delay(100);
        }

        public (byte[] allocationID, byte[] key, byte[] connectionData, string ip, int port) GetHostConnectionInfo()
        {
            return (_allocationIDBytes, _key, _connectionData, _ip, _port);
        }

        public (byte[] allocationID, byte[] key, byte[] connectionData, byte[] hostConnectionData, string ip, int port) GetClientConnectionInfo()
        {
            return (_allocationIDBytes, _key, _connectionData, _hostconnectionData, _ip, _port);
        }
    }
}
