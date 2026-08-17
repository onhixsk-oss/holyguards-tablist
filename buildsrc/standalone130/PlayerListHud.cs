using holyguardstablist.gui.element;
using holyguardstablist.util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace holyguardstablist.gui;

public sealed class PlayerListHud : HudElement {
    private const double TopOffset = 22;
    private const double HorizontalSafeMargin = 24;
    private const double BottomSafeMargin = 24;

    private readonly PlayerList _mod;
    private readonly KeyHandler _keyHandler;
    private readonly long _gameTickListenerId;
    private List<string> _players = [];

    private int _lastFrameWidth = -1;
    private int _lastFrameHeight = -1;
    private double _lastGuiScale = -1;

    public PlayerListHud(PlayerList mod) : base((ICoreClientAPI)mod.Api) {
        _mod = mod;
        _keyHandler = new KeyHandler(capi);
        _gameTickListenerId = capi.Event.RegisterGameTickListener(_ => UpdateList(), 1000);
    }

    public void UpdateList(bool force = false) {
        List<string> players = capi.World.AllOnlinePlayers
            .OrderBy(player => player.PlayerName)
            .Select(player => player.PlayerUID)
            .ToList();

        bool viewportChanged = HasViewportChanged();
        if (!force && !viewportChanged && _players.SequenceEqual(players)) return;

        CaptureViewportState();
        Compose(_players = players);
        TryOpen();
    }

    private bool HasViewportChanged() {
        return _lastFrameWidth != capi.Render.FrameWidth
            || _lastFrameHeight != capi.Render.FrameHeight
            || Math.Abs(_lastGuiScale - RuntimeEnv.GUIScale) > 0.001;
    }

    private void CaptureViewportState() {
        _lastFrameWidth = capi.Render.FrameWidth;
        _lastFrameHeight = capi.Render.FrameHeight;
        _lastGuiScale = RuntimeEnv.GUIScale;
    }

    private double PreferredMaxScale() {
        int frameWidth = capi.Render.FrameWidth;
        if (frameWidth <= 1920) return 0.86;
        if (frameWidth <= 2560) return 0.94;
        return 1.0;
    }

    private double GetLayoutScale() {
        double guiScale = Math.Max(0.01, RuntimeEnv.GUIScale);
        double logicalWidth = capi.Render.FrameWidth / guiScale;
        double logicalHeight = capi.Render.FrameHeight / guiScale;

        double availableWidth = Math.Max(1, logicalWidth - HorizontalSafeMargin * 2);
        double availableHeight = Math.Max(1, logicalHeight - TopOffset - BottomSafeMargin);

        double widthScale = availableWidth / GuiHolyguardsTable.Width;
        double heightScale = availableHeight / GuiHolyguardsTable.Height;
        double preferredMax = PreferredMaxScale();

        return Math.Clamp(Math.Min(Math.Min(widthScale, heightScale), preferredMax), 0.1, preferredMax);
    }

    private void Compose(List<string> players) {
        if (players.Count == 0) return;

        double layoutScale = GetLayoutScale();
        double width = GuiHolyguardsTable.Width * layoutScale;
        double height = GuiHolyguardsTable.Height * layoutScale;

        ElementBounds content = new() {
            Alignment = EnumDialogArea.CenterTop,
            BothSizing = ElementSizing.Fixed,
            fixedWidth = width,
            fixedHeight = height
        };

        ElementBounds table = new() {
            Alignment = EnumDialogArea.LeftTop,
            BothSizing = ElementSizing.Fixed,
            fixedWidth = width,
            fixedHeight = height
        };

        SingleComposer = capi.Gui
            .CreateCompo("holyguardstablist", new ElementBounds {
                Alignment = EnumDialogArea.CenterTop,
                BothSizing = ElementSizing.FitToChildren,
                fixedOffsetY = TopOffset * layoutScale
            })
            .BeginChildElements(content)
            .AddStaticElement(new GuiHolyguardsTable(_mod, players, table, layoutScale))
            .EndChildElements()
            .Compose();
    }

    public override bool ShouldReceiveRenderEvents() => _keyHandler.IsKeyComboActive();
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

    public override void Dispose() {
        base.Dispose();
        _keyHandler.Dispose();
        capi.Event.UnregisterGameTickListener(_gameTickListenerId);
    }
}
