using Game.Managers;
using Game.UI.Messaging;
using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Script used for a the simple multiplayer scene to test out the netcode part only.
    /// </summary>
    public class SimpleMultiplayerUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private TextMeshProUGUI _fillInNameError;

        [SerializeField] private Chat _chat;

        [SerializeField] private TMP_InputField _nameInputField;

        private void Awake()
        {
            _hostButton.onClick.AddListener(() => StartGame(false));
            _clientButton.onClick.AddListener(() => StartGame(true));
        }

        public void StartGame(bool joinHost)
        {
            if (!CheckName())
                return;

            try
            {
                if (joinHost)
                    NetworkManager.Singleton.StartClient();
                else
                    NetworkManager.Singleton.StartHost();
                _chat.IsActive = true;
                Destroy(gameObject);
            }
            catch (Exception e)
            {
                ShowError(e.Message);
            }
        }

        private bool CheckName()
        {
            if (_nameInputField.text.Trim().Length == 0)
            {
                ShowError("Please fill in a name.");
                return false;
            }

            PlayerInfoManager.Instance.SetName(_nameInputField.text);
            return true;
        }

        private void ShowError(string message)
        {
            _fillInNameError.text = message;
            _fillInNameError.gameObject.SetActive(true);
        }
    }
}