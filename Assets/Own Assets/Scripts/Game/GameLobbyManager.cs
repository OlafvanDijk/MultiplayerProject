using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Game.Data;
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
            LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
        }

        private void OnDisable()
        {
            LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
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
            return await LobbyManager.Instance.CreateLobby(4, true, GetLobbyPlayerData(), GetLobbyData());
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

        public Tuple<int, string> GetLobbyMapData()
        {
            return new Tuple<int, string>(_lobbyData.MapIndex, _lobbyData.Difficulty);
        }

        /// <summary>
        /// Update local lobby data and lobby player data on lobby update.
        /// </summary>
        /// <param name="lobby">Updated lobby.</param>
        private void OnLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.Instance.GetPlayersData();
            _lobbyPlayerData.Clear();
            Debug.Log($"Amount of players: {playerData.Count}");
            string playerID = AuthenticationService.Instance.PlayerId;
            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new();
                lobbyPlayerData.Init(data);

                if (lobbyPlayerData.ID == playerID)
                {
                    _localLobbyPlayerData = lobbyPlayerData;
                }

                _lobbyPlayerData.Add(lobbyPlayerData);
            }
            _lobbyData = new LobbyData();
            _lobbyData.Initialize(lobby.Data);

            Events.LobbyEvents.OnLobbyUpdated?.Invoke();
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
