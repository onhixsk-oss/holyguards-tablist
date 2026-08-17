using Cairo;
using holyguardstablist.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace holyguardstablist.gui.element;

public sealed class GuiHolyguardsTable : GuiElement {
    // Reference canvas. Every coordinate below is authored against this exact
    // 720x480 layout so the Holyguards design never changes proportions.
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

        // Scale the full canvas as one unit. The background and every text
        // coordinate use the same factor, which keeps the screenshot layout
        // pixel-for-pixel proportional on smaller resolutions.
        Bounds.fixedWidth = Width * _layoutScale;
        Bounds.fixedHeight = Height * _layoutScale;
        _background = new HeaderImage(mod, "holyguardstablist:textures/gui/tablist.png", Bounds);
    }

    public override void ComposeElements(Context ctx, ImageSurface surface) {
        Bounds.CalcWorldBounds();
        _background.ComposeElements(ctx, surface);

        using CairoFont infoFont = ScaleFont(Util.DefaultFont);

        for (int i = 0; i < _players.Count; i++) {
            PlayerData player = _players[i];
            double y = Bounds.drawY + scaled(Layout(FirstRowY + i * RowHeight));

            using CairoFont nameFont = ScaleFont(player.Font);
            nameFont.SetupContext(ctx);
            _textUtil.DrawTextLine(ctx, player.Name, nameFont, Bounds.drawX + scaled(Layout(NameX)), y);

            infoFont.SetupContext(ctx);
            _textUtil.DrawTextLine(ctx, "Hráč", infoFont, Bounds.drawX + scaled(Layout(RankX)), y);

            string ping = player.Ping >= 0 ? $"{player.Ping} ms" : "—";
            _textUtil.DrawTextLine(ctx, ping, infoFont, Bounds.drawX + scaled(Layout(PingX)), y);
        }
    }

    private double Layout(double value) => value * _layoutScale;

    private CairoFont ScaleFont(CairoFont source) {
        return source.Clone().WithFontSize((float)(source.UnscaledFontsize * _layoutScale));
    }
}
