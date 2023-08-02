using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem;
using TMPro;

public class Chat : MonoBehaviour
{
    [SerializeField] private GameObject _chat;
    [SerializeField] private GameObject _logPrefab;
    [SerializeField] private Transform _logParent;
    [SerializeField] private InputActionReference _enterAction;

    private bool _chatShown;

    private void OnEnable()
    {
        _enterAction.action.performed += PressedEnter;
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        _enterAction.action.performed -= PressedEnter;
        Application.logMessageReceived -= HandleLog;
    }

    private void PressedEnter(InputAction.CallbackContext callback)
    {
        if (_chatShown) {
           
        } else {
            _chat.SetActive(true);
            _chatShown = true;
        }

        //if visible sent chat otherwise open chat
    }

    private void HandleLog(string LogString, string stackTrace, LogType type)
    {
        GameObject messageObject = Instantiate(_logPrefab, _logParent);
        TextMeshProUGUI chatMessage = messageObject.GetComponent<TextMeshProUGUI>();
        chatMessage.text = LogString;
        if (type == LogType.Error)
            chatMessage.text += "n/" + stackTrace;
        messageObject.SetActive(true);
    }
}
