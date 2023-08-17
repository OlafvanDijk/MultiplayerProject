using Unity.Services.Lobbies.Models;

public static class LobbyEvents
{
    public delegate void LobbyUpdated(Lobby lobby);
    public static LobbyUpdated OnLobbyUpdated;
}
