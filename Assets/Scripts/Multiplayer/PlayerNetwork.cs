using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Linq;
using System;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private Transform _eyesParent;

    [SerializeField] private NetworkObject _networkObject;

    [SerializeField] private List<Component> _componentsToDestroy;
    [SerializeField] private List<GameObject> _gameObjectsToDestroy;

    public static PlayerInput Player;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetEyesLayer(_eyesParent);
        } else
        {
            DestoryPlayerItems();
        }
    }

    private void DestoryPlayerItems()
    {
        if (_componentsToDestroy.Count > 0)
        {
            foreach (Component component in _componentsToDestroy.ToList())
            {
                DestroyImmediate(component);
            }
        }
        if (_gameObjectsToDestroy.Count > 0)
        {
            foreach (GameObject gameObject in _gameObjectsToDestroy.ToList())
            {
                DestroyImmediate(gameObject);
            }
        }
    }

    private void SetEyesLayer(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.layer = Mathf.RoundToInt(Mathf.Log(_layerMask.value, 2));
            SetEyesLayer(child);
        }
    }
}
