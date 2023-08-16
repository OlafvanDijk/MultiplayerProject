using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerName : NetworkBehaviour
{

    [SerializeField] private PlayerData _playerData;
    [SerializeField] private Transform _canvas;
    [SerializeField] private TextMeshProUGUI _playerNameField;
    [SerializeField] private LayerMask _ignoreCameraMask;

    private NetworkVariable<FixedString64Bytes> _playerName = new();

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            Helper.SetLayerMask(_canvas, _ignoreCameraMask);
            string name = _playerData.Name;
            _playerNameField.text = name;
            SetNameServerRpc(name);
            _playerData.E_OnNameChange.AddListener((newName) => SetNameServerRpc(newName));
        }
        else 
        {
            if (_playerName.Value.ToString() != string.Empty)
            {
                _playerNameField.text = _playerName.Value.ToString();
            }
            _playerName.OnValueChanged += NameChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsLocalPlayer)
        {
            _playerData.E_OnNameChange.RemoveListener((newName) => SetNameServerRpc(newName));
        } else 
        {
            _playerName.OnValueChanged -= NameChanged;
        }
    }

    private void NameChanged(FixedString64Bytes previousName, FixedString64Bytes newName)
    {
        _playerNameField.text = newName.ToString();
    }

    private void Update()
    {
        if (IsClient && IsLocalPlayer || _playerData.EyesPosition == null)
            return;

        Quaternion rotation = _playerData.EyesPosition.rotation;
        transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetNameServerRpc(string name)
    {
        _playerName.Value = name;
    }
}
