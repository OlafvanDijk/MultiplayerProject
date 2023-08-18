using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

namespace Game
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _codeField;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _readyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonText;


        private void OnEnable()
        {
            _readyButton.onClick.AddListener(OnReadyPressed);
        }

        private void OnDisable()
        {
            _readyButton.onClick.RemoveListener(OnReadyPressed);
        }

        private void Start()
        {
            _codeField.text = $"Lobby Code: {GameLobbyManager.Instance.GetLobbyCode()}";
        }

        private async void OnReadyPressed()
        {
            bool succeed = await GameLobbyManager.Instance.SetPlayerReady();
            if (succeed)
            {
                _readyButtonText.text = "Not Ready";
            }
        }
    }
}
