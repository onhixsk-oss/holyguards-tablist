using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HolyguardsSkyblock
{
    public sealed class HolyguardsIslandCommands : ModSystem
    {
        private ICoreServerAPI sapi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            IChatCommand root = api.ChatCommands.Create("island")
                .WithDescription("Holyguards Skyblock island commands")
                .WithExamples("/island", "/island home", "/island rescue", "/island info")
                .RequiresPrivilege("chat")
                .RequiresPlayer()
                .HandleWith(OnHome);

            root.BeginSubCommand("home")
                .WithDescription("Teleport back to the Holyguards Skyblock island")
                .RequiresPrivilege("chat")
                .RequiresPlayer()
                .HandleWith(OnHome)
                .EndSubCommand();

            root.BeginSubCommand("spawn")
                .WithDescription("Teleport back to the Holyguards Skyblock island")
                .RequiresPrivilege("chat")
                .RequiresPlayer()
                .HandleWith(OnHome)
                .EndSubCommand();

            root.BeginSubCommand("rescue")
                .WithDescription("Rescue yourself back to the Holyguards Skyblock island")
                .RequiresPrivilege("chat")
                .RequiresPlayer()
                .HandleWith(OnHome)
                .EndSubCommand();

            root.BeginSubCommand("info")
                .WithDescription("Show the current Holyguards island spawn coordinates")
                .RequiresPrivilege("chat")
                .RequiresPlayer()
                .HandleWith(OnInfo)
                .EndSubCommand();

            root.BeginSubCommand("help")
                .WithDescription("Show Holyguards island command help")
                .RequiresPrivilege("chat")
                .RequiresPlayer()
                .HandleWith(OnHelp)
                .EndSubCommand();

            root.BeginSubCommand("sethome")
                .WithDescription("Set the shared Holyguards island home to your current position")
                .RequiresPrivilege("controlserver")
                .RequiresPlayer()
                .HandleWith(OnSetHome)
                .EndSubCommand();
        }

        private TextCommandResult OnHome(TextCommandCallingArgs args)
        {
            if (args == null || args.Caller == null || args.Caller.Entity == null)
            {
                return TextCommandResult.Error("This command must be used by an in-game player.");
            }

            int[] spawn = sapi.WorldManager.DefaultSpawnPosition;
            if (spawn == null || spawn.Length < 3)
            {
                return TextCommandResult.Error("Holyguards island spawn is not available.");
            }

            args.Caller.Entity.TeleportToDouble(spawn[0] + 0.5, spawn[1] + 0.1, spawn[2] + 0.5);
            return TextCommandResult.Success("Teleported to the Holyguards Skyblock island.");
        }

        private TextCommandResult OnInfo(TextCommandCallingArgs args)
        {
            int[] spawn = sapi.WorldManager.DefaultSpawnPosition;
            if (spawn == null || spawn.Length < 3)
            {
                return TextCommandResult.Error("Holyguards island spawn is not available.");
            }

            return TextCommandResult.Success(
                "Holyguards island home: X " + spawn[0] + ", Y " + spawn[1] + ", Z " + spawn[2] + "."
            );
        }

        private TextCommandResult OnHelp(TextCommandCallingArgs args)
        {
            return TextCommandResult.Success(
                "Holyguards commands: /island, /island home, /island rescue, /island info. Admin: /island sethome"
            );
        }

        private TextCommandResult OnSetHome(TextCommandCallingArgs args)
        {
            if (args == null || args.Caller == null || args.Caller.Pos == null)
            {
                return TextCommandResult.Error("This command must be used by an in-game player.");
            }

            int x = (int)Math.Floor(args.Caller.Pos.X);
            int y = (int)Math.Floor(args.Caller.Pos.Y);
            int z = (int)Math.Floor(args.Caller.Pos.Z);

            sapi.WorldManager.SetDefaultSpawnPosition(x, y, z);
            return TextCommandResult.Success(
                "Holyguards island home set to X " + x + ", Y " + y + ", Z " + z + "."
            );
        }
    }
}
