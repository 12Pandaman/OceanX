using Unity.Netcode;
using Unity.Collections;

public struct FishData : INetworkSerializable
{
    public FixedString32Bytes fishHeader;
    public FixedString32Bytes fishName;
    public FixedString128Bytes fishInfo;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref fishHeader);
        serializer.SerializeValue(ref fishName);
        serializer.SerializeValue(ref fishInfo);
    }
}