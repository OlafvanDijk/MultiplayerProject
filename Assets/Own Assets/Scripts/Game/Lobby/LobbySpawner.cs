using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Events;

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
                }
            }
        }

        private void CheckPlayersLeft(List<LobbyPlayerData> playerData)
        {
            if (_playersJoined.Count == 0)
                return;

            List<string> playersToRemove = new();
            foreach ((string ID, LobbyPlayer player) in _playersJoined)
            {
                if (playerData.Find(pd => pd.ID.Equals(ID)) != null)
                    return;
                playersToRemove.Add(ID);
                Debug.Log($"Player {player.Name} left");
            }

            foreach (string ID in playersToRemove)
            {
                _playersJoined[ID].PlayerLeft();
                _playersJoined.Remove(ID);
            }
        }
    }
}
