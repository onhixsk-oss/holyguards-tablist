using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HolyguardsPlayerList;

public sealed class HolyguardsPlayerListServerModSystem : ModSystem
{
    private const string DefaultLogo = "https://raw.githubusercontent.com/onhixsk-oss/holyguards-tablist/main/assets/PlayerLists_Holyguards_logo_300x200.jpg";

    private ICoreServerAPI sapi;
    private HolyguardsSettings settings;
    private bool appliedOnce;

    // Run after PlayerLists so its _config object already exists.
    public override double ExecuteOrder() => 999.0;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        settings = api.LoadModConfig<HolyguardsSettings>("holyguardstablist.json") ?? new HolyguardsSettings();
        api.StoreModConfig(settings, "holyguardstablist.json");

        ApplyBranding();
        api.Event.RegisterCallback(_ => ApplyBranding(), 1000);

        api.Logger.Notification("[holyguardstablist] Server-only Holyguards branding bridge active. Clients only need original PlayerLists.");
    }

    private void ApplyBranding()
    {
        try
        {
            ModSystem original = sapi.ModLoader.GetModSystem("playerlist.PlayerList");
            if (original == null)
            {
                if (!appliedOnce)
                {
                    sapi.Logger.Warning("[holyguardstablist] Original PlayerLists mod system not found. Install original PlayerLists 2.3.7 on the server.");
                }
                return;
            }

            FieldInfo configField = original.GetType().GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic);
            object config = configField?.GetValue(original);
            if (config == null)
            {
                if (!appliedOnce)
                {
                    sapi.Logger.Warning("[holyguardstablist] PlayerLists config object is not ready yet.");
                }
                return;
            }

            SetProperty(config, "Logo", string.IsNullOrWhiteSpace(settings.Logo) ? DefaultLogo : settings.Logo);
            SetProperty(config, "Header", settings.Header);
            SetProperty(config, "Footer", settings.Footer);
            SetProperty(config, "Thresholds", settings.Thresholds ?? new[] { 100, 250, 500 });
            SetProperty(config, "MaxNameLength", settings.MaxNameLength);

            if (!appliedOnce)
            {
                appliedOnce = true;
                sapi.Logger.Notification("[holyguardstablist] Holyguards branding applied to original PlayerLists server config.");
                sapi.Logger.Notification("[holyguardstablist] Client dependency remains original PlayerLists from ModDB; this Holyguards mod is server-only.");
            }
        }
        catch (Exception e)
        {
            sapi.Logger.Error("[holyguardstablist] Could not apply PlayerLists branding: {0}", e);
        }
    }

    private static void SetProperty(object target, string name, object value)
    {
        PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null || !property.CanWrite) return;
        property.SetValue(target, value);
    }
}

public sealed class HolyguardsSettings
{
    public string Logo { get; set; } = "https://raw.githubusercontent.com/onhixsk-oss/holyguards-tablist/main/assets/PlayerLists_Holyguards_logo_300x200.jpg";
    public string Header { get; set; } = "HOLYGUARDS";
    public string Footer { get; set; } = null;
    public int[] Thresholds { get; set; } = new[] { 100, 250, 500 };
    public int MaxNameLength { get; set; } = 20;
}
