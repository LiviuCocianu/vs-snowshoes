using Snowshoes.classes;
using Snowshoes.utils;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Snowshoes
{
    public class SnowshoesModSystem : ModSystem
    {
        public static ICoreAPI api;
        public ICoreClientAPI capi;
        public ICoreServerAPI sapi;
        private ILogger logger;

        public static SnowshoesModSystem GetInstance() => api.ModLoader.GetModSystem<SnowshoesModSystem>();

        public ILogger Logger {
            get {  return logger; }
        }

        public override void Start(ICoreAPI api)
        {
            logger = Mod.Logger;
            SnowshoesModSystem.api = api;

            api.RegisterBlockClass(Mod.Info.ModID + ".snowlayer", typeof(BlockSnowshoesSnowLayer));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            int radius = 1;
            bool handleSnowWalking(IPlayer pl, ref AnimationMetaData meta, ref EnumHandling handling)
            {
                long listener = 0;

                if (!meta.Code.Contains("walk")
                    && !meta.Code.Contains("sprint")
                    && !meta.Code.Contains("sneak")) return false;

                // Frequent checks are needed so players don't sink in the snow
                listener = api.Event.RegisterGameTickListener(fl =>
                {
                    EntityControls ec = pl.Entity.Controls;

                    // Stop listener if player stops moving
                    if (!ec.Forward && !ec.Backward && !ec.Left && !ec.Right)
                    {
                        api.Event.UnregisterGameTickListener(listener);
                        return;
                    }

                    IBlockAccessor bacc = api.World.BlockAccessor;

                    // Check snow in a radius
                    for (int i = -radius; i <= radius; i++)
                    {
                        for (int j = -radius; j <= radius; j++)
                        {
                            BlockPos blPos = pl.Entity.Pos.AsBlockPos.AddCopy(i, 0, j);
                            Block bl = bacc.GetBlock(blPos);

                            if (!AssetUtils.IsSnowloggable(bl)) continue;

                            float condition = InventoryUtils.GetShoesCondition(pl);

                            // If condition drops below 25%, negate snowshoe's effect
                            if (condition == -1) continue;
                            if (condition < 0.25) continue;

                            int currentLayer = AssetUtils.GetSnowloggedLayer(bl);

                            // Theoretically, this should never trigger, since the block is confirmed to be snow
                            if (currentLayer == -1) continue;

                            // If player is inside snow layer, place them on top
                            if (i == 0 && i == j) PlacePlayerOnTop(pl, bl);

                            // Set snowlogged block to my custom snow
                            bacc.SetBlock(AssetUtils.GetSnowloggedBlockId(bl, currentLayer, "snowshoes"), blPos);

                            // Set my custom snow back to normal after some time
                            RevertSnowloggedBlock(blPos, currentLayer);
                        }
                    }
                }, 50);

                return false;
            }

            capi.Event.IsPlayerReady += (ref EnumHandling handling) =>
            {
                IClientPlayer pl = capi.World.Player;

                if (pl == null)
                {
                    logger.Warning("Couldn't register snow walking logic for " + pl.PlayerName + ".. Snowshoes won't work for this player!");
                    return false;
                }

                bool handleSnowWalkingNoPl(ref AnimationMetaData meta, ref EnumHandling handling) => handleSnowWalking(pl, ref meta, ref handling);
                pl.Entity.OtherAnimManager.OnStartAnimation += handleSnowWalkingNoPl;

                return true;
            };
        }


        private void PlacePlayerOnTop(IPlayer pl, Block bl)
        {
            if (bl == null) return;
            if (bl.CollisionBoxes == null) return;

            double actualY = pl.Entity.Pos.Y;
            int normalizedY = (int)actualY;
            float layerHeight = bl.CollisionBoxes[0].Height;

            if (actualY < normalizedY + layerHeight)
            {
                double diff = (normalizedY + layerHeight) - actualY;
                pl.Entity.Pos.Add(0, diff, 0);
            }
        }

        private long RevertSnowloggedBlock(BlockPos blPos, int currentLayer)
        {
            return api.World.RegisterCallback((fl) =>
            {
                bool plFilter(IPlayer pl) => InventoryUtils.AreSnowshoesEquipped(pl).Item1;
                Block bl = api.World.BlockAccessor.GetBlock(blPos);

                if (!AssetUtils.IsSnowloggable(bl)) return;
                if (AssetUtils.GetSnowloggedLayer(bl) == -1) return; // Check if there is still snow at this block

                // If a player with snowshoes is still stood on this snow layer, keep checking and don't revert it yet
                if (api.World.GetPlayersAround(blPos.ToVec3d(), 1, 1, plFilter).Length > 0)
                {
                    RevertSnowloggedBlock(blPos, currentLayer);
                    return;
                }

                // Revert snow layer to its vanilla version
                api.World.BlockAccessor.SetBlock(AssetUtils.GetSnowloggedBlockId(bl, currentLayer, "game"), blPos);
            }, 500);
        }
    }
}
