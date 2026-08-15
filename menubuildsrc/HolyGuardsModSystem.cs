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
    private const int UnknownCoordinate = int.MinValue;

    private GuiHolyGuards? dialog;
    private ICoreServerAPI? serverApi;
    private IServerNetworkChannel? serverChannel;
    private bool broadcastErrorLogged;

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
        api.Logger.Notification("[HolyGuards] HolyGuards 0.4.2 loaded on the server.");
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
        if (serverChannel == null || serverApi == null) return;

        try
        {
            serverChannel.SendPacket(BuildServerInfoPacket(), player);
        }
        catch (Exception e)
        {
            serverApi.Logger.Error("[HolyGuards] Failed to send initial server info: {0}", e.Message);
        }
    }

    private void BroadcastServerInfo()
    {
        if (serverChannel == null || serverApi == null) return;

        try
        {
            serverChannel.BroadcastPacket(BuildServerInfoPacket());
            broadcastErrorLogged = false;
        }
        catch (Exception e)
        {
            // Do not flood server-main.log if a future API field is unavailable.
            if (!broadcastErrorLogged)
            {
                serverApi.Logger.Error("[HolyGuards] Server-info update failed: {0}", e.Message);
                broadcastErrorLogged = true;
            }
        }
    }

    private HolyGuardsServerInfoPacket BuildServerInfoPacket()
    {
        if (serverApi == null)
        {
            return new HolyGuardsServerInfoPacket
            {
                SpawnX = UnknownCoordinate,
                SpawnY = UnknownCoordinate,
                SpawnZ = UnknownCoordinate
            };
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

        // VS 1.22.4 can throw NullReferenceException while reading
        // WorldManager.DefaultSpawnPosition even after the world is loaded.
        // Spawn is therefore optional for this build instead of risking the server loop.
        int spawnX = UnknownCoordinate;
        int spawnY = UnknownCoordinate;
        int spawnZ = UnknownCoordinate;

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
            SpawnX = spawnX,
            SpawnY = spawnY,
            SpawnZ = spawnZ
        };
    }
}
