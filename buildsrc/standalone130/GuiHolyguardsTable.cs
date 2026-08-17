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
        // resized with the same uniform layout scale used for the text positions.
        Bounds.fixedWidth = Width * _layoutScale;
        Bounds.fixedHeight = Height * _layoutScale;
        _background = new HeaderImage(mod, "holyguardstablist:textures/gui/tablist.png", Bounds);
    }

    public override void ComposeElements(Context ctx, ImageSurface surface) {
        Bounds.CalcWorldBounds();
        _background.ComposeElements(ctx, surface);

        // Vintagestory's Cairo Context wrapper does not expose Context.Scale().
        // Keep the approved 720x480 geometry responsive by scaling every authored
        // coordinate before converting it to GUI pixels. At the canonical scale
        // this is pixel-identical to the reference composition.
        for (int i = 0; i < _players.Count; i++) {
            PlayerData player = _players[i];
            double y = Bounds.drawY + scaled((FirstRowY + i * RowHeight) * _layoutScale);

            CairoFont nameFont = player.Font;
            nameFont.SetupContext(ctx);
            _textUtil.DrawTextLine(
                ctx,
                player.Name,
                nameFont,
                Bounds.drawX + scaled(NameX * _layoutScale),
                y
            );

            CairoFont infoFont = Util.DefaultFont;
            infoFont.SetupContext(ctx);
            _textUtil.DrawTextLine(
                ctx,
                "Hráč",
                infoFont,
                Bounds.drawX + scaled(RankX * _layoutScale),
                y
            );

            string ping = player.Ping >= 0 ? $"{player.Ping} ms" : "—";
            _textUtil.DrawTextLine(
                ctx,
                ping,
                infoFont,
                Bounds.drawX + scaled(PingX * _layoutScale),
                y
            );
        }
    }
}
