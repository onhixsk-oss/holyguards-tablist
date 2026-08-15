using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HolyguardsPlayerList;

public sealed class HolyguardsPlayerListModSystem : ModSystem
{
    private HolyguardsPlayerListHud hud;
    private bool originalSuppressed;

    // Start after ordinary mods so we can cleanly suppress the stock PlayerLists HUD if it is present.
    public override double ExecuteOrder() => 999.0;

    public override void StartClientSide(ICoreClientAPI capi)
    {
        SuppressOriginalPlayerLists(capi);
        capi.Event.RegisterCallback(_ => SuppressOriginalPlayerLists(capi), 500);

        hud = new HolyguardsPlayerListHud(capi);
        capi.Logger.Notification("[holyguardsplayerlist] Holyguards server TAB list active.");
    }

    private void SuppressOriginalPlayerLists(ICoreClientAPI capi)
    {
        if (originalSuppressed) return;

        try
        {
            ModSystem original = capi.ModLoader.GetModSystem("playerlist.PlayerList");
            if (original == null) return;

            original.Dispose();
            originalSuppressed = true;
            capi.Logger.Notification("[holyguardsplayerlist] Original PlayerLists client HUD disabled for this session.");
        }
        catch (Exception e)
        {
            capi.Logger.Warning("[holyguardsplayerlist] Could not disable original PlayerLists HUD: {0}", e.Message);
        }
    }

    public override void Dispose()
    {
        hud?.Dispose();
        hud = null;
    }
}

public sealed class HolyguardsPlayerListHud : HudElement
{
    private const double GuiWidth = 720.0;
    private const double GuiHeight = 480.0;
    private const double SourceWidth = 1536.0;
    private const double SourceHeight = 1024.0;
    private const double Scale = GuiWidth / SourceWidth;
    private const int MaxRows = 11;

    // Horizontal separators in the source 1536x1024 artwork.
    private static readonly int[] RowLines =
    {
        405, 452, 496, 540, 586, 630, 675, 720, 765, 810, 853, 898
    };

    private readonly HolyguardsKeyHandler keyHandler;
    private readonly long tickListenerId;
    private string lastSignature = string.Empty;

    public HolyguardsPlayerListHud(ICoreClientAPI capi) : base(capi)
    {
        keyHandler = new HolyguardsKeyHandler(capi);
        tickListenerId = capi.Event.RegisterGameTickListener(_ => UpdateList(), 750);
        UpdateList(true);
    }

    private void UpdateList(bool force = false)
    {
        List<IPlayer> allPlayers = capi.World.AllOnlinePlayers
            .OrderBy(player => player.PlayerName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        string signature = BuildSignature(allPlayers);
        if (!force && signature == lastSignature) return;
        lastSignature = signature;

        Compose(allPlayers);
        TryOpen();
    }

    private static string BuildSignature(List<IPlayer> players)
    {
        return string.Join("|", players.Select(player =>
        {
            int ping = GetPingMs(player);
            int pingBucket = ping < 0 ? -1 : ping / 25;
            return $"{player.PlayerUID}:{player.PlayerName}:{GetRank(player)}:{pingBucket}";
        }));
    }

    private void Compose(List<IPlayer> allPlayers)
    {
        ElementBounds root = new()
        {
            Alignment = EnumDialogArea.CenterTop,
            BothSizing = ElementSizing.Fixed,
            fixedWidth = GuiWidth,
            fixedHeight = GuiHeight,
            fixedOffsetY = 18
        };

        GuiComposer composer = capi.Gui
            .CreateCompo("holyguardsplayerlist", root)
            .BeginChildElements()
            .AddImage(
                ElementBounds.Fixed(0, 0, GuiWidth, GuiHeight),
                new AssetLocation("holyguardsplayerlist", "textures/gui/tablist.png")
            );

        CairoFont nameFont = CairoFont.WhiteSmallText()
            .WithFontSize(13f)
            .WithOrientation(EnumTextOrientation.Center);

        CairoFont rankFont = CairoFont.WhiteSmallText()
            .WithFontSize(12f)
            .WithOrientation(EnumTextOrientation.Center)
            .WithColor(new[] { 0.96, 0.80, 0.42, 1.0 });

        CairoFont pingFont = CairoFont.WhiteSmallText()
            .WithFontSize(11f)
            .WithOrientation(EnumTextOrientation.Center);

        int regularRows = Math.Min(allPlayers.Count, MaxRows);
        bool hasOverflow = allPlayers.Count > MaxRows;
        if (hasOverflow) regularRows = MaxRows - 1;

        for (int i = 0; i < regularRows; i++)
        {
            IPlayer player = allPlayers[i];
            AddRow(composer, i, player.PlayerName, GetRank(player), FormatPing(GetPingMs(player)), nameFont, rankFont, pingFont);
        }

        if (hasOverflow)
        {
            int remaining = allPlayers.Count - regularRows;
            AddRow(composer, MaxRows - 1, $"+{remaining} ďalších", string.Empty, string.Empty, nameFont, rankFont, pingFont);
        }

        SingleComposer = composer
            .EndChildElements()
            .Compose();
    }

    private static void AddRow(
        GuiComposer composer,
        int row,
        string name,
        string rank,
        string ping,
        CairoFont nameFont,
        CairoFont rankFont,
        CairoFont pingFont)
    {
        int top = RowLines[row];
        int bottom = RowLines[row + 1];

        // Leave a little vertical breathing room inside the painted row.
        double y = Px(top + 6);
        double h = Math.Max(14, Px(bottom - top - 10));

        // Source artwork columns: player 223-614, rank 619-1044, ping 1049-1313.
        ElementBounds nameBounds = ElementBounds.Fixed(Px(228), y, Px(382), h);
        ElementBounds rankBounds = ElementBounds.Fixed(Px(624), y, Px(414), h);

        // Keep the right side of the PING column clear for the painted gold bar icon.
        ElementBounds pingBounds = ElementBounds.Fixed(Px(1055), y, Px(145), h);

        composer.AddStaticText(Trim(name, 24), nameFont, nameBounds);
        composer.AddStaticText(Trim(rank, 22), rankFont, rankBounds);
        composer.AddStaticText(ping, pingFont, pingBounds);
    }

    private static double Px(double sourcePixels) => sourcePixels * Scale;

    private static string Trim(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max) return text ?? string.Empty;
        return text[..max] + "…";
    }

