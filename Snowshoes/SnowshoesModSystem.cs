using Snowshoes.src.blocktypes;
using Snowshoes.src.itemtypes;
using Snowshoes.src.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Snowshoes
{
    public class SnowshoesModSystem : ModSystem
    {
        public static ICoreAPI api;
        public ICoreClientAPI capi;
        public ICoreServerAPI sapi;

        private static int CHECKING_FREQUENCY = 50;
        private static int SECONDS_BEFORE_DEPLETION = 5;

        private ILogger logger;
        private Dictionary<string, int> movingWithSnowshoes = new(); // only used server-side

        public static SnowshoesModSystem GetInstance() => api.ModLoader.GetModSystem<SnowshoesModSystem>();

        public ILogger Logger {
            get {  return logger; }
        }

        public override void Start(ICoreAPI api)
        {
            logger = Mod.Logger;
            SnowshoesModSystem.api = api;

            api.RegisterBlockClass(Mod.Info.ModID + ".snowlayer", typeof(SnowshoesSnowLayer));
            api.RegisterItemClass(Mod.Info.ModID + ".snowshoes", typeof(SnowshoesItem));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            api.Network.RegisterChannel("snowshoes-durability");

            HandleDurabilityDepletion(api);

            api.Event.PlayerLeave += (pl) =>
            {
                movingWithSnowshoes.Remove(pl.PlayerUID);
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            capi.Event.IsPlayerReady += (ref EnumHandling handling) =>
            {
                IClientPlayer pl = capi.World.Player;

                if (pl == null)
                {
                    logger.Warning("Couldn't register snow walking logic for " + pl.PlayerName + ".. Snowshoes won't work for this player!");
                    return false;
                }

                bool HandleSnowWalkingNoPlayer(ref AnimationMetaData meta, ref EnumHandling handling) => HandleSnowWalking(api, pl, ref meta, ref handling);
                pl.Entity.OtherAnimManager.OnStartAnimation += HandleSnowWalkingNoPlayer;

                return true;
            };
        }

        private bool HandleSnowWalking(ICoreClientAPI api, IPlayer pl, ref AnimationMetaData meta, ref EnumHandling handling)
        {
            int radius = 1;
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

                if (!InventoryUtils.AreSnowshoesEquipped(pl).Item1) return;

                IBlockAccessor bacc = api.World.BlockAccessor;

                // Check snow in a radius
                for (int i = -radius; i <= radius; i++)
                {
                    for (int j = -radius; j <= radius; j++)
                    {
                        BlockPos blPos = pl.Entity.Pos.AsBlockPos.AddCopy(i, 0, j);
                        Block bl = bacc.GetBlock(blPos);

                        if (!AssetUtils.IsSnowloggable(bl)) continue;

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
            }, CHECKING_FREQUENCY);

            return false;
        }

        private void HandleDurabilityDepletion(ICoreServerAPI api)
        {
            api.Event.RegisterGameTickListener(fl =>
            {
                api.World.AllOnlinePlayers.Foreach((pl) =>
                {
                    IServerPlayer spl = (IServerPlayer)pl;

                    // Increment based on snowshoe usage for durability depletion
                    if(spl.Entity.ServerControls.TriesToMove && InventoryUtils.AreSnowshoesEquipped(spl).Item1)
                    {
                        if (!movingWithSnowshoes.ContainsKey(spl.PlayerUID))
                        {
                            movingWithSnowshoes.Add(spl.PlayerUID, 0);
                        }
                        else
                        {
                            movingWithSnowshoes[spl.PlayerUID] += 1;
                        }
                    }

                    if (movingWithSnowshoes.ContainsKey(spl.PlayerUID))
                    {
                        Tuple<bool, ItemStack> res = InventoryUtils.AreSnowshoesEquipped(spl);
                        float timeMultiplier = 1f;

                        // Even if value inside movingWithSnowshoes increases only when having snowshoes equipped, performing checks on all players
                        // regardless of their equipment would be pretty bad. Entry inside this dictionary doesn't get removed in player stops moving
                        if (res.Item1)
                        {
                            // If not walking on snow, durability will deplete slightly faster
                            if (!AssetUtils.IsSnowloggable(api.World.BlockAccessor.GetBlock(spl.Entity.Pos.AsBlockPos)))
                            {
                                timeMultiplier = 0.8f;
                            }

                            // If this player moved with snowshoes equipped for SECONDS_BEFORE_DECAY seconds in total, decrease 1 durability
                            if (movingWithSnowshoes[spl.PlayerUID] >= (20 * SECONDS_BEFORE_DEPLETION * timeMultiplier))
                            {
                                CollectibleObject col = res.Item2.Collectible;

                                col.SetDurability(res.Item2, col.GetRemainingDurability(res.Item2) - 1);
                                InventoryUtils.MarkSnowshoesSlotDirty(spl);

                                movingWithSnowshoes[spl.PlayerUID] = 0;
                            }
                        }
                    }
                });
            }, CHECKING_FREQUENCY);
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
