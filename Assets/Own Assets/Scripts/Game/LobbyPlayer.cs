using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Game.Data;

namespace Game
{
    public class LobbyPlayer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameField;
        [SerializeField] private GameObject _readyObject;
        [SerializeField] private SwitchObjectActive switchObjectActive;

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

        private LobbyPlayerData _playerData;

        public void SetDataExternal(LobbyPlayerData playerData)
        {
            SetDataInternal(playerData, playerData.Name, playerData.IsReady, true, true);
        }

        public void PlayerLeft()
        {
            SetDataInternal(null, string.Empty, false, false, false);
        }

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