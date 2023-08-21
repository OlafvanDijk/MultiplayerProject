using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Game.Data;
using Game.Events;
using UnityEngine;

namespace Game
{
    public class GameLobbyManager : Singleton<GameLobbyManager>
    {
        public bool IsHost
        {
            get
            {
                if (_localLobbyPlayerData == null)
                    return false;
                return LobbyManager.Instance.IsHost(_localLobbyPlayerData.ID);
            }
        }

        private bool _inGame;
        private bool _lobbyReady;
        private int _maxNumberOfPlayers = 4;

        private PlayerInfoManager _playerInfoManager;

        private LobbyData _lobbyData;
        private LobbyPlayerData _localLobbyPlayerData;
        private List<LobbyPlayerData> _lobbyPlayerData = new();

        private void Awake()
        {
            _playerInfoManager = PlayerInfoManager.Instance;
        }

        private void OnEnable()
        {
            LobbyEvents.E_NewLobbyData.AddListener(OnLobbyUpdated);
        }

        private void OnDisable()
        {
            LobbyEvents.E_NewLobbyData.RemoveListener(OnLobbyUpdated);
        }

        /// <summary>
        /// Create a new lobby with player data.
        /// </summary>
        /// <returns>True if lobby has been created.</returns>
        public async Task<bool> CreateLobby()
        {
            //Dictionary<string, string> playerData = new()
            //{
            //    { _playerInfoManager.Name, "HostPlayer" }
            //};
            return await LobbyManager.Instance.CreateLobby(_maxNumberOfPlayers, true, GetLobbyPlayerData(), GetLobbyData());
        }

        /// <summary>
        /// Joins lobby with given code.
        /// </summary>
        /// <param name="code">Lobby Code.</param>
        /// <returns>True if lobby has been joined.</returns>
        public async Task<bool> JoinLobby(string code)
        {
            //Dictionary<string, string> playerData = new()
            //{
            //    { _playerInfoManager.Name, "HostPlayer" }
            //};
            return await LobbyManager.Instance.JoinLobby(code, GetLobbyPlayerData());
        }

        public async Task StartGame(string scenePath)
        {
            string relayJoinCode = await RelayManager.Instance.CreateRelay(_maxNumberOfPlayers);
            _inGame = true;

            _lobbyData.RelayJoinCode = relayJoinCode;
            await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize());

            await UpdatePlayerDataOnJoinGame();
            LoadScene.E_LoadSceneWithPath.Invoke(scenePath);
        }

        private async Task<bool> JoinRelayServer(string relayJoinCode)
        {
            await RelayManager.Instance.JoinRelay(relayJoinCode);
            _inGame = true;
            await UpdatePlayerDataOnJoinGame();
            return true;
        }

        private async Task UpdatePlayerDataOnJoinGame()
        {
            string allocationID = RelayManager.Instance.AllocationID;
            string connectionData = RelayManager.Instance.ConnectionData;
            await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.ID, _localLobbyPlayerData.Serialize(), allocationID, connectionData);
        }

        public Tuple<int, string> GetLobbyMapData()
        {
            return new Tuple<int, string>(_lobbyData.MapIndex, _lobbyData.Difficulty);
        }

        /// <summary>
        /// Update local lobby data and lobby player data on lobby update.
        /// </summary>
        /// <param name="lobby">Updated lobby.</param>
        private async void OnLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.Instance.GetPlayersData();
            _lobbyPlayerData.Clear();
            Debug.Log($"Amount of players: {playerData.Count}");
            string playerID = AuthenticationService.Instance.PlayerId;

            int playersReady = 0;

            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new();
                lobbyPlayerData.Init(data);

                if (lobbyPlayerData.ID == playerID)
                {
                    _localLobbyPlayerData = lobbyPlayerData;
                }

                if (lobbyPlayerData.IsReady)
                    playersReady++;

                _lobbyPlayerData.Add(lobbyPlayerData);
            }
            _lobbyData = new LobbyData();
            _lobbyData.Initialize(lobby.Data);

            LobbyEvents.E_LobbyUpdated?.Invoke();

            if (_inGame)
                return;

            CheckLobbyReady(playersReady, playerData.Count);

            if (_lobbyData.RelayJoinCode != default)
            {
                await JoinRelayServer(_lobbyData.RelayJoinCode);
                LoadScene.E_LoadSceneWithMapIndex.Invoke(_lobbyData.MapIndex);
            }
        }

        private void CheckLobbyReady(int playersReady, int totalPlayerCount)
        {
            if (playersReady == totalPlayerCount)
            {
                if (!_lobbyReady)
                {
                    _lobbyReady = true;
                    LobbyEvents.E_OnLobbyReadyChanged?.Invoke(true);
                }
            }
            else
            {
                _lobbyReady = false;
                LobbyEvents.E_OnLobbyReadyChanged?.Invoke(false);
            }
        }

        public async Task<bool> SetSelectedMap(int mapIndex, string difficulty)
        {
            _lobbyData.Initialize(mapIndex, difficulty);
            return await LobbyManager.Instance.UpdateLobbyData(_lobbyData.Serialize());
        }

        /// <summary>
        /// Returns the lobby code.
        /// </summary>
        /// <returns>Lobby code.</returns>
        public string GetLobbyCode()
        {
            return LobbyManager.Instance.GetLobbyCode();
        }

        /// <summary>
        /// Get all player data.
        /// </summary>
        /// <returns>List of lobby player data.</returns>
        public List<LobbyPlayerData> GetPlayers()
        {
            return _lobbyPlayerData;
        }

        public async Task<bool> SetPlayerReady()
        {
            _localLobbyPlayerData.IsReady = !_localLobbyPlayerData.IsReady;
            return await LobbyManager.Instance.UpdatePlayerData(_localLobbyPlayerData.ID, _localLobbyPlayerData.Serialize());
        }

        private Dictionary<string, string> GetLobbyPlayerData()
        {
            _localLobbyPlayerData = new();
            _localLobbyPlayerData.Init(AuthenticationService.Instance.PlayerId, _playerInfoManager.Name);
            return _localLobbyPlayerData.Serialize();
        }

        private Dictionary<string, string> GetLobbyData()
        {
            _lobbyData = new LobbyData();
            _lobbyData.Initialize(0, "Easy");   //TODO Get default difficulty from somewhere
            return _lobbyData.Serialize();
        }
    }
}
