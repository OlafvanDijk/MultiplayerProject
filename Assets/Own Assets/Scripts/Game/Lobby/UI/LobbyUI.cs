using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Events;

namespace Game
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _codeField;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _readyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonText;
        [SerializeField] private ChooseMapUI _chooseMapUI;

        private bool _isReady;

        private void OnEnable()
        {
            _readyButton.onClick.AddListener(OnReadyPressed);
            _startGameButton.onClick.AddListener(OnStartPressed);
            if (GameLobbyManager.Instance.IsHost)
                LobbyEvents.E_OnLobbyReadyChanged.AddListener(OnLobbyReadyChanged);
        }

        private void Awake()
        {
            _startGameButton.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _readyButton.onClick.RemoveListener(OnReadyPressed);
            _startGameButton.onClick.RemoveListener(OnStartPressed);
            if (GameLobbyManager.Instance.IsHost)
                LobbyEvents.E_OnLobbyReadyChanged.RemoveListener(OnLobbyReadyChanged);
        }

        private void OnLobbyReadyChanged(bool ready)
        {
            _startGameButton.gameObject.SetActive(ready);
        }

        private void Start()
        {
            _chooseMapUI.Init();
            _codeField.text = $"Lobby Code: {GameLobbyManager.Instance.GetLobbyCode()}";
        }

        private async void OnReadyPressed()
        {
            bool succeed = await GameLobbyManager.Instance.SetPlayerReady();
            _isReady = !_isReady;
            _readyButtonText.text = _isReady ? "Not Ready" : "Ready";
        }

        private async void OnStartPressed()
        {
            await GameLobbyManager.Instance.StartGame(_chooseMapUI.ScenePath);
        }
    }
}
