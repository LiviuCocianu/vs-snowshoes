using Snowshoes.classes;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Snowshoes
{
    public class SnowshoesModSystem : ModSystem
    {
        private static ICoreAPI api;
        public ICoreClientAPI capi;
        public ICoreServerAPI sapi;
        private ILogger logger;

        public static SnowshoesModSystem GetInstance() => api.ModLoader.GetModSystem<SnowshoesModSystem>();

        public ILogger Logger {
            get {  return logger; }
        }

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass(Mod.Info.ModID + ".snowshoes", typeof(SnowshoesItem));
            logger = Mod.Logger;
            SnowshoesModSystem.api = api;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("snowshoes:hello"));
            sapi = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("snowshoes:hello"));
            capi = api;
        }
    }
}
