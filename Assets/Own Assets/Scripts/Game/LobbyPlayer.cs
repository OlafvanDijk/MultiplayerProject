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
       
        private LobbyPlayerData _playerData;

        public void SetData(LobbyPlayerData playerData)
        {
            _playerData = playerData;
            _nameField.text = playerData.Name;

            switchObjectActive.Activate(_playerData.IsReady);
            _readyObject.SetActive(true);
            gameObject.SetActive(true);
        }
    }
}