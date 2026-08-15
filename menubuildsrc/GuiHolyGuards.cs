using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HolyGuards;

public sealed class GuiHolyGuards : GuiDialog
{
    private const double PlayerPanelWidth = 300;
    private const double PanelGap = 10;
    private const double MenuWidth = 960;
    private const double MenuHeight = 720;
    private const double TotalWidth = PlayerPanelWidth + PanelGap + MenuWidth;

    private const int PlayerSlots = 20;
    private const int PlayerRowsPerColumn = 10;

    private readonly ElementBounds imageBounds;

    public override string ToggleKeyCombinationCode => "holyguardsmenu";
    public override bool PrefersUngrabbedMouse => true;

    public GuiHolyGuards(ICoreClientAPI capi) : base(capi)
    {
        ElementBounds dialogBounds = ElementBounds
            .Fixed(0, 0, TotalWidth, MenuHeight)
            .WithAlignment(EnumDialogArea.CenterMiddle);

        ElementBounds playerPanelBounds =
            ElementBounds.Fixed(0, 116, PlayerPanelWidth, 490);

        imageBounds = ElementBounds.Fixed(
            PlayerPanelWidth + PanelGap,
            0,
            MenuWidth,
            MenuHeight
        );

        CairoFont infoFont = CairoFont.WhiteSmallishText().WithFontSize(17);
        CairoFont smallInfoFont = CairoFont.WhiteSmallishText().WithFontSize(14);
        CairoFont playerTitleFont = CairoFont.WhiteSmallishText().WithFontSize(20);
        CairoFont playerFont = CairoFont.WhiteSmallishText().WithFontSize(14);

        GuiComposer composer = capi.Gui
            .CreateCompo("holyguards-mainmenu", dialogBounds)
            .AddShadedDialogBG(playerPanelBounds)
            .AddStaticText(
                "HRÁČI ONLINE",
                playerTitleFont,
                EnumTextOrientation.Center,
                ElementBounds.Fixed(20, 137, PlayerPanelWidth - 40, 35)
            )
            .AddDynamicText(
                "0 / 0",
                playerFont,
                EnumTextOrientation.Center,
                ElementBounds.Fixed(20, 174, PlayerPanelWidth - 40, 24),
                "hg-playercount"
            )
            .AddImage(
                imageBounds,
                new AssetLocation("holyguards:textures/gui/menu.png")
            );

        double menuX = PlayerPanelWidth + PanelGap;

        composer
            .AddDynamicText(
                "-",
                infoFont,
                ElementBounds.Fixed(menuX + 662, 278, 130, 24),
                "hg-gameversion"
            )
            .AddDynamicText(
                "- / -",
                infoFont,
                ElementBounds.Fixed(menuX + 662, 305, 100, 24),
                "hg-online"
            )
            .AddDynamicText(
                "--:--:--",
                infoFont,
                ElementBounds.Fixed(menuX + 662, 333, 100, 24),
                "hg-uptime"
            )
            .AddDynamicText(
                "--:--",
                infoFont,
                ElementBounds.Fixed(menuX + 662, 360, 100, 24),
                "hg-worldtime"
            )
            .AddDynamicText(
                "-",
                smallInfoFont,
                ElementBounds.Fixed(menuX + 662, 387, 138, 24),
                "hg-seed"
            )
            .AddDynamicText(
                "-",
                infoFont,
                ElementBounds.Fixed(menuX + 662, 414, 135, 24),
                "hg-maptype"
            )
            .AddDynamicText(
                "-",
                smallInfoFont,
                ElementBounds.Fixed(menuX + 662, 441, 138, 24),
                "hg-spawn"
            );

        for (int index = 0; index < PlayerSlots; index++)
        {
            int column = index / PlayerRowsPerColumn;
            int row = index % PlayerRowsPerColumn;
            double x = 18 + column * 141;
            double y = 212 + row * 34;

            composer.AddDynamicText(
                "",
                playerFont,
                ElementBounds.Fixed(x, y, 128, 28),
                $"hg-player-{index}"
            );
        }

        SingleComposer = composer.Compose(false);
    }

    public void UpdateServerInfo(HolyGuardsServerInfoPacket packet)
    {
        if (SingleComposer == null) return;

        SetText("hg-gameversion", packet.GameVersion);
        SetText("hg-online", $"{packet.OnlinePlayers} / {packet.MaxPlayers}");
        SetText("hg-playercount", $"{packet.OnlinePlayers} / {packet.MaxPlayers}");
        SetText("hg-uptime", FormatUptime(packet.ServerUptimeSeconds));
        SetText("hg-worldtime", FormatWorldTime(packet.WorldHour));
        SetText("hg-seed", packet.WorldSeed.ToString(CultureInfo.InvariantCulture));
        SetText("hg-maptype", FormatPlayStyle(packet.PlayStyleCode, packet.WorldType));
        SetText("hg-spawn", $"{packet.SpawnX}, {packet.SpawnY}, {packet.SpawnZ}");
        UpdatePlayerList(packet);
    }

