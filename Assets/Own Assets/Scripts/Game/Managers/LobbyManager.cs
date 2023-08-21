using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System;
using Game.Events;

public class LobbyManager : Singleton<LobbyManager>
{
    private Lobby _lobby;
    private Player _player;

    public bool IsHost(string ID)
    {
        if (_lobby == null)
            return false;

        return _lobby.HostId == ID;
    }

    /// <summary>
    /// Creates a lobby. If succeeded it will start a heartbeat Coroutine to keep the lobby active.
    /// Also checks for new lobby updates every second.
    /// </summary>
    /// <param name="maxPlayers">Maximum players.</param>
    /// <param name="isPrivate">Should the lobby not be visible when searching.</param>
    /// <param name="lobbyPlayerData">Player Data.</param>
    /// <returns></returns>
    public async Task<bool> CreateLobby(int maxPlayers, bool isPrivate, Dictionary<string, string> lobbyPlayerData, Dictionary<string, string> lobbyData)
    {
        Dictionary<string, PlayerDataObject> playerData = SerlializePlayerData(lobbyPlayerData);
        _player = new Player(AuthenticationService.Instance.PlayerId, null, playerData);

        CreateLobbyOptions lobbyOptions = new CreateLobbyOptions()
        {
            Data = SerializeLobbyData(lobbyData),
            IsPrivate = isPrivate,
            Player = _player
        };

        try
        {
            _lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", maxPlayers, lobbyOptions);
        }
        catch (Exception e)
        {
            Debug.LogError($"Lobby could not be created");
            Debug.LogError(e.Message);
            return false;
        }
        
        Debug.Log($"Lobby created with ID: {_lobby.Id}");

        StartCoroutine(HeartbeatLobbyCoroutine(6f));
        StartCoroutine(RefreshLobbyCoroutine(1f));
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
        _player = new Player(AuthenticationService.Instance.PlayerId, null, SerlializePlayerData(data));
        options.Player = _player;
        
        try
        {
            _lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
        }
        catch (Exception e)
        {
            Debug.LogError($"Lobby could not be joined");
            Debug.LogError(e.Message);
            return false;
        }

        StartCoroutine(RefreshLobbyCoroutine(1f));
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
    /// Updates player data.
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public async Task<bool> UpdatePlayerData(string playerID, Dictionary<string, string> data, string allocationID = default, string connectionData = default)
    {
        Dictionary<string, PlayerDataObject> playerData = SerlializePlayerData(data);
        UpdatePlayerOptions options = new()
        {
            Data = playerData,
            AllocationId = allocationID,
            ConnectionInfo = connectionData
        };

        try
        {
            _lobby = await LobbyService.Instance.UpdatePlayerAsync(_lobby.Id, playerID, options);
        }
        catch (Exception)
        {
            return false;
        }

        LobbyEvents.E_NewLobbyData?.Invoke(_lobby);
        return true;
    }

    /// <summary>
    /// Updates lobby data.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public async Task<bool> UpdateLobbyData(Dictionary<string, string> data)
    {
        Dictionary<string, DataObject> lobbyData = SerializeLobbyData(data);
        UpdateLobbyOptions options = new()
        {
            Data = lobbyData
        };

        try
        {
            _lobby = await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, options);
        }
        catch (Exception e)
        {
            Debug.LogError("Updating lobby data went wrong");
            Debug.LogError(e.Message);
            return false;
        }

        LobbyEvents.E_NewLobbyData?.Invoke(_lobby);
        return true;
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
            playerData.Add(key, new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member, value: value));
        }
        return playerData;
    }

    /// <summary>
    /// Transforms string dictionary to usable dictionary.
    /// </summary>
    /// <param name="data">Lobby data in string form.</param>
    /// <returns>Lobby data with a usable format.</returns>
    private Dictionary<string, DataObject> SerializeLobbyData(Dictionary<string, string> data)
    {
        Dictionary<string, DataObject> lobbyData = new();
        foreach ((string key, string value) in data)
        {
            lobbyData.Add(key, new DataObject(visibility: DataObject.VisibilityOptions.Member, value: value));
        }
        return lobbyData;
    }

    /// <summary>
    /// Deletes lobby on quiting.
    /// </summary>
    public async void OnApplicationQuit()
    {
        if (_lobby != null && _lobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
        }
    }

    /// <summary>
    /// Sends a Heartbeat ping at the given interval.
    /// </summary>
    /// <param name="lobbyID">Lobby to ping.</param>
    /// <param name="interval">Interval in seconds.</param>
    /// <returns></returns>
    private IEnumerator HeartbeatLobbyCoroutine(float interval)
    {
        while (true)
        {
            //Debug.Log("Heartbeat");
            LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    /// <summary>
    /// Refreshes lobby data at given interval.
    /// </summary>
    /// <param name="lobbyID">Lobby to update.</param>
    /// <param name="interval">Interval in seconds.</param>
    /// <returns></returns>
    private IEnumerator RefreshLobbyCoroutine(float interval)
    {
        while (true)
        {
            Task<Lobby> task = LobbyService.Instance.GetLobbyAsync(_lobby.Id);
            yield return new WaitUntil(() => task.IsCompleted);

            //Debug.Log("Refresh");
            Lobby newLobby = task.Result;
            if (newLobby.LastUpdated > _lobby.LastUpdated)
            {
                _lobby = newLobby;
                LobbyEvents.E_NewLobbyData?.Invoke(_lobby);
            }

            yield return new WaitForSecondsRealtime(interval);
        }
    }
}