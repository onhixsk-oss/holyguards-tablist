using Cairo;
using holyguardstablist.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace holyguardstablist.gui.element;

public sealed class GuiHolyguardsTable : GuiElement {
    // Canonical canvas taken from the approved Holyguards visual target.
    public const double Width = 1040;
    public const double Height = 580;

    private const int MaxRows = 4;
    private const double CountX = 490;
    private const double CountY = 395;
    private const double RowX = 150;
    private const double RowY = 438;
    private const double RowWidth = 740;
    private const double RowsAreaHeight = 102;

    private readonly PlayerList _mod;
    private readonly List<PlayerData> _players;
    private readonly TextDrawUtil _textUtil = new();
    private readonly HeaderImage _background;
    private readonly BitmapRef? _playerIcon;
    private readonly double _layoutScale;

    public GuiHolyguardsTable(PlayerList mod, List<string> players, ElementBounds bounds, double layoutScale)
        : base(mod.Api as ICoreClientAPI, bounds) {
        _mod = mod;
        _layoutScale = Math.Clamp(layoutScale, 0.1, 1.0);

        _players = players
            .Take(MaxRows)
            .Select(uid => new PlayerData(mod, api.World.PlayerByUid(uid)))
            .ToList();

        Bounds.fixedWidth = Width * _layoutScale;
        Bounds.fixedHeight = Height * _layoutScale;
        _background = new HeaderImage(mod, "holyguardstablist:textures/gui/tablist.png", Bounds);

        try {
            _playerIcon = api.Assets
                .Get(new AssetLocation("holyguardstablist", "textures/gui/playericon.png"))
                .ToBitmap(api);
        } catch {
            _playerIcon = null;
        }
    }

    public override void ComposeElements(Context ctx, ImageSurface surface) {
        Bounds.CalcWorldBounds();
        _background.ComposeElements(ctx, surface);

        DrawCount(ctx);
        DrawPlayers(ctx, surface);
    }

    private void DrawCount(Context ctx) {
        // Cover the example count baked into the visual mock-up, then draw live data.
        double patchX = Bounds.drawX + scaled(438 * _layoutScale);
        double patchY = Bounds.drawY + scaled(386 * _layoutScale);
        double patchW = scaled(170 * _layoutScale);
        double patchH = scaled(48 * _layoutScale);

        ctx.SetSourceRGBA(0.045, 0.052, 0.055, 0.93);
        Rectangle(ctx, patchX, patchY, patchW, patchH);
        ctx.Fill();

        int maxPlayers = Math.Max(_players.Count, _mod.Config.MaxPlayers ?? _players.Count);
        string count = $"[{_players.Count}/{maxPlayers}]";

        CairoFont countFont = Util.DefaultFont.Clone()
            .WithFontSize((float)(24 * _layoutScale));
        countFont.SetupContext(ctx);

        _textUtil.DrawTextLine(
            ctx,
            countFont,
            count,
            Bounds.drawX + scaled(CountX * _layoutScale),
            Bounds.drawY + scaled(CountY * _layoutScale)
        );
    }

    private void DrawPlayers(Context ctx, ImageSurface surface) {
        // Cover the static example row from the approved mock-up.
        double coverX = Bounds.drawX + scaled(142 * _layoutScale);
        double coverY = Bounds.drawY + scaled(432 * _layoutScale);
        double coverW = scaled(756 * _layoutScale);
        double coverH = scaled(112 * _layoutScale);

        ctx.SetSourceRGBA(0.055, 0.060, 0.060, 0.94);
        Rectangle(ctx, coverX, coverY, coverW, coverH);
        ctx.Fill();

        if (_players.Count == 0) return;

        double gap = _players.Count == 1 ? 0 : 3;
        double rowHeight = Math.Min(48, (RowsAreaHeight - gap * (_players.Count - 1)) / _players.Count);
        double fontSize = Math.Clamp(rowHeight * 0.43, 11, 20);
        double iconSize = Math.Clamp(rowHeight - 12, 12, 30);

        for (int i = 0; i < _players.Count; i++) {
            PlayerData player = _players[i];
            double rowTop = RowY + i * (rowHeight + gap);

            double x = Bounds.drawX + scaled(RowX * _layoutScale);
            double y = Bounds.drawY + scaled(rowTop * _layoutScale);
            double w = scaled(RowWidth * _layoutScale);
            double h = scaled(rowHeight * _layoutScale);

            ctx.SetSourceRGBA(0.105, 0.110, 0.110, 0.90);
            Rectangle(ctx, x, y, w, h);
            ctx.Fill();

            double iconX = x + scaled(10 * _layoutScale);
            double iconY = y + (h - scaled(iconSize * _layoutScale)) / 2;
            int iconPixels = Math.Max(1, (int)scaled(iconSize * _layoutScale));

            if (_playerIcon != null) {
                surface.Image(_playerIcon, (int)iconX, (int)iconY, iconPixels, iconPixels);
            }

            CairoFont nameFont = Util.DefaultFont.Clone()
                .WithFontSize((float)(fontSize * _layoutScale));
            nameFont.SetupContext(ctx);
            _textUtil.DrawTextLine(
                ctx,
                nameFont,
                player.Name,
                x + scaled(54 * _layoutScale),
                y + scaled(Math.Max(2, (rowHeight - fontSize) / 2 - 1) * _layoutScale)
            );

            string ping = player.Ping >= 0 ? player.Ping.ToString() : "—";
            CairoFont pingFont = Util.DefaultFont.Clone()
                .WithFontSize((float)(fontSize * _layoutScale));
            pingFont.SetupContext(ctx);
            _textUtil.DrawTextLine(
                ctx,
                pingFont,
                ping,
                x + w - scaled(95 * _layoutScale),
                y + scaled(Math.Max(2, (rowHeight - fontSize) / 2 - 1) * _layoutScale)
            );

            int pingIconSize = Math.Max(1, (int)scaled(Math.Min(24, rowHeight - 10) * _layoutScale));
            surface.Image(
                _mod.PingIcon(player.Ping),
                (int)(x + w - scaled(40 * _layoutScale)),
                (int)(y + (h - pingIconSize) / 2),
                pingIconSize,
                pingIconSize
            );
        }
    }
}
