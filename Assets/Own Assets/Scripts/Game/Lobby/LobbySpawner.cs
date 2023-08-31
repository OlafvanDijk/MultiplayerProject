using Game.Data;
using Game.Events;
using Game.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class LobbySpawner : MonoBehaviour
    {
        [SerializeField] private List<LobbyPlayer> _players;

        private Dictionary<string, LobbyPlayer> _playersJoined = new();

        private void OnEnable()
        {
            LobbyEvents.E_LobbyUpdated.AddListener(OnLobbyUpdated);
        }

        private void OnDisable()
        {
            LobbyEvents.E_LobbyUpdated.RemoveListener(OnLobbyUpdated);
        }

        /// <summary>
        /// Updates the LobbyPlayers based on the LobbyPlayerData list after a lobby has been updated.
        /// </summary>
        private void OnLobbyUpdated()
        {
            List<LobbyPlayerData> playerData = GameLobbyManager.Instance.GetPlayers();
            CheckPlayersLeft(playerData);

            foreach (LobbyPlayerData data in playerData)
            {
                string ID = data.ID;
                if (_playersJoined.ContainsKey(ID))
                {
                    _playersJoined[ID].SetDataExternal(data);
                }
                else
                {
                    LobbyPlayer player = _players.Find(p => !p.ID.Equals(ID) && !_playersJoined.ContainsKey(p.ID));
                    _playersJoined.Add(ID, player);
                    player.SetDataExternal(data);
                    if (player.ID == PlayerInfoManager.Instance.AuthPlayerID)
                        player.EnableCharacterSelect();
                }
            }
        }

        /// <summary>
        /// Checks if a player has left. If so it will reset their LobbyPlayer.
        /// </summary>
        /// <param name="playerData">List of all current players.</param>
        private void CheckPlayersLeft(List<LobbyPlayerData> playerData)
        {
            if (_playersJoined.Count == 0)
                return;

            Dictionary<string, LobbyPlayer> playersToRemove = new();

            foreach (KeyValuePair<string, LobbyPlayer> player in _playersJoined)
            {
                if (playerData.Exists(pd => player.Key.Equals(pd.ID)))
                    continue;
                playersToRemove.Add(player.Key, player.Value);
            }

            if (playersToRemove.Count == 0)
                return;

            foreach (KeyValuePair<string, LobbyPlayer> player in playersToRemove)
            {
                player.Value.PlayerLeft();
                Debug.Log($"{player.Value.Name} Left");
                _playersJoined.Remove(player.Key);
            }
        }
    }
}
