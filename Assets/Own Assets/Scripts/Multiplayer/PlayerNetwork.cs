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

    [SerializeField] private PlayerData playerData;

    [SerializeField] private NetworkObject _networkObject;

    [SerializeField] private ToDestroy _toDestroyWhenPlayer;
    [SerializeField] private ToDestroy _toDestroyWhenNotPlayer;

    [Serializable]
    public class ToDestroy 
    {
        public List<Component> ComponentsToDestroy = new();
        public List<GameObject> GameObjectsToDestroy = new();
    }

    public static PlayerInput Player;

    public override void OnNetworkSpawn()
    {
        if (IsClient && IsLocalPlayer)
        {
            playerData.EyesPosition = _eyesParent;
            SetEyesLayer(_eyesParent);
            DestoryPlayerItems(_toDestroyWhenPlayer);
        } else
        {
            DestoryPlayerItems(_toDestroyWhenNotPlayer);
        }
    }
    
    private void DestoryPlayerItems(ToDestroy toDestroy)
    {
        List<Component> components = toDestroy.ComponentsToDestroy;
        List<GameObject> gameObjects = toDestroy.GameObjectsToDestroy;

        if (components.Count > 0)
        {
            foreach (Component component in components.ToList())
            {
                DestroyImmediate(component);
            }
        }
        if (gameObjects.Count > 0)
        {
            foreach (GameObject gameObject in gameObjects.ToList())
            {
                DestroyImmediate(gameObject);
            }
        }
    }

    private void SetEyesLayer(Transform parent)
    {
        Helper.SetLayerMask(parent, _layerMask);
    }
}
