using Cairo;
using holyguardstablist.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace holyguardstablist.gui.element;

public sealed class GuiHolyguardsTable : GuiElement {
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

    public GuiHolyguardsTable(PlayerList mod, List<string> players, ElementBounds bounds)
        : base(mod.Api as ICoreClientAPI, bounds) {
        _players = players
            .Take(MaxRows)
            .Select(uid => new PlayerData(mod, api.World.PlayerByUid(uid)))
            .ToList();

        _background = new HeaderImage(mod, "holyguardstablist:textures/gui/tablist.png", bounds);
        Bounds.fixedWidth = Width;
        Bounds.fixedHeight = Height;
    }

    public override void ComposeElements(Context ctx, ImageSurface surface) {
        Bounds.CalcWorldBounds();
        _background.ComposeElements(ctx, surface);

        for (int i = 0; i < _players.Count; i++) {
            PlayerData player = _players[i];
            double y = Bounds.drawY + scaled(FirstRowY + i * RowHeight);

            CairoFont nameFont = player.Font;
            nameFont.SetupContext(ctx);
            _textUtil.DrawTextLine(ctx, player.Name, nameFont, Bounds.drawX + scaled(NameX), y);

            CairoFont infoFont = Util.DefaultFont;
            infoFont.SetupContext(ctx);
            _textUtil.DrawTextLine(ctx, "Hráč", infoFont, Bounds.drawX + scaled(RankX), y);

            string ping = player.Ping >= 0 ? $"{player.Ping} ms" : "—";
            _textUtil.DrawTextLine(ctx, ping, infoFont, Bounds.drawX + scaled(PingX), y);
        }
    }
}
