using UnityEngine.Events;
using Unity.Services.Lobbies.Models;

namespace Game.Events
{
    /// <summary>
    /// All Lobby Events
    /// </summary>
    public static class LobbyEvents
    {
        public static UnityEvent<Lobby> E_NewLobbyData = new();
        public static UnityEvent E_LobbyUpdated = new();
        public static UnityEvent<bool> E_OnLobbyReadyChanged = new();
    }
}
