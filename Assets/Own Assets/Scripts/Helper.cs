using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public static class Helper
{
    public static ServerRpcParams CreateServerParam(ulong ID)
    { 
        return new ServerRpcParams() { Receive = new ServerRpcReceiveParams { SenderClientId = ID } };
    }

    public static ClientRpcParams ServerToClientParam(ServerRpcParams serverRpcParams)
    {
        List<ulong> targetClientIds = new();
        targetClientIds.Add(serverRpcParams.Receive.SenderClientId);
        ClientRpcSendParams clientRpcSendParams = new();
        clientRpcSendParams.TargetClientIds = targetClientIds;
        return new ClientRpcParams { Send = clientRpcSendParams };
    }

    public static bool CheckPlayer(ServerRpcParams serverRpcParams, ulong ID)
    {
        return serverRpcParams.Receive.SenderClientId == ID;
    }

    public static TransformState TransformState(int tick, Vector3 position, Quaternion rotation, bool hasStartedMoving)
    {
        return new TransformState()
        {
            Tick = tick,
            Position = position,
            Rotation = rotation,
            HasStartedMoving = hasStartedMoving
        };
    }

    public static IEnumerable<Enum> GetFlags(this Enum e)
    {
        return Enum.GetValues(e.GetType()).Cast<Enum>().Where(e.HasFlag);
    }

    public static bool ContainsAnyFlags(this Enum e, Enum other)
    {
        return
            e.GetFlags().ToList().Where(value => !value.ToString().Equals("None")).Any(
                value => e.HasFlag(value) && other.HasFlag(value));
    }

    public static void SetLayerMask(Transform parent, LayerMask layerMask, int mask = -2)
    {
        if (mask == -2)
            mask = Mathf.RoundToInt(Mathf.Log(layerMask.value, 2));

        foreach (Transform child in parent)
        {
            child.gameObject.layer = mask;
            SetLayerMask(child, layerMask, mask);
        }
    }


}
