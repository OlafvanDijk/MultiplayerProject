using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Events;
using System;

namespace Game
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _codeField;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _readyButton;
        [SerializeField] private Button _copyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonText;
        [SerializeField] private ChooseMapUI _chooseMapUI;

        private bool _isReady;

        private GameLobbyManager _gameLobbyManager;

        private void OnEnable()
        {
            if (!_gameLobbyManager)
                _gameLobbyManager = GameLobbyManager.Instance;
            _readyButton.onClick.AddListener(OnReadyPressed);
            _startGameButton.onClick.AddListener(OnStartPressed);
            _copyButton.onClick.AddListener(CopyLobbyCode);
            if (_gameLobbyManager.IsHost)
                LobbyEvents.E_OnLobbyReadyChanged.AddListener(OnLobbyReadyChanged);
        }

        private void CopyLobbyCode()
        {
            _gameLobbyManager.GetLobbyCode().CopyToClipBoard();
        }

        private void Awake()
        {
            _startGameButton.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _readyButton.onClick.RemoveListener(OnReadyPressed);
            _startGameButton.onClick.RemoveListener(OnStartPressed);
            if (_gameLobbyManager.IsHost)
                LobbyEvents.E_OnLobbyReadyChanged.RemoveListener(OnLobbyReadyChanged);
        }

        private void OnLobbyReadyChanged(bool ready)
        {
            _startGameButton.gameObject.SetActive(ready);
        }

        private void Start()
        {
            _chooseMapUI.Init();
            _codeField.text = $"Lobby Code: {_gameLobbyManager.GetLobbyCode()}";
        }

        private async void OnReadyPressed()
        {
            bool succeed = await _gameLobbyManager.SetPlayerReady();
            _isReady = !_isReady;
            _readyButtonText.text = _isReady ? "Not Ready" : "Ready";
        }

        private async void OnStartPressed()
        {
            await GameLobbyManager.Instance.StartGame(_chooseMapUI.ScenePath);
        }
    }
}
