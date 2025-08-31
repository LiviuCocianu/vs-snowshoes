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
        // Merge attributes from fur boots and snowshoes into fur snowshoes item because "mergeAttributesFrom" in JSON doesn't work...
        public override void OnCreatedByCrafting(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe byRecipe) {
            if (outputSlot is DummySlot) return;

            if (byRecipe.Name.Path.Contains("repair")) {
                SnowshoeRepairMaterial mat = byRecipe.Name.ToString().Contains("crude")
                    ? SnowshoeRepairMaterial.ROPE
                    : byRecipe.Name.ToString().Contains("metal")
                    ? SnowshoeRepairMaterial.LEATHER : SnowshoeRepairMaterial.TWINE;

                CalculateRepairValue(inSlots, outputSlot, mat, out float repairValue, out int matCostPerMatType);

                int curDur = outputSlot.Itemstack.Collectible.GetRemainingDurability(outputSlot.Itemstack);
                int maxDur = GetMaxDurability(outputSlot.Itemstack);

                outputSlot.Itemstack.Attributes.SetInt("durability", Math.Min(maxDur, (int)(curDur + maxDur * repairValue)));
            }

            if (Regex.IsMatch(byRecipe.Name, @"snowshoes:assemble-(un)?treated")) {
                ItemSlot snowshoesSlot = inSlots.First((sl) => {
                    return sl.Itemstack != null && sl.Itemstack.Item != null && sl.Itemstack.Item.FirstCodePart(3).Equals("plain");
                });

                ItemSlot bootsSlot = inSlots.First((sl) => {
                    return sl.Itemstack != null && sl.Itemstack.Item != null
                    && sl.Itemstack.Item.Code.ToString().Contains("clothes-foot-knee-high-fur-boots");
                });

                ITreeAttribute attr = outputSlot.Itemstack.Attributes;
                int maxDur = snowshoesSlot.Itemstack.Collectible.GetMaxDurability(snowshoesSlot.Itemstack);

                attr.SetInt("durability", snowshoesSlot.Itemstack.Attributes.GetInt("durability", maxDur));
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

            if (Regex.IsMatch(gridRecipe.Name, @"snowshoes:disassemble-(un)?treated")) {
                ItemSlot toUncraft = allInputSlots.First((sl) => sl.Itemstack != null);
                ItemStack furBoots = new(pl.Entity.World.SearchItems("game:clothes-foot-knee-high-fur-boots")[0]);

                furBoots.Attributes.SetFloat("condition", toUncraft.Itemstack.Attributes.GetFloat("condition", 1));
                pl.Entity.TryGiveItemStack(furBoots);
            }

            base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
        }

        public override bool ConsumeCraftingIngredients(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe recipe) {
            // Consume as much materials in the input grid as needed
            if (recipe.Name.Path.Contains("repair")) {
                SnowshoeRepairMaterial mat = recipe.Name.ToString().Contains("crude")
                    ? SnowshoeRepairMaterial.ROPE
                    : recipe.Name.ToString().Contains("metal")
                    ? SnowshoeRepairMaterial.LEATHER : SnowshoeRepairMaterial.TWINE;

                CalculateRepairValue(inSlots, outputSlot, mat, out float repairValue, out int matCostPerMatType);

                foreach (var islot in inSlots) {
                    if (islot.Empty) continue;

                    if (islot.Itemstack.Collectible == this) { islot.Itemstack = null; continue; }

                    islot.TakeOut(matCostPerMatType);
                }

                return true;
            }

            return false;
        }

        public void CalculateRepairValue(ItemSlot[] inSlots, ItemSlot outputSlot, SnowshoeRepairMaterial mat, out float repairValue, out int matCostPerMatType) {
            var armorSlot = inSlots.FirstOrDefault(slot => slot.Itemstack?.Collectible is ItemWearable);
            int curDur = outputSlot.Itemstack.Collectible.GetRemainingDurability(armorSlot.Itemstack);
            int maxDur = GetMaxDurability(outputSlot.Itemstack);

            // How much 1x mat repairs in %
            float repairValuePerItem = mat == SnowshoeRepairMaterial.ROPE
                ? SnowshoesModSystem.GetInstance().config.ropeRepairPercentage
                : mat == SnowshoeRepairMaterial.TWINE
                ? SnowshoesModSystem.GetInstance().config.flaxRepairPercentage
                : SnowshoesModSystem.GetInstance().config.leatherRepairPercentage;

            // How much the mat repairs in durability
            float repairDurabilityPerItem = repairValuePerItem * maxDur;
            // Divide missing durability by repair per item = items needed for full repair 
            int fullRepairMatCount = (int)Math.Max(1, Math.Round((maxDur - curDur) / repairDurabilityPerItem));
            // Limit repair value to smallest stack size of all repair mats
            var minMatStackSize = GetInputRepairCount(inSlots);
            // Divide the cost amongst all mats
            var matTypeCount = GetRepairMatTypeCount(inSlots);

            var availableRepairMatCount = Math.Min(fullRepairMatCount, minMatStackSize * matTypeCount);
            matCostPerMatType = Math.Min(fullRepairMatCount, minMatStackSize);

            // Repairing costs half as many materials as newly creating it
            repairValue = Math.Min(availableRepairMatCount * repairValuePerItem, 1.0f);
        }

        private int GetRepairMatTypeCount(ItemSlot[] slots) {
            List<ItemStack> stackTypes = new List<ItemStack>();
            foreach (var slot in slots) {
                if (slot.Empty) continue;
                bool found = false;
                if (slot.Itemstack.Collectible is ItemWearable) continue;

                foreach (var stack in stackTypes) {
                    if (slot.Itemstack.Satisfies(stack)) {
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    stackTypes.Add(slot.Itemstack);
                }
            }

            return stackTypes.Count;
        }
    }
}
