using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
}
