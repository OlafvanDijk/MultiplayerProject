using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Game.Data;

namespace Game
{
    public class LobbySpawner : MonoBehaviour
    {
        [SerializeField] private List<LobbyPlayer> _players;

        private void OnEnable()
        {
            Events.LobbyEvents.OnLobbyUpdated += OnLobbyUpdated;
        }

        private void OnDisable()
        {
            Events.LobbyEvents.OnLobbyUpdated -= OnLobbyUpdated;
        }

        private void OnLobbyUpdated()
        {
            List<LobbyPlayerData> playerData = GameLobbyManager.Instance.GetPlayers();
            for (int i = 0; i < playerData.Count; i++)
            {
                LobbyPlayerData data = playerData[i];
                _players[i].SetData(data);
            }
        }
    }
}
