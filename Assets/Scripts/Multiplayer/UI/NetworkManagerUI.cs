using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button _serverButton;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;

    private void Awake()
    {
        _serverButton.onClick.AddListener(OnClickServer);
        _hostButton.onClick.AddListener(OnClickHost);
        _clientButton.onClick.AddListener(OnClickClient);
    }

    private void OnClickServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    private void OnClickHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void OnClickClient()
    {
        NetworkManager.Singleton.StartClient();
    }    
}
