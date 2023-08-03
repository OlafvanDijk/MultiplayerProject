using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    [SerializeField] private GameObject _chat;
    [SerializeField] private GameObject _logPrefab;
    [SerializeField] private Transform _logParent;
    [SerializeField] private Scrollbar _scrollbar;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private InputActionReference _submitAction;
    [SerializeField] private InputActionReference _cancelAction;
    [SerializeField] private int _maxMessages = 20;

    public static UnityEvent<string, bool, bool> E_SendMessage = new();

    private bool _chatShown;

    List<GameObject> messages = new();

    #region AddListeners
    private void Awake()
    {
        E_SendMessage.AddListener(ShowMessage); //TODO check in between if message should only be local or for everyone
    }

    private void OnEnable()
    {
        _submitAction.action.performed += PressedSubmit;
    }

    private void OnDisable()
    {
        _submitAction.action.performed -= PressedSubmit;
    }
    #endregion

    #region Action Listeners
    private void PressedSubmit(InputAction.CallbackContext callback)
    {
        if (_chatShown)
        {
            if (!_inputField.isFocused)
                return;

            //TODO sent message via host to all players
            string input = _inputField.text.Trim();
            if (input.Length <= 0)
                return;
            _inputField.text = string.Empty;
            ShowMessage(input, true);
        } else
        {
            ShowChat(true);
        }
    }

    private void PressedCancel(InputAction.CallbackContext callback)
    {
        ShowChat(false);
    }
    #endregion

    private void ShowChat(bool show)
    {
        if (show)
        {
            _chat.SetActive(true);
            _cancelAction.action.performed += PressedCancel;
            SelectInput();
            _scrollbar.value = 0f;
            _chatShown = true;
        }
        else
        {
            _cancelAction.action.performed -= PressedCancel;
            _chat.SetActive(false);
            _chatShown = false;
        }
    }

    public void ShowMessage(string message, bool select = false, bool showChat = true)
    {
        if (showChat && !_chatShown)
            ShowChat(true);

        GameObject messageObject = Instantiate(_logPrefab, _logParent);
        TextMeshProUGUI chatMessage = messageObject.GetComponent<TextMeshProUGUI>();
        TimeSpan timeNow = DateTime.Now.TimeOfDay;
        chatMessage.text = $"[{string.Format("{0:d2}", timeNow.Hours)}:{string.Format("{0:d2}", timeNow.Minutes)}:{string.Format("{0:d2}", timeNow.Seconds)}] Player: {message}";
        messages.Add(messageObject);
        messageObject.SetActive(true);

        if (messages.Count > 20)
        {
            GameObject toDestroy = messages[0];
            messages.RemoveAt(0);
            Destroy(toDestroy);
        }

        if (select)
            SelectInput();
    }

    private void SelectInput()
    {
        _inputField.ActivateInputField();
        _inputField.Select();
    }
}
