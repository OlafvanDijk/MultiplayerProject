using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;

public struct ChatMessage : INetworkSerializable
{
    public FixedString128Bytes Message;
    public EMessageType MessageType;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            FastBufferReader reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out Message);
            reader.ReadValueSafe(out MessageType);
        }
        else
        {
            FastBufferWriter writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(Message);
            writer.WriteValueSafe(MessageType);
        }
    }
}

[Serializable]
[Flags]
public enum EMessageType
{
    Global = 1,
    Local = 1 << 1,
    Log = 1 << 2,
    Error = 1 << 3
}
