using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace Game.Data
{
    /// <summary>
    /// Data class holding a player's lobby data.
    /// </summary>
    public class LobbyPlayerData
    {
        private string _id;
        private string _name;
        private bool _isReady;

        public string ID => _id;
        public string Name => _name;
        public bool IsReady
        {
            get => _isReady;
            set => _isReady = value;
        }

        public void Init(string id, string name)
        {
            _id = id;
            _name = name;
        }

        public void Init(Dictionary<string, PlayerDataObject> playerData)
        {
            UpdateState(playerData);
        }

        public void UpdateState(Dictionary<string, PlayerDataObject> playerData)
        {
            if (playerData.ContainsKey("ID"))
            {
                _id = playerData["ID"].Value;
            }
            if (playerData.ContainsKey("Name"))
            {
                _name = playerData["Name"].Value;
            }
            if (playerData.ContainsKey("IsReady"))
            {
                _isReady = bool.Parse(playerData["IsReady"].Value);
            }

        }

        public Dictionary<string, string> Serialize()
        {
            return new Dictionary<string, string>()
            {
                {"ID", _id },
                {"Name", _name },
                {"IsReady", _isReady.ToString() }
            };
        }
    }
}
