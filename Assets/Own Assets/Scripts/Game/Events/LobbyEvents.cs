using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Lobbies.Models;

namespace Game.Events
{
    public static class LobbyEvents
    {
        public static UnityEvent<Lobby> E_NewLobbyData = new();
        public static UnityEvent E_LobbyUpdated = new();
        public static UnityEvent<bool> E_OnLobbyReadyChanged = new();
    }
}
