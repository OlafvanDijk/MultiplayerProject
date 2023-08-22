using Game.Data;
using TMPro;
using UnityEngine;

namespace Game
{
    public class LobbyPlayer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameField;
        [SerializeField] private GameObject _readyObject;
        [SerializeField] private SwitchObjectActive switchObjectActive;

        private LobbyPlayerData _playerData;

        public string ID
        {
            get
            {
                if (_playerData != null)
                    return _playerData.ID;
                return string.Empty;
            }
        }

        public string Name
        {
            get
            {
                if (_playerData != null)
                    return _playerData.Name;
                return string.Empty;
            }
        }
               
        /// <summary>
        /// Set the data for the joined player
        /// </summary>
        /// <param name="playerData"></param>
        public void SetDataExternal(LobbyPlayerData playerData)
        {
            SetDataInternal(playerData, playerData.Name, playerData.IsReady, true, true);
        }

        /// <summary>
        /// Reset the data for the player that left.
        /// </summary>
        public void PlayerLeft()
        {
            SetDataInternal(null, string.Empty, false, false, false);
        }

        /// <summary>
        /// Sets all fields.
        /// </summary>
        /// <param name="playerData"></param>
        /// <param name="name"></param>
        /// <param name="isReady"></param>
        /// <param name="showReady"></param>
        /// <param name="showPlayer"></param>
        private void SetDataInternal(LobbyPlayerData playerData, string name, bool isReady, bool showReady, bool showPlayer)
        {
            _playerData = playerData;
            _nameField.text = name;

            switchObjectActive.Activate(isReady);
            _readyObject.SetActive(showReady);
            gameObject.SetActive(showPlayer);
        }
    }
}