using holyguardstablist.gui.element;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace holyguardstablist.gui;

public sealed class PlayerListHud : HudElement {
    private readonly PlayerList _mod;
    private readonly KeyHandler _keyHandler;
    private readonly long _gameTickListenerId;
    private List<string> _players = [];

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

        if (!force && _players.SequenceEqual(players)) return;

        Compose(_players = players);
        TryOpen();
    }

    private void Compose(List<string> players) {
        if (players.Count == 0) return;

        ElementBounds table = new() {
            Alignment = EnumDialogArea.CenterTop,
            BothSizing = ElementSizing.Fixed,
            fixedWidth = GuiHolyguardsTable.Width,
            fixedHeight = GuiHolyguardsTable.Height
        };

        SingleComposer = capi.Gui
            .CreateCompo("holyguardstablist", new ElementBounds {
                Alignment = EnumDialogArea.CenterTop,
                BothSizing = ElementSizing.FitToChildren,
                fixedOffsetY = 22
            })
            .BeginChildElements()
            .AddStaticElement(new GuiHolyguardsTable(_mod, players, table))
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
