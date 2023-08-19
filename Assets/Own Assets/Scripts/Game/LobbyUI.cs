using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        }

        private void OnDisable()
        {
            _readyButton.onClick.RemoveListener(OnReadyPressed);
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
    }
}
