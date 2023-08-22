using Game.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utility;
using Game.Managers;

namespace Game.GameLobby.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _codeField;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _readyButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private Button _copyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonText;
        [SerializeField] private ChooseMapUI _chooseMapUI;
        [SerializeField] SceneReference _mainMenu;

        private bool _isReady;

        private GameLobbyManager _gameLobbyManager;

        #region Unity Methods
        /// <summary>
        /// Set button listeners.
        /// When host listen for lobby ready change.
        /// </summary>
        private void OnEnable()
        {
            if (!_gameLobbyManager)
                _gameLobbyManager = GameLobbyManager.Instance;

            _readyButton.onClick.AddListener(OnReadyPressed);
            _startGameButton.onClick.AddListener(OnStartPressed);
            _leaveButton.onClick.AddListener(OnLeavePressed);
            _copyButton.onClick.AddListener(OnCopyPressed);

            if (_gameLobbyManager.IsHost)
                LobbyEvents.E_OnLobbyReadyChanged.AddListener(OnLobbyReadyChanged);
        }

        /// <summary>
        /// Remove all listeners.
        /// </summary>
        private void OnDisable()
        {
            _readyButton.onClick.RemoveListener(OnReadyPressed);
            _startGameButton.onClick.RemoveListener(OnStartPressed);
            _leaveButton.onClick.RemoveListener(OnLeavePressed);
            _copyButton.onClick.RemoveListener(OnCopyPressed);

            if (_gameLobbyManager.IsHost)
                LobbyEvents.E_OnLobbyReadyChanged.RemoveListener(OnLobbyReadyChanged);
        }

        /// <summary>
        /// Hide the Start Button show the code and Initialise the choose map script.
        /// </summary>
        private void Start()
        {
            _startGameButton.gameObject.SetActive(false);
            _chooseMapUI.Init();
            _codeField.text = $"Lobby Code: {_gameLobbyManager.GetLobbyCode()}";
        }
        #endregion

        #region Listener Methods
        #region Async
        /// <summary>
        /// Sets the player to the ready state.
        /// </summary>
        private async void OnReadyPressed()
        {
            bool succeed = await _gameLobbyManager.SetPlayerReady();
            if(!succeed)
            {
                Debug.LogError("Something went wrong when setting the player to ready.");
                return;
            }

            _isReady = !_isReady;
            _readyButtonText.text = _isReady ? "Not Ready" : "Ready";
        }

        /// <summary>
        /// Starts the game with the chosen maps' scenepath.
        /// </summary>
        private async void OnStartPressed()
        {
            await GameLobbyManager.Instance.StartGame(_chooseMapUI.ScenePath);
        }

        /// <summary>
        /// Leaves the lobby.
        /// </summary>
        private async void OnLeavePressed()
        {
            await GameLobbyManager.Instance.LeaveLobby();
            LoadScene.E_LoadScene.Invoke(_mainMenu);
        }
        #endregion

        /// <summary>
        /// Copies the lobby code to the clipboard.
        /// </summary>
        private void OnCopyPressed()
        {
            _gameLobbyManager.GetLobbyCode().CopyToClipBoard();
        }

        /// <summary>
        /// Shows the start game button based on the given bool.
        /// </summary>
        /// <param name="ready"></param>
        private void OnLobbyReadyChanged(bool ready)
        {
            _startGameButton.gameObject.SetActive(ready);
        }
        #endregion
    }
}
