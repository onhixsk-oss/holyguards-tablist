using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HolyguardsPlayerList;

/// <summary>
/// Server-side companion system bundled inside the same PlayerLists 2.3.7 ZIP.
/// The original PlayerLists.dll remains byte-identical so the client can use the
/// official PlayerLists 2.3.7 downloaded from Vintage Story ModDB and keep the
/// exact protobuf/network schema.
/// </summary>
public sealed class HolyguardsPlayerListsEmbeddedServerSystem : ModSystem
{
    private const string LogoUrl = "https://raw.githubusercontent.com/onhixsk-oss/holyguards-tablist/main/assets/PlayerLists_Holyguards_logo_300x200.jpg";

    private ICoreServerAPI sapi;
    private bool applied;

    // Apply after the original playerlist.PlayerList has created its config.
    public override double ExecuteOrder() => 999.0;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        Apply();
        api.Event.RegisterCallback(_ => Apply(), 1000);
    }

    private void Apply()
    {
        if (applied || sapi == null) return;

        try
        {
            ModSystem original = sapi.ModLoader.GetModSystem("playerlist.PlayerList");
            if (original == null)
            {
                sapi.Logger.Warning("[playerlists-holyguards] Original PlayerLists system not ready yet.");
                return;
            }

            FieldInfo configField = original.GetType().GetField(
                "_config",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            object config = configField?.GetValue(original);
            if (config == null)
            {
                sapi.Logger.Warning("[playerlists-holyguards] Original PlayerLists config not ready yet.");
                return;
            }

            Set(config, "Logo", LogoUrl);
            Set(config, "Header", "HOLYGUARDS");
            Set(config, "Footer", null);
            Set(config, "Thresholds", new[] { 100, 250, 500 });
            Set(config, "MaxNameLength", 20);

            applied = true;
            sapi.Logger.Notification("[playerlists-holyguards] Holyguards branding embedded into PlayerLists 2.3.7 server config.");
            sapi.Logger.Notification("[playerlists-holyguards] Clients remain compatible with the official PlayerLists 2.3.7 from ModDB.");
        }
        catch (Exception e)
        {
            sapi.Logger.Error("[playerlists-holyguards] Failed to apply embedded branding: {0}", e);
        }
    }

    private static void Set(object target, string propertyName, object value)
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (property?.CanWrite == true)
        {
            property.SetValue(target, value);
        }
    }
}
