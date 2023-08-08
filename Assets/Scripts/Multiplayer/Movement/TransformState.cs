using UnityEngine;
using Unity.Netcode;

public class TransformState : INetworkSerializable
{
    public int Tick;
    public Vector3 Position;
    public Quaternion Rotation;
    public bool HasStartedMoving;

    /// <summary>
    /// Reading the values needs to be in order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serializer"></param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            FastBufferReader reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out Tick);
            reader.ReadValueSafe(out Position);
            reader.ReadValueSafe(out Rotation);
            reader.ReadValueSafe(out HasStartedMoving);
        }
        else
        {
            FastBufferWriter reader = serializer.GetFastBufferWriter();
            reader.WriteValueSafe(Tick);
            reader.WriteValueSafe(Position);
            reader.WriteValueSafe(Rotation);
            reader.WriteValueSafe(HasStartedMoving);
        }
    }
}
