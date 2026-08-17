using Cairo;
using holyguardstablist.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace holyguardstablist.gui.element;

public sealed class GuiHolyguardsTable : GuiElement {
    // Canonical reference canvas. All coordinates are authored against this
    // exact 720x480 composition from the approved Holyguards screenshot.
    public const double Width = 720;
    public const double Height = 480;

    private const int MaxRows = 10;
    private const double FirstRowY = 190;
    private const double RowHeight = 21.05;
    private const double NameX = 126;
    private const double RankX = 337;
    private const double PingX = 520;

    private readonly List<PlayerData> _players;
    private readonly TextDrawUtil _textUtil = new();
    private readonly HeaderImage _background;
    private readonly double _layoutScale;

    public GuiHolyguardsTable(PlayerList mod, List<string> players, ElementBounds bounds, double layoutScale)
        : base(mod.Api as ICoreClientAPI, bounds) {
        _layoutScale = Math.Clamp(layoutScale, 0.1, 1.0);

        _players = players
            .Take(MaxRows)
            .Select(uid => new PlayerData(mod, api.World.PlayerByUid(uid)))
            .ToList();

        // HeaderImage uses the element bounds when drawing, so the artwork is
        // resized with the very same uniform scale used for the text below.
        Bounds.fixedWidth = Width * _layoutScale;
        Bounds.fixedHeight = Height * _layoutScale;
        _background = new HeaderImage(mod, "holyguardstablist:textures/gui/tablist.png", Bounds);
    }

    public override void ComposeElements(Context ctx, ImageSurface surface) {
        Bounds.CalcWorldBounds();
        _background.ComposeElements(ctx, surface);

        // Keep names, rank, ping and row spacing locked to the original
        // 720x480 design. Scaling the Cairo context also scales the fonts,
        // instead of independently rounding every text size/coordinate.
        ctx.Save();
        try {
            ctx.Translate(Bounds.drawX, Bounds.drawY);
            ctx.Scale(_layoutScale, _layoutScale);

            for (int i = 0; i < _players.Count; i++) {
                PlayerData player = _players[i];
                double y = scaled(FirstRowY + i * RowHeight);

                CairoFont nameFont = player.Font;
                nameFont.SetupContext(ctx);
                _textUtil.DrawTextLine(ctx, player.Name, nameFont, scaled(NameX), y);

                CairoFont infoFont = Util.DefaultFont;
                infoFont.SetupContext(ctx);
                _textUtil.DrawTextLine(ctx, "Hráč", infoFont, scaled(RankX), y);

                string ping = player.Ping >= 0 ? $"{player.Ping} ms" : "—";
                _textUtil.DrawTextLine(ctx, ping, infoFont, scaled(PingX), y);
            }
        } finally {
            ctx.Restore();
        }
    }
}
