using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Switch : NetworkBehaviour
{
    public UnityEvent<bool> OnSwitchChanged = new();

    private NetworkVariable<bool> _isActive = new();   

    public override void OnNetworkSpawn()
    {
        _isActive.OnValueChanged += OnValueChanged;
    }

    private void OnValueChanged(bool wasActive, bool isActive)
    {
        if (isActive)
        {
            Debug.Log("Active");
        }
        else 
        {
            Debug.Log("Not Active");
        }
        OnSwitchChanged.Invoke(isActive);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            _isActive.Value = true;
            return;
        }

        OnSwitchChangedServerRpc(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsServer)
        {
            _isActive.Value = false;
            return;
        }

        OnSwitchChangedServerRpc(false);
    }

    [ServerRpc]
    public void OnSwitchChangedServerRpc(bool isActive)
    {
        _isActive.Value = isActive;
    }
}
