using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Triggerable_Collider : Triggerable
{
    public UnityEvent<bool> E_OnSwitchChanged = new();

    private NetworkVariable<bool> _isActive = new();   

    public override void OnNetworkSpawn()
    {
        _isActive.OnValueChanged += OnValueChanged;
    }

    private void OnValueChanged(bool wasActive, bool isActive)
    {
        if (isActive)
        {
            Activate();
        }
        else 
        {
            Deactivate();
        }
        E_OnSwitchChanged.Invoke(isActive);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsServer && IsLocalPlayer)
        {
            _isActive.Value = true;
            return;
        }

        OnSwitchChangedServerRpc(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsServer && IsLocalPlayer)
        {
            _isActive.Value = false;
            return;
        }

        OnSwitchChangedServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnSwitchChangedServerRpc(bool isActive)
    {
        _isActive.Value = isActive;
    }
}
