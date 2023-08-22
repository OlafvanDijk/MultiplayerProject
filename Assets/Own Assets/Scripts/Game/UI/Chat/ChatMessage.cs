using System;
using Unity.Collections;
using Unity.Netcode;

namespace Game.UI.Messaging
{
    public struct ChatMessage : INetworkSerializable
    {
        public FixedString512Bytes Message;
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

}