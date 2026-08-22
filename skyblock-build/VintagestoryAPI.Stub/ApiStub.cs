using System;

namespace Vintagestory.API.MathTools
{
    public class Vec3d
    {
        public double X;
        public double Y;
        public double Z;
    }
}

namespace Vintagestory.API.Common.Entities
{
    public class Entity
    {
        public virtual void TeleportToDouble(double x, double y, double z, Action onTeleported = null) { }
    }
}

namespace Vintagestory.API.Common
{
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;

    public delegate TextCommandResult OnCommandDelegate(TextCommandCallingArgs args);

    public class Caller
    {
        public Vec3d Pos;
        public Entity Entity { get; set; }
        public string GetName() => "caller";
    }

    public class TextCommandCallingArgs
    {
        public Caller Caller;
    }

    public enum EnumCommandStatus
    {
        NoSuchCommand,
        Success,
        Deferred,
        Error,
        UnknownLegacy
    }

    public class TextCommandResult
    {
        public string ErrorCode;
        public string StatusMessage;
        public EnumCommandStatus Status;
        public object Data;

        public static TextCommandResult Success(string message = "", object data = null) => new TextCommandResult();
        public static TextCommandResult Error(string message, string errorCode = "") => new TextCommandResult();
    }

    public interface IChatCommand
    {
        IChatCommand WithDescription(string description);
        IChatCommand WithExamples(params string[] examples);
        IChatCommand RequiresPrivilege(string privilege);
        IChatCommand RequiresPlayer();
        IChatCommand BeginSubCommand(string name);
        IChatCommand EndSubCommand();
        IChatCommand HandleWith(OnCommandDelegate handler);
    }

    public interface IChatCommandApi
    {
        IChatCommand Create(string name);
    }

    public interface ICoreAPI
    {
        IChatCommandApi ChatCommands { get; }
    }

    public abstract class ModSystem
    {
        public virtual void StartServerSide(ICoreServerAPI api) { }
    }
}

namespace Vintagestory.API.Server
{
    using Vintagestory.API.Common;

    public interface IWorldManagerAPI
    {
        int[] DefaultSpawnPosition { get; }
        void SetDefaultSpawnPosition(int x, int y, int z);
    }

    public interface ICoreServerAPI : ICoreAPI
    {
        IWorldManagerAPI WorldManager { get; }
    }
}
