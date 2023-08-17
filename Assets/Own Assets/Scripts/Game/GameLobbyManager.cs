using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;

namespace Game
{
    public class GameLobbyManager : Singleton<GameLobbyManager>
    {
        private PlayerInfoManager _playerInfoManager;

        private List<LobbyPlayerData> _lobbyPlayerData = new();

        private LobbyPlayerData _localLobbyPlayerData;

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
            LobbyPlayerData playerData = new();
            playerData.Init(AuthenticationService.Instance.PlayerId, "HostPlayer");
            //Dictionary<string, string> playerData = new()
            //{
            //    { _playerInfoManager.Name, "HostPlayer" }
            //};
            return await LobbyManager.Instance.CreateLobby(4, true, playerData.Serialize());
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
            LobbyPlayerData playerData = new();
            playerData.Init(AuthenticationService.Instance.PlayerId, "HostPlayer");
            return await LobbyManager.Instance.JoinLobby(code, playerData.Serialize());
        }

        /// <summary>
        /// update local lobby data on lobby update.
        /// </summary>
        /// <param name="lobby">Updated lobby.</param>
        private void OnLobbyUpdated(Lobby lobby)
        {
            List<Dictionary<string, PlayerDataObject>> playerData = LobbyManager.Instance.GetPlayersData();
            _lobbyPlayerData.Clear();
            foreach (Dictionary<string, PlayerDataObject> data in playerData)
            {
                LobbyPlayerData lobbyPlayerData = new();
                lobbyPlayerData.Init(data);

                if (lobbyPlayerData.ID == AuthenticationService.Instance.PlayerId)
                {
                    _localLobbyPlayerData = lobbyPlayerData;
                }

                _lobbyPlayerData.Add(lobbyPlayerData);
            }

            Events.LobbyEvents.OnLobbyUpdated?.Invoke();
        }
    }
}