    private void SetText(string key, string? value)
    {
        SingleComposer?
            .GetDynamicText(key)?
            .SetNewText(
                string.IsNullOrWhiteSpace(value) ? "-" : value,
                false,
                true
            );
    }

    private void UpdatePlayerList(HolyGuardsServerInfoPacket packet)
    {
        string[] names = packet.PlayerNames ?? Array.Empty<string>();
        int[] pings = packet.PlayerPingsMs ?? Array.Empty<int>();

        for (int index = 0; index < PlayerSlots; index++)
        {
            string line = "";

            if (index < names.Length)
            {
                string name = ShortenName(names[index], 13);
                int ping = index < pings.Length ? pings[index] : -1;

                if (index == PlayerSlots - 1 && names.Length > PlayerSlots)
                {
                    line = $"+{names.Length - PlayerSlots + 1} ďalších";
                }
                else if (ping >= 0)
                {
                    line = $"{name}  {ping}ms";
                }
                else
                {
                    line = name;
                }
            }

            SingleComposer
                .GetDynamicText($"hg-player-{index}")
                ?.SetNewText(line, false, true);
        }
    }

    private static string FormatPlayStyle(string? code, string? worldType)
    {
        string normalized = (code ?? "").Trim().ToLowerInvariant();

        return normalized switch
        {
            "surviveandbuild" => "Survival",
            "survival" => "Survival",
            "creativebuilding" => "Creative",
            "creative" => "Creative",
            "exploration" => "Exploration",
            _ => HumanizeCode(
                !string.IsNullOrWhiteSpace(code) ? code! :
                !string.IsNullOrWhiteSpace(worldType) ? worldType! :
                "-"
            )
        };
    }

    private static string HumanizeCode(string value)
    {
        string clean = value.Trim();
        if (clean.Length == 0) return "-";

        clean = clean.Replace("-", " ").Replace("_", " ");
        StringBuilder builder = new StringBuilder(clean.Length + 8);

        for (int i = 0; i < clean.Length; i++)
        {
            char current = clean[i];

            if (i > 0 && char.IsUpper(current) && char.IsLower(clean[i - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        string result = builder.ToString().Trim();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(result.ToLowerInvariant());
    }

    private static string ShortenName(string name, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Player";

        string clean = name.Trim();
        if (clean.Length <= maxCharacters) return clean;

        return clean[..Math.Max(1, maxCharacters - 1)] + "…";
    }

    public override void OnMouseDown(MouseEvent args)
    {
        base.OnMouseDown(args);

        if (args.Handled || !imageBounds.PointInside(args.X, args.Y))
        {
            return;
        }

        double nx = (args.X - imageBounds.absX) / imageBounds.OuterWidth;
        double ny = (args.Y - imageBounds.absY) / imageBounds.OuterHeight;

        if (Inside(nx, ny, 0.178, 0.545, 0.380, 0.470))
        {
            OpenUrl(HolyGuardsLinks.LiveMap, "LiveMapa");
            args.Handled = true;
            return;
        }

        if (Inside(nx, ny, 0.178, 0.545, 0.482, 0.580))
        {
            OpenUrl(HolyGuardsLinks.Discord, "Discord");
            args.Handled = true;
            return;
        }
    }

    private static string FormatUptime(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);

        if (time.TotalDays >= 1)
        {
            return $"{(int)time.TotalDays}d {time.Hours:00}:{time.Minutes:00}";
        }

        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private static string FormatWorldTime(float hourOfDay)
    {
        hourOfDay %= 24f;
        if (hourOfDay < 0) hourOfDay += 24f;

        int hour = (int)Math.Floor(hourOfDay);
        int minute = (int)Math.Floor((hourOfDay - hour) * 60f);
        return $"{hour:00}:{minute:00}";
    }

    private static bool Inside(
        double x,
        double y,
        double minX,
        double maxX,
        double minY,
        double maxY
    )
    {
        return x >= minX && x <= maxX && y >= minY && y <= maxY;
    }

    private void OpenUrl(string url, string buttonName)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            capi.Logger.Warning("[HolyGuards] Button '{0}' has no URL configured.", buttonName);
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            capi.Logger.Error("[HolyGuards] Invalid URL for '{0}': {1}", buttonName, url);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            capi.Logger.Error("[HolyGuards] Could not open '{0}': {1}", buttonName, e);
        }
    }
}
