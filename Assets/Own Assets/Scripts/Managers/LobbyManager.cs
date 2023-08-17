using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System;

public class LobbyManager : Singleton<LobbyManager>
{
    private Lobby _lobby;
    private Coroutine _heartbeatCoroutine;
    private Coroutine _refreshCoroutine;

    /// <summary>
    /// Creates a lobby. If succeeded it will start a heartbeat Coroutine to keep the lobby active.
    /// Also checks for new lobby updates every second.
    /// </summary>
    /// <param name="maxPlayers">Maximum players.</param>
    /// <param name="isPrivate">Should the lobby not be visible when searching.</param>
    /// <param name="data">Player Data.</param>
    /// <returns></returns>
    public async Task<bool> CreateLobby(int maxPlayers, bool isPrivate, Dictionary<string, string> data)
    {
        Dictionary<string, PlayerDataObject> playerData = SerlializePlayerData(data);
        Player player = new Player(AuthenticationService.Instance.PlayerId, null, playerData);

        CreateLobbyOptions lobbyOptions = new CreateLobbyOptions()
        {
            IsPrivate = isPrivate,
            Player = player
        };

        try
        {
            _lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", maxPlayers, lobbyOptions);
        }
        catch (Exception e)
        {
            Debug.Log($"Lobby could not be created");
            Debug.LogError(e.Message);
            return false;
        }
        
        Debug.Log($"Lobby created with ID: {_lobby.Id}");

        _heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(_lobby.Id, 6f));
        _refreshCoroutine = StartCoroutine(RefreshLobbyCoroutine(_lobby.Id, 1f));
        return true;
    }

    /// <summary>
    /// Tries to join a lobby with the given code.
    /// Tries to refresh lobby data every second.
    /// </summary>
    /// <param name="code">Lobby Code.</param>
    /// <param name="data">Player Data.</param>
    /// <returns></returns>
    public async Task<bool> JoinLobby(string code, Dictionary<string, string> data)
    {
        JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();
        Player player = new Player(AuthenticationService.Instance.PlayerId, null, SerlializePlayerData(data));
        options.Player = player;
        
        try
        {
            _lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
        }
        catch (Exception e)
        {
            Debug.Log($"Lobby could not be joined");
            Debug.LogError(e.Message);
            return false;
        }

        StartCoroutine(RefreshLobbyCoroutine(_lobby.Id, 1f));
        return true;
    }

    /// <summary>
    /// Get all data from lobby players.
    /// </summary>
    /// <returns>List of player data.</returns>
    public List<Dictionary<string, PlayerDataObject>> GetPlayersData()
    {
        List<Dictionary<string, PlayerDataObject>> data = new();
        foreach (Player player in _lobby.Players)
        {
            data.Add(player.Data);
        }
        return data;
    }

    /// <summary>
    /// Returns the lobby code.
    /// </summary>
    /// <returns>Code of the current lobby.</returns>
    public string GetLobbyCode()
    {
        return _lobby?.LobbyCode;
    }

    /// <summary>
    /// Transforms string dictionary to usable dictionary.
    /// </summary>
    /// <param name="data">Player data in string form.</param>
    /// <returns>Player data with a usable format.</returns>
    private Dictionary<string, PlayerDataObject> SerlializePlayerData(Dictionary<string, string> data)
    {
        Dictionary<string, PlayerDataObject> playerData = new();
        foreach ((string key, string value) in data)
        {
            Debug.Log("Name is " + key);
            playerData.Add(key, new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: value));
        }
        return playerData;
    }

    /// <summary>
    /// Deletes lobby on quiting.
    /// </summary>
    public void OnApplicationQuit()
    {
        if (_lobby != null && _lobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
        }
    }

    /// <summary>
    /// Sends a Heartbeat ping at the given interval.
    /// </summary>
    /// <param name="lobbyID">Lobby to ping.</param>
    /// <param name="interval">Interval in seconds.</param>
    /// <returns></returns>
    private IEnumerator HeartbeatLobbyCoroutine(string lobbyID, float interval)
    {
        while (true)
        {
            Debug.Log("Heartbeat");
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyID);
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    /// <summary>
    /// Refreshes lobby data at given interval.
    /// </summary>
    /// <param name="lobbyID">Lobby to update.</param>
    /// <param name="interval">Interval in seconds.</param>
    /// <returns></returns>
    private IEnumerator RefreshLobbyCoroutine(string lobbyID, float interval)
    {
        while (true)
        {
            Debug.Log("Refresh");
            Task<Lobby> task = LobbyService.Instance.GetLobbyAsync(lobbyID);
            yield return new WaitUntil(() => task.IsCompleted);

            Lobby newLobby = task.Result;
            if (newLobby.LastUpdated > _lobby.LastUpdated)
            {
                _lobby = newLobby;
                LobbyEvents.OnLobbyUpdated?.Invoke(_lobby);
            }

            yield return new WaitForSecondsRealtime(interval);
        }
    }
}
