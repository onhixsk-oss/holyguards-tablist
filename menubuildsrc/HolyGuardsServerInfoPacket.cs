using System;
using ProtoBuf;

namespace HolyGuards;

[ProtoContract]
public sealed class HolyGuardsServerInfoPacket
{
    [ProtoMember(1)] public int OnlinePlayers { get; set; }
    [ProtoMember(2)] public int MaxPlayers { get; set; }
    [ProtoMember(3)] public int ServerUptimeSeconds { get; set; }
    [ProtoMember(4)] public float WorldHour { get; set; }
    [ProtoMember(5)] public int WorldSeed { get; set; }
    [ProtoMember(6)] public string[] PlayerNames { get; set; } = Array.Empty<string>();
    [ProtoMember(7)] public int[] PlayerPingsMs { get; set; } = Array.Empty<int>();
    [ProtoMember(8)] public string GameVersion { get; set; } = "";
    [ProtoMember(9)] public string PlayStyleCode { get; set; } = "";
    [ProtoMember(10)] public string WorldType { get; set; } = "";
    [ProtoMember(11)] public int SpawnX { get; set; }
    [ProtoMember(12)] public int SpawnY { get; set; }
    [ProtoMember(13)] public int SpawnZ { get; set; }
}