    private static string GetRank(IPlayer player)
    {
        try
        {
            string rank = player.Role?.Name;
            return string.IsNullOrWhiteSpace(rank) ? string.Empty : rank;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int GetPingMs(IPlayer player)
    {
        try
        {
            PropertyInfo prop = player.GetType().GetProperty(
                "Ping",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            object value = prop?.GetValue(player);
            return value switch
            {
                float seconds => (int)Math.Round(seconds * 1000.0),
                double seconds => (int)Math.Round(seconds * 1000.0),
                int milliseconds => milliseconds,
                _ => -1
            };
        }
        catch
        {
            return -1;
        }
    }

    private static string FormatPing(int ping)
    {
        return ping < 0 ? "—" : $"{ping} ms";
    }

    public override bool ShouldReceiveRenderEvents() => keyHandler.IsKeyComboActive();

    public override double InputOrder => 1.0999;
    public override double DrawOrder => 0.8899;
    public override float ZSize => 200F;

    public override bool ShouldReceiveKeyboardEvents() => false;
    public override void OnKeyDown(KeyEvent args) { }
    public override void OnKeyPress(KeyEvent args) { }
    public override void OnKeyUp(KeyEvent args) { }
    public override bool OnEscapePressed() => false;
    public override bool ShouldReceiveMouseEvents() => false;
    public override void OnMouseDown(MouseEvent args) { }
    public override void OnMouseUp(MouseEvent args) { }
    public override void OnMouseMove(MouseEvent args) { }
    public override void OnMouseWheel(MouseWheelEventArgs args) { }
    public override bool OnMouseEnterSlot(ItemSlot slot) => false;
    public override bool OnMouseLeaveSlot(ItemSlot itemSlot) => false;
    public override bool CaptureAllInputs() => false;

    public override bool TryClose() => false;
    public override void Toggle() { }
    public override void UnFocus() { }
    public override void Focus() { }
    public override bool Focused => false;
    protected override void OnFocusChanged(bool on) => focused = false;

    public override void Dispose()
    {
        base.Dispose();
        keyHandler.Dispose();
        capi.Event.UnregisterGameTickListener(tickListenerId);
    }
}

public sealed class HolyguardsKeyHandler
{
    private readonly ICoreClientAPI capi;

    public HolyguardsKeyHandler(ICoreClientAPI capi)
    {
        this.capi = capi;
        capi.Input.RegisterHotKey(
            "holyguardsplayerlist",
            "Holyguards player list",
            GlKeys.Tab,
            HotkeyType.GUIOrOtherControls
        );
        capi.Input.SetHotKeyHandler("holyguardsplayerlist", _ => true);
    }

    public bool IsKeyComboActive()
    {
        KeyCombination combo = capi.Input.GetHotKeyByCode("holyguardsplayerlist").CurrentMapping;
        bool[] keys = capi.Input.KeyboardKeyState;

        if (combo == null || keys == null || combo.KeyCode < 0 || combo.KeyCode >= keys.Length) return false;

        return keys[combo.KeyCode]
               && IsAltDown() == combo.Alt
               && IsCtrlDown() == combo.Ctrl
               && IsShiftDown() == combo.Shift;
    }

    private bool IsAltDown() => IsDown(GlKeys.AltLeft) || IsDown(GlKeys.AltRight) || IsDown(GlKeys.LAlt) || IsDown(GlKeys.RAlt);
    private bool IsCtrlDown() => IsDown(GlKeys.ControlLeft) || IsDown(GlKeys.ControlRight) || IsDown(GlKeys.LControl) || IsDown(GlKeys.RControl);
    private bool IsShiftDown() => IsDown(GlKeys.ShiftLeft) || IsDown(GlKeys.ShiftRight) || IsDown(GlKeys.LShift) || IsDown(GlKeys.RShift);

    private bool IsDown(GlKeys key)
    {
        bool[] keys = capi.Input.KeyboardKeyState;
        int index = (int)key;
        return keys != null && index >= 0 && index < keys.Length && keys[index];
    }

    public void Dispose()
    {
        // Vintage Story owns the hotkey registry; nothing else is required here.
    }
}
