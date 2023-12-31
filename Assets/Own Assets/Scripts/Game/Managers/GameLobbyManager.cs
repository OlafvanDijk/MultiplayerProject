using Game.Data;
using Game.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Utility;

namespace Game.Managers
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
        private readonly int _maxNumberOfPlayers = 4;

        private LobbyManager _lobbyManager;
        private RelayManager _relayManager;
        private PlayerInfoManager _playerInfoManager;

        private LobbyData _lobbyData;
        private LobbyPlayerData _localLobbyPlayerData;
        private List<LobbyPlayerData> _lobbyPlayerData = new();

        #region Unity Methods
        private void Awake()
        {
            _lobbyManager = LobbyManager.Instance;
            _relayManager = RelayManager.Instance;
            _playerInfoManager = PlayerInfoManager.Instance;
            _lobbyManager.E_ConnectionLost.AddListener(OnConnectionLost);
        }

        private void OnEnable()
        {
            LobbyEvents.E_NewLobbyData.AddListener(OnLobbyUpdated);
        }

        private void OnDisable()
        {
            LobbyEvents.E_NewLobbyData.RemoveListener(OnLobbyUpdated);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create a new lobby with player data.
        /// </summary>
        /// <returns>True if lobby has been created.</returns>
        public async Task<bool> CreateLobby()
        {
            return await _lobbyManager.CreateLobby(_maxNumberOfPlayers, true, GetLobbyPlayerData(), GetLobbyData());
        }

        /// <summary>
        /// Joins lobby with given code.
        /// </summary>
        /// <param name="code">Lobby Code.</param>
        /// <returns>True if lobby has been joined.</returns>
        public async Task<bool> JoinLobby(string code)
        {
            return await _lobbyManager.JoinLobby(code, GetLobbyPlayerData());
        }

        /// <summary>
        /// Creates a relay and puts the relay code in the lobbydata before updating the lobby data.
        /// Then it updates the player's data so they can connect to the relay.
        /// </summary>
        /// <param name="scenePath">Chosen map's scene path.</param>
        /// <returns></returns>
        public async Task StartGame(string scenePath)
        {
            string relayJoinCode = await _relayManager.CreateRelay(_maxNumberOfPlayers);
            _inGame = true;

            _lobbyData.RelayJoinCode = relayJoinCode;
            await _lobbyManager.UpdateLobbyData(_lobbyData.Serialize(), true);

            await UpdatePlayerDataOnJoinGame();
            LoadScene.E_LoadSceneWithPath.Invoke(scenePath);
        }

        /// <summary>
        /// Leaves the lobby and resets the lobby data and variables.
        /// </summary>
        /// <returns></returns>
        public async Task LeaveLobby()
        {
            await _lobbyManager.LeaveLobby();
            CleanUp();
        }

        /// <summary>
        /// Leaves the relay and then the lobby.
        /// </summary>
        /// <returns></returns>
        public async Task LeaveGame()
        {
            await _relayManager.LeaveRelay();
            await LeaveLobby();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            LoadScene.E_LoadSceneWithBuildIndex.Invoke(2);
        }

        /// <summary>
        /// Sets the selected map and updates the lobby data.
        /// </summary>
        /// <param name="mapIndex"></param>
        /// <param name="difficulty"></param>
        /// <returns></returns>
        public async Task<bool> SetSelectedMap(int mapIndex, string difficulty)
        {
            _lobbyData.Initialize(mapIndex, difficulty);
            return await _lobbyManager.UpdateLobbyData(_lobbyData.Serialize());
        }

        /// <summary>
        /// Set the local player to ready and update the player data.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SetPlayerReady()
        {
            _localLobbyPlayerData.IsReady = !_localLobbyPlayerData.IsReady;
            return await _lobbyManager.UpdatePlayerData(_localLobbyPlayerData.ID, _localLobbyPlayerData.Serialize());
        }

        public async Task<bool> SetPlayerCharacter(int index)
        {
            _localLobbyPlayerData.CharacterIndex = index;
            return await _lobbyManager.UpdatePlayerData(_localLobbyPlayerData.ID, _localLobbyPlayerData.Serialize());
        }

        /// <summary>
        /// Returns the lobby code.
        /// </summary>
        /// <returns>Lobby code.</returns>
        public string GetLobbyCode()
        {
            return _lobbyManager.GetLobbyCode();
        }

        /// <summary>
        /// Get all player data.
        /// </summary>
        /// <returns>List of lobby player data.</returns>
        public List<LobbyPlayerData> GetPlayers()
        {
            return _lobbyPlayerData;
        }

        /// <summary>
        /// Get the current lobby map data.
        /// </summary>
        /// <returns></returns>
        public Tuple<int, string> GetLobbyMapData()
        {
            return new Tuple<int, string>(_lobbyData.MapIndex, _lobbyData.Difficulty);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Update local lobby data and lobby player data on lobby update.
        /// </summary>
        /// <param name="lobby">Updated lobby.</param>
        private async void OnLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = _lobbyManager.GetPlayersData();
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

        /// <summary>
        /// Joins a relay server with the given relay code.
        /// </summary>
        /// <param name="relayJoinCode">Code of the relay to join. Works like a lobby code.</param>
        /// <returns></returns>
        private async Task JoinRelayServer(string relayJoinCode)
        {
            await _relayManager.JoinRelay(relayJoinCode);
            _inGame = true;
            await UpdatePlayerDataOnJoinGame();
        }

        /// <summary>
        /// Updates the player data so they know the relay's connection data.
        /// </summary>
        /// <returns></returns>
        private async Task UpdatePlayerDataOnJoinGame()
        {
            string allocationID = _relayManager.AllocationID;
            string connectionData = _relayManager.ConnectionData;
            await _lobbyManager.UpdatePlayerData(_localLobbyPlayerData.ID, _localLobbyPlayerData.Serialize(), allocationID, connectionData);
        }

        /// <summary>
        /// Checks if everyone in the lobby is ready.
        /// If so it will invoke the LobbyEvents.E_OnLobbyReadyChanged event.
        /// </summary>
        /// <param name="playersReady">Total of the players that are ready.</param>
        /// <param name="totalPlayerCount">Total players in the lobby.</param>
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

        /// <summary>
        /// Initializes the _locallobbyPlayerData variable and returns the serialized version.
        /// </summary>
        /// <returns>Serialized local lobby player data after initialization.</returns>
        private Dictionary<string, string> GetLobbyPlayerData()
        {
            _localLobbyPlayerData = new();
            _playerInfoManager.AuthPlayerID = AuthenticationService.Instance.PlayerId;
            _localLobbyPlayerData.Init(_playerInfoManager.AuthPlayerID, _playerInfoManager.Name);
            return _localLobbyPlayerData.Serialize();
        }

        /// <summary>
        /// Initializes the _lobbyData variable and returns the serialized version.
        /// </summary>
        /// <returns>Serialized local lobby data after initialization.</returns>
        private Dictionary<string, string> GetLobbyData()
        {
            _lobbyData = new LobbyData();
            _lobbyData.Initialize(0, "Easy");   //TODO Get default difficulty from somewhere
            return _lobbyData.Serialize();
        }

        /// <summary>
        /// On connection lost leave the game.
        /// </summary>
        private async void OnConnectionLost()
        {
            await LeaveGame();
        }

        /// <summary>
        /// Cleans up all game depending variables.
        /// </summary>
        private void CleanUp()
        {
            _playerInfoManager.CleanUp();
            _inGame = false;
            _lobbyReady = false;

            _lobbyData = default;
            _localLobbyPlayerData = default;
            _lobbyPlayerData = new();
        }
        #endregion
    }
}
