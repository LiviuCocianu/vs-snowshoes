using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace Snowshoes.src.itemtypes
{
    internal partial class SnowshoesFurItem : ItemWearable
    {
        private static readonly SnowshoesItem snowshoes = new(SnowshoesModSystem.api);
        public static Dictionary<string, string> VARIANTS = [];

        static SnowshoesFurItem()
        {
            if (VARIANTS.Count == 0)
            {
                VARIANTS.Add("fur", "game:clothes-foot-knee-high-fur-boots");
                VARIANTS.Add("nadiyanbrown", "game:clothes-nadiya-foot-winter1");
                VARIANTS.Add("nadiyanblue", "game:clothes-nadiya-foot-winter2");
                VARIANTS.Add("reindeer", "game:clothes-foot-fur-lined-reindeer-herder-shoes");
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            snowshoes.HandleHeldItemInfo(inSlot, dsc);
        }

        // Merge attributes from fur boots and snowshoes into fur snowshoes item because "mergeAttributesFrom" in JSON doesn't work...
        public override void OnCreatedByCrafting(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe byRecipe) {
            if (outputSlot is DummySlot) return;

            if (byRecipe.Name.Path.Contains("repair"))
            {
                if (!snowshoes.HandleOnCreatedByCraftingRepair(inSlots, ref outputSlot, byRecipe)) return;
            }

            if (Regex.IsMatch(byRecipe.Name, @"snowshoes:assemble-(un)?treated.*")) {
                ItemSlot snowshoesSlot = inSlots.First((sl) => {
                    return sl.Itemstack != null && sl.Itemstack.Item != null && sl.Itemstack.Item.FirstCodePart(3).Equals("plain");
                });

                ItemSlot bootsSlot = inSlots.First((sl) => {
                    return sl.Itemstack != null && sl.Itemstack.Item != null
                    && VARIANTS.Values.Contains(sl.Itemstack.Item.Code.ToString());
                });

                ITreeAttribute attr = outputSlot.Itemstack.Attributes;
                int maxDur = snowshoesSlot.Itemstack.Collectible.GetMaxDurability(snowshoesSlot.Itemstack);

                attr.SetInt("durability", snowshoesSlot.Itemstack.Attributes.GetInt("durability", maxDur));
                attr.SetInt("repairCount", snowshoesSlot.Itemstack.Attributes.GetInt("repairCount", 0));
                attr.SetFloat("condition", bootsSlot.Itemstack.Attributes.GetFloat("condition", 1f));

                if (api.Side == EnumAppSide.Server) {
                    outputSlot.MarkDirty();
                }
            }
        }

        // Preserve fur boots condition when disassembling
        public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity) {
            if (api.Side == EnumAppSide.Client) {
                base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
                return;
            }

            IServerPlayer pl = (IServerPlayer)byPlayer;

            if (stackInSlot.Itemstack == null || stackInSlot.Itemstack.Item == null) return;

            if (Regex.IsMatch(gridRecipe.Name, @"snowshoes:disassemble-(un)?treated.*")) {
                ItemSlot toUncraft = allInputSlots.First((sl) => sl.Itemstack != null);
                string furCode = VARIANTS.Get(toUncraft.Itemstack.Item.FirstCodePart(3));
                ItemStack furBoots = new(pl.Entity.World.SearchItems(furCode)[0]);

                furBoots.Attributes.SetFloat("condition", toUncraft.Itemstack.Attributes.GetFloat("condition", 1));
                pl.Entity.TryGiveItemStack(furBoots);
            }

            base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
        }

        // Add new attributes to existing snowshoes if not exist
        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            snowshoes.HandleAttributeAssignOnSlotChange(slot, extractedStack);
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }

        public override bool ConsumeCraftingIngredients(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe recipe) {
            return snowshoes.HandleConsumeCraftingIngredients(inSlots, outputSlot, recipe);
        }
    }
}
