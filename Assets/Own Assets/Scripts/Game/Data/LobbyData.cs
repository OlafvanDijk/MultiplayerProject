using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

namespace Game.Data
{
    class LobbyData
    {
        private int _mapIndex;
        private string _difficulty;

        public int MapIndex => _mapIndex;

        public string Difficulty => _difficulty;

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
        }

        public Dictionary<string, string> Serialize()
        {
            return new Dictionary<string, string>()
            {
                {"MapIndex", _mapIndex.ToString() },
                {"Difficulty", _difficulty }
            };
        }
    }
}
