using Snowshoes.src.blocktypes;
using Snowshoes.src.config;
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
        public SnowshoesConfig config;

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

            api.RegisterBlockClass(Mod.Info.ModID + ".SnowLayer", typeof(SnowshoesSnowLayer));
            api.RegisterItemClass("SnowshoesPlain", typeof(SnowshoesPlainItem));
            api.RegisterItemClass("SnowshoesFur", typeof(SnowshoesFurItem));
            api.RegisterItemClass("SnowshoesOld", typeof(SnowshoesOldItem));

            TryToLoadConfig(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            HandleDurabilityDepletion(api);

            api.Event.PlayerLeave += (pl) =>
            {
                movingWithSnowshoes.Remove(pl.PlayerUID);
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.IsPlayerReady += (ref EnumHandling handling) =>
            {
                IClientPlayer pl = api.World.Player;

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

        private void TryToLoadConfig(ICoreAPI api) {
            try {
                config = api.LoadModConfig<SnowshoesConfig>("Snowshoes.json");

                if (config == null) {
                    config = new SnowshoesConfig();
                } else {
                    SnowshoesConfig defaults = new();

                    // Validation checks
                    if(config.checkRadius < 0) {
                        api.Logger.Warning("Config property 'checkRadius' cannot be negative! Please change it! Will default to original value");
                        config.checkRadius = defaults.checkRadius;
                    } else if (config.checkRadius >= 10) {
                        api.Logger.Warning("Config property 'checkRadius' is worryingly large! Might cause unexpected issues with world and performance! Will not change the value, but caution is advised");
                    }

                    if (config.radiusCheckFrequency < 0) {
                        api.Logger.Warning("Config property 'radiusCheckFrequency' cannot be negative! Please change it! Will default to original value");
                        config.radiusCheckFrequency = defaults.radiusCheckFrequency;
                    }

                    if (config.durabilityAmount < 1) {
                        api.Logger.Warning("Config property 'durabilityAmount' cannot go below 1! Please change it! Will default to original value");
                        config.durabilityAmount = defaults.durabilityAmount;
                    }

                    if (config.secondsBeforeDepletion < 1) {
                        api.Logger.Warning("Config property 'secondsBeforeDepletion' cannot be less than 1! Please change it! Will default to original value");
                        config.secondsBeforeDepletion = defaults.secondsBeforeDepletion;
                    }

                    if (config.timeReductionMultiplier < 0) {
                        api.Logger.Warning("Config property 'timeReductionMultiplier' cannot be negative! Please change it! Will default to original value");
                        config.timeReductionMultiplier = defaults.timeReductionMultiplier;
                    } else if (config.timeReductionMultiplier > 1) {
                        api.Logger.Warning("Config property 'timeReductionMultiplier' cannot exceed 1! Please change it! Will set it to 1 instead");
                        config.timeReductionMultiplier = 1;
                    }

                    if (config.flaxRepairPercentage < 0) {
                        api.Logger.Warning("Config property 'flaxRepairPercentage' cannot be negative! Please change it! Will default to original value");
                        config.flaxRepairPercentage = defaults.flaxRepairPercentage;
                    } else if (config.flaxRepairPercentage > 1) {
                        api.Logger.Warning("Config property 'flaxRepairPercentage' cannot exceed 1! Please change it! Will set it to 1 instead");
                        config.flaxRepairPercentage = 1;
                    }

                    if (config.ropeRepairPercentage < 0) {
                        api.Logger.Warning("Config property 'ropeRepairPercentage' cannot be negative! Please change it! Will default to original value");
                        config.ropeRepairPercentage = defaults.ropeRepairPercentage;
                    } else if (config.ropeRepairPercentage > 1) {
                        api.Logger.Warning("Config property 'ropeRepairPercentage' cannot exceed 1! Please change it! Will set it to 1 instead");
                        config.ropeRepairPercentage = 1;
                    }

                    if (config.leatherRepairPercentage < 0) {
                        api.Logger.Warning("Config property 'leatherRepairPercentage' cannot be negative! Please change it! Will default to original value");
                        config.leatherRepairPercentage = defaults.leatherRepairPercentage;
                    } else if (config.leatherRepairPercentage > 1) {
                        api.Logger.Warning("Config property 'leatherRepairPercentage' cannot exceed 1! Please change it! Will set it to 1 instead");
                        config.leatherRepairPercentage = 1;
                    }
                }

                api.StoreModConfig<SnowshoesConfig>(config, "Snowshoes.json");
            } catch (Exception e) {
                Mod.Logger.Error("Could not load 'Snowshoes 'config! Loading default settings instead.");
                Mod.Logger.Error(e);
                config = new SnowshoesConfig();
            }
        }

        private bool HandleSnowWalking(ICoreClientAPI api, IPlayer pl, ref AnimationMetaData meta, ref EnumHandling handling)
        {
            int radius = config.checkRadius;
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
            }, config.radiusCheckFrequency);

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

                        // Convert remnant old snowshoes to the new itemtype before doing anything else
                        if (InventoryUtils.AreOldSnowshoesEquipped(spl))
                        {
                            SnowshoesOldItem.PatchSnowshoesToVersion2(InventoryUtils.GetSnowshoes(spl), (newSh) =>
                            {
                                InventoryUtils.SetFootAndMarkDirty(spl, new(newSh));
                            });

                            return;
                        }

                        // Even if value inside movingWithSnowshoes increases only when having snowshoes equipped, performing checks on all players
                        // regardless of their equipment would be pretty bad. Entry inside this dictionary doesn't get removed in player stops moving
                        if (res.Item1)
                        {
                            // If not walking on snow, durability will deplete slightly faster
                            if (!AssetUtils.IsSnowloggable(api.World.BlockAccessor.GetBlock(spl.Entity.Pos.AsBlockPos)))
                            {
                                timeMultiplier = config.timeReductionMultiplier;
                            }

                            float updatedSeconds = (float)Math.Round(Math.Max(1, config.secondsBeforeDepletion * timeMultiplier));

                            // If this player moved with snowshoes equipped for SECONDS_BEFORE_DECAY seconds in total, decrease 1 durability
                            if (movingWithSnowshoes[spl.PlayerUID] >= (20 * updatedSeconds))
                            {
                                CollectibleObject col = res.Item2.Collectible;
                                int subtracted = col.GetRemainingDurability(res.Item2) - config.durabilityAmount;

                                if (subtracted <= 0) {
                                    InventoryUtils.GetFootwareSlot(spl).Itemstack = null;

                                    // If they are fur snowshoes, give player the fur boots back
                                    if(SnowshoesFurItem.VARIANTS.ContainsKey(col.FirstCodePart(3)))
                                    {
                                        string furCode = SnowshoesFurItem.VARIANTS.Get(col.FirstCodePart(3));
                                        ItemStack furBoots = new(pl.Entity.World.SearchItems(furCode)[0]);
                                        ItemSlot shoesSlot = InventoryUtils.GetFootwareSlot(pl);

                                        furBoots.Attributes.SetFloat("condition", res.Item2.Attributes.GetFloat("condition", 1));
                                        shoesSlot.Itemstack = furBoots;
                                    }

                                    api.World.PlaySoundAt(
                                        new AssetLocation("game:sounds/effect/toolbreak"),
                                        spl.Entity,
                                        null, true, 16
                                    );
                                } else {
                                    col.SetDurability(res.Item2, subtracted);
                                }

                                InventoryUtils.MarkSnowshoesSlotDirty(spl);

                                movingWithSnowshoes[spl.PlayerUID] = 0;
                            }
                        }
                    }
                });
            }, config.radiusCheckFrequency);
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
