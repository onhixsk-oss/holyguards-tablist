using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace HolyGuards;

public sealed class HolyGuardsModSystem : ModSystem
{
    private const string NetworkChannelName = "holyguards-info";

    private GuiHolyGuards? dialog;
    private ICoreServerAPI? serverApi;
    private IServerNetworkChannel? serverChannel;

    public override void Start(ICoreAPI api)
    {
        api.Network
            .RegisterChannel(NetworkChannelName)
            .RegisterMessageType<HolyGuardsServerInfoPacket>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        serverApi = api;
        serverChannel = api.Network.GetChannel(NetworkChannelName);
        api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        api.Event.RegisterGameTickListener(_ => BroadcastServerInfo(), 5000, 2500);
        api.Logger.Notification("[HolyGuards] HolyGuards 0.4.0 loaded on the server.");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        dialog = new GuiHolyGuards(api);

        api.Network
            .GetChannel(NetworkChannelName)
            .SetMessageHandler<HolyGuardsServerInfoPacket>(OnServerInfo);

        api.Input.RegisterHotKey(
            "holyguardsmenu",
            "Open HolyGuards Menu",
            GlKeys.Home,
            HotkeyType.GUIOrOtherControls
        );

        api.Input.SetHotKeyHandler("holyguardsmenu", OnToggleMenu);
    }

    private bool OnToggleMenu(KeyCombination keyCombination)
    {
        if (dialog == null) return false;
        dialog.Toggle();
        return true;
    }

    private void OnServerInfo(HolyGuardsServerInfoPacket packet)
    {
        dialog?.UpdateServerInfo(packet);
    }

    private void OnPlayerNowPlaying(IServerPlayer player)
    {
        if (serverChannel == null) return;
        serverChannel.SendPacket(BuildServerInfoPacket(), player);
    }

    private void BroadcastServerInfo()
    {
        if (serverChannel == null || serverApi == null) return;
        serverChannel.BroadcastPacket(BuildServerInfoPacket());
    }

    private HolyGuardsServerInfoPacket BuildServerInfoPacket()
    {
        if (serverApi == null)
        {
            return new HolyGuardsServerInfoPacket();
        }

        IServerPlayer[] players = serverApi.World.AllOnlinePlayers
            .OfType<IServerPlayer>()
            .Where(player => player.ConnectionState == EnumClientState.Playing)
            .OrderBy(player => player.PlayerName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] names = players
            .Select(player => player.PlayerName ?? "Player")
            .ToArray();

        int[] pings = players
            .Select(player =>
            {
                float pingSeconds = player.Ping;

                if (float.IsNaN(pingSeconds) ||
                    float.IsInfinity(pingSeconds) ||
                    pingSeconds < 0)
                {
                    return -1;
                }

                return (int)Math.Round(pingSeconds * 1000f);
            })
            .ToArray();

        PlayStyle? playStyle = serverApi.WorldManager.CurrentPlayStyle;
        int[] spawn = serverApi.WorldManager.DefaultSpawnPosition ?? Array.Empty<int>();

        return new HolyGuardsServerInfoPacket
        {
            OnlinePlayers = players.Length,
            MaxPlayers = serverApi.Server.Config.MaxClients,
            ServerUptimeSeconds = serverApi.Server.ServerUptimeSeconds,
            WorldHour = serverApi.World.Calendar?.HourOfDay ?? 0f,
            WorldSeed = serverApi.World.Seed,
            PlayerNames = names,
            PlayerPingsMs = pings,
            GameVersion = GameVersion.ShortGameVersion,
            PlayStyleCode = playStyle?.Code ?? "",
            WorldType = playStyle?.WorldType ?? "",
            SpawnX = spawn.Length > 0 ? spawn[0] : 0,
            SpawnY = spawn.Length > 1 ? spawn[1] : 0,
            SpawnZ = spawn.Length > 2 ? spawn[2] : 0
        };
    }
}
