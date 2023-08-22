using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.GameLobby.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Main Menu")]
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private TextMeshProUGUI _nameError;
        [SerializeField] private TMP_InputField _nameInputField;

        [Header("Join Lobby")]
        [SerializeField] private GameObject _joinMenu;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _lobbyError;
        [SerializeField] private TMP_InputField _codeInputField;

        [Header("Next Scene")]
        [SerializeField] private SceneReference _lobbyScene;

        #region Unity Methods
        /// <summary>
        /// Add button listeners.
        /// </summary>
        private void OnEnable()
        {
            _hostButton.onClick.AddListener(OnHostClicked);
            _clientButton.onClick.AddListener(OnClientClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);
            _backButton.onClick.AddListener(OnBackClicked);
        }

        /// <summary>
        /// Remove button listeners.
        /// </summary>
        private void OnDisable()
        {
            _hostButton.onClick.RemoveListener(OnHostClicked);
            _clientButton.onClick.RemoveListener(OnClientClicked);
            _joinButton.onClick.RemoveListener(OnJoinClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);
        }
        #endregion

        #region Button Click Methods
        #region Async
        /// <summary>
        /// Creates a lobby. On succeed progresses into the lobby scene.
        /// </summary>
        private async void OnHostClicked()
        {
            if (!CheckName())
                return;
            bool succeeded = await GameLobbyManager.Instance.CreateLobby();
            if (succeeded)
                LoadScene.E_LoadScene.Invoke(_lobbyScene);
        }

        /// <summary>
        /// Tries to join the lobby with a code that was put into the code field.
        /// On succeed progresses into the lobby scene.
        /// </summary>
        private async void OnJoinClicked()
        {
            string code = _codeInputField.text;
            bool succeeded = await GameLobbyManager.Instance.JoinLobby(code);
            if (succeeded)
                LoadScene.E_LoadScene.Invoke(_lobbyScene);
        }
        #endregion

        /// <summary>
        /// Opens the join menu.
        /// </summary>
        private void OnClientClicked()
        {
            if (!CheckName())
                return;

            _joinMenu.SetActive(true);
            _mainMenu.SetActive(false);
        }

        /// <summary>
        /// Opens the main menu again.
        /// </summary>
        private void OnBackClicked()
        {
            if (!CheckName())
                return;

            _joinMenu.SetActive(false);
            _mainMenu.SetActive(true);
        }
        #endregion

        /// <summary>
        /// Check if name has been filled in.
        /// </summary>
        /// <returns></returns>
        private bool CheckName()
        {
            if (_nameInputField.text.Trim().Length == 0)
            {
                _nameError.text = "Please fill in a name.";
                _nameError.gameObject.SetActive(true);
                return false;
            }

            PlayerInfoManager.Instance.SetName(_nameInputField.text);
            return true;
        }
    }
}
