using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace RefinedGem.Multiplayer;

/// <summary>
/// Broadcasts a player's local refined pool snapshot to peers (lobby ready / join / rejoin).
/// Sender id from the net layer is authoritative; this payload has no player id.
/// </summary>
public sealed class RefinedPoolSyncMessage : INetMessage
{
    public List<string> cardIds = [];

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.VeryDebug;

    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(cardIds.Count);
        foreach (var id in cardIds)
            writer.WriteString(id);
    }

    public void Deserialize(PacketReader reader)
    {
        var count = reader.ReadInt();
        cardIds = new List<string>(count);
        for (var i = 0; i < count; i++)
            cardIds.Add(reader.ReadString());
    }
}
