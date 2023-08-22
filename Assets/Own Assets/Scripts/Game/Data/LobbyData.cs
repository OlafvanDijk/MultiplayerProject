using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Game.Data
{

    /// <summary>
    /// Data class that holds the lobbies mapIndex, difficulty and the relayCode.
    /// The relaycode is only filled when the game has been started by the host.
    /// </summary>
    class LobbyData
    {
        private int _mapIndex;
        private string _difficulty;
        private string _relayJoinCode;

        public int MapIndex => _mapIndex;
        public string Difficulty => _difficulty;
        public string RelayJoinCode
        {
            get
            {
                return _relayJoinCode;
            }
            set
            {
                _relayJoinCode = value;
            }
        }

        public void Initialize(int mapIndex, string difficulty)
        {
            _mapIndex = mapIndex;
            _difficulty = difficulty;
        }

        public void Initialize(Dictionary<string, DataObject> lobbyData)
        {
            UpdateState(lobbyData);
        }

        public void UpdateState(Dictionary<string, DataObject> lobbyData)
        {
            if (lobbyData.ContainsKey("MapIndex"))
            {
                _mapIndex = Convert.ToInt32(lobbyData["MapIndex"].Value);
            }

            if (lobbyData.ContainsKey("Difficulty"))
            {
                _difficulty = lobbyData["Difficulty"].Value;
            }
            if (lobbyData.ContainsKey("Difficulty"))
            {
                _relayJoinCode = lobbyData["RelayJoinCode"].Value;
            }
        }

        public Dictionary<string, string> Serialize()
        {
            return new Dictionary<string, string>()
            {
                { "MapIndex", _mapIndex.ToString() },
                { "Difficulty", _difficulty },
                { "RelayJoinCode", _relayJoinCode }
            };
        }
    }
}
