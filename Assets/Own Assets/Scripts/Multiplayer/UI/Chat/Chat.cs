using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using UnityEngine.UI;
using Unity.Netcode;
using Game;
public class Chat : NetworkBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _submitAction;
    [SerializeField] private InputActionReference _cancelAction;

    [Header("Chat References")]
    [SerializeField] private GameObject _chatMainObject;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private GameObject _messagePrefab;
    [SerializeField] private GameObject _chatPrefab;
    [SerializeField] private Transform _chatParent;
    [SerializeField] private GameObject _tabButtonPrefab;
    [SerializeField] private Transform _tabButtonParent;
    [SerializeField] private List<ChatTab> tabs;

    public bool IsActive;

    public static UnityEvent<string, EMessageType> E_SendMessage = new();

    private bool _chatShown;
    private ChatTab _currentTab;

    private NetworkVariable<ChatMessage> _message = new();

    private PlayerInfoManager _playerInfoManager;

    #region Add Listeners
    public override void OnNetworkSpawn()
    {
        _playerInfoManager = PlayerInfoManager.Instance;
        GenerateTabs();
        E_SendMessage.AddListener(OnSendMessage);
        _message.OnValueChanged += OnMessageChanged;
    }

    public override void OnNetworkDespawn()
    {
        E_SendMessage.RemoveListener(OnSendMessage);
        _message.OnValueChanged -= OnMessageChanged;
        _playerInfoManager.LockInput = false;
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
    /// <summary>
    /// Send message on submit.
    /// </summary>
    /// <param name="callback"></param>
    private void PressedSubmit(InputAction.CallbackContext callback)
    {
        if (!IsActive)
            return;

        if (_chatShown)
        {
            if (!_inputField.isFocused)
                return;

            string input = _inputField.text.Trim();
            if (input.Length <= 0)
                return;
            _inputField.text = string.Empty;

            //TODO get message type based on selected chat tab
            OnSendMessage(input, EMessageType.Global);
        } else
        {
            ShowChat(true);
        }
        SelectInput();
    }

    /// <summary>
    /// Hides chat when pressing cancel.
    /// </summary>
    /// <param name="callback"></param>
    private void PressedCancel(InputAction.CallbackContext callback)
    {
        ShowChat(false);
    }
    #endregion

    /// <summary>
    /// Generate tabs based on the prefilled tab list.
    /// </summary>
    private void GenerateTabs()
    {
        foreach (ChatTab tab in tabs)
        {
            Button tabButton = Instantiate(_tabButtonPrefab, _tabButtonParent).GetComponent<Button>();
            TextMeshProUGUI buttonText = tabButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = tab.TabName;

            GameObject chatObject = Instantiate(_chatPrefab, _chatParent);
            ChatElements chatElement = chatObject.GetComponent<ChatElements>();

            tab.Init(tabButton, chatElement.Scrollbar, chatElement.Content, chatElement.View, _messagePrefab);
            tab.ShowTab.AddListener(OnShowTab);

            Destroy(chatElement);
        }

        tabs[0].OnClickTab();
    }

    /// <summary>
    /// Show or hide the chat based on the given bool.
    /// Locks input when chat is visible.
    /// </summary>
    /// <param name="show">Show the chat.</param>
    private void ShowChat(bool show)
    {
        _chatMainObject.SetActive(show);
        _chatShown = show;
        _playerInfoManager.LockInput = show;
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;

        if (show)
        {
            _cancelAction.action.performed += PressedCancel;
            SelectInput();
            _currentTab.SetScrollBarValue(0f);
        }
        else
        {
            _cancelAction.action.performed -= PressedCancel;
        }
    }

    /// <summary>
    /// Sends the message to either everyone or the local chat.
    /// </summary>
    /// <param name="message">Typed message.</param>
    /// <param name="messageType">Type of the message.</param>
    private void OnSendMessage(string message, EMessageType messageType)
    {
        TimeSpan timeNow = DateTime.Now.TimeOfDay;
        string time = $"[{string.Format("{0:d2}", timeNow.Hours)}:{string.Format("{0:d2}", timeNow.Minutes)}:{string.Format("{0:d2}", timeNow.Seconds)}]";
        ChatMessage chatMessage = new ChatMessage()
        {
            Message = $"{time} {_playerInfoManager.Name}: {message}",
            MessageType = messageType
        };

        if (messageType == EMessageType.Log || messageType == EMessageType.Error || messageType == EMessageType.Local)
        {
            ShowMessage(chatMessage);
        }
        else
        {
            SendMessageServerRpc(chatMessage);
        }
    }

    /// <summary>
    /// On message networkvariable value change show it in the chat.
    /// </summary>
    /// <param name="previousMessage"></param>
    /// <param name="newMessage"></param>
    private void OnMessageChanged(ChatMessage previousMessage, ChatMessage newMessage)
    {
        if (IsServer && !IsClient)
            return;
        ShowMessage(newMessage);
    }

    /// <summary>
    /// Adds message to corresponding tabs.
    /// If no tab was found then message will be put in the global tab.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public void ShowMessage(ChatMessage message)
    {
        if (message.MessageType == EMessageType.Error && !_chatShown)
            ShowChat(true);

        List<ChatTab> avaiableTabs = tabs.FindAll(t => t.MessageTypes.ContainsAnyFlags(message.MessageType));
        if (avaiableTabs.Count == 0)
            avaiableTabs.Add(tabs.Find(t => t.MessageTypes.HasFlag(EMessageType.Global)));

        foreach (ChatTab chatTab in avaiableTabs)
        {
            chatTab.AddMessage(message);
        }
    }

    /// <summary>
    /// Focus inputfield.
    /// </summary>
    private void SelectInput()
    {
        _inputField.ActivateInputField();
        _inputField.Select();
    }

    /// <summary>
    /// Hides current tab and shows given tab.
    /// </summary>
    /// <param name="tab">Tab to open.</param>
    private void OnShowTab(ChatTab tab)
    {
        if(_currentTab != null)
            _currentTab.HideTab();
        _currentTab = tab;
    }

    /// <summary>
    /// Sets the message networkvariable value, updating all clients with the new message.
    /// </summary>
    /// <param name="chatMessage"></param>
    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(ChatMessage chatMessage)
    {
        _message.Value = chatMessage;
    }
}
