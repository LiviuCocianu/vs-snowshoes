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
    internal partial class SnowshoesPlainItem : ItemWearableAttachment
    {
        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
            if (outputSlot is DummySlot) return;

            if (byRecipe.Name.Path.Contains("repair")) {
                SnowshoeRepairMaterial mat = byRecipe.Name.ToString().Contains("crude")
                    ? SnowshoeRepairMaterial.ROPE
                    : byRecipe.Name.ToString().Contains("metal")
                    ? SnowshoeRepairMaterial.LEATHER : SnowshoeRepairMaterial.TWINE;

                CalculateRepairValue(allInputslots, outputSlot, mat, out float repairValue, out int matCostPerMatType);

                int curDur = outputSlot.Itemstack.Collectible.GetRemainingDurability(outputSlot.Itemstack);
                int maxDur = GetMaxDurability(outputSlot.Itemstack);

                outputSlot.Itemstack.Attributes.SetInt("durability", Math.Min(maxDur, (int)(curDur + maxDur * repairValue)));
            }
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

        // Allow waxed snowshoes to retain attributes after they finish treating
        public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props) {
            if (slot.Itemstack == null || slot.Itemstack.Item == null) return base.OnTransitionNow(slot, props);
            if (props.TransitionedStack == null || props.TransitionedStack.ResolvedItemstack == null) return base.OnTransitionNow(slot, props);

            ItemStack resolved = props.TransitionedStack.ResolvedItemstack;

            ITreeAttribute attr = slot.Itemstack.Attributes;
            resolved.Attributes.MergeTree(attr);

            return resolved;
        }

        public void CalculateRepairValue(ItemSlot[] inSlots, ItemSlot outputSlot, SnowshoeRepairMaterial mat, out float repairValue, out int matCostPerMatType) {
            var armorSlot = inSlots.FirstOrDefault(slot => slot.Itemstack?.Collectible is ItemWearableAttachment);
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
                if (slot.Itemstack.Collectible is ItemWearableAttachment) continue;

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

        public int GetInputRepairCount(ItemSlot[] inputSlots) {
            OrderedDictionary<int, int> matcounts = new();
            foreach (var slot in inputSlots) {
                if (slot.Empty || slot.Itemstack.Collectible is ItemWearableAttachment) continue;
                var hash = slot.Itemstack.GetHashCode();
                matcounts.TryGetValue(hash, out int cnt);
                matcounts[hash] = cnt + slot.StackSize;
            }
            return matcounts.Values.Min();
        }
    }
}
