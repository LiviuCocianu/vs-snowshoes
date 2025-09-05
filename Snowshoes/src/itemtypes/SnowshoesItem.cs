using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Snowshoes.src.itemtypes
{
    // Functionality that is shared by both types of snowshoes
    internal class SnowshoesItem : CollectibleObject
    {
        private new readonly ICoreAPI api;

        public SnowshoesItem(ICoreAPI api) {
            this.api = api;
        }

        public override int Id => throw new NotImplementedException();

        public override EnumItemClass ItemClass => throw new NotImplementedException();

        public bool HandleOnCreatedByCraftingRepair(ItemSlot[] inputs, ref ItemSlot outputSlot, GridRecipe byRecipe)
        {

            int curDur = outputSlot.Itemstack.Collectible.GetRemainingDurability(outputSlot.Itemstack);
            int maxDur = outputSlot.Itemstack.Collectible.GetMaxDurability(outputSlot.Itemstack);

            if (curDur == maxDur) {
                outputSlot.Itemstack = null;
                return false;
            }

            SnowshoeRepairMaterial mat = byRecipe.Name.ToString().Contains("crude")
                ? SnowshoeRepairMaterial.ROPE
                : byRecipe.Name.ToString().Contains("metal")
                ? SnowshoeRepairMaterial.LEATHER : SnowshoeRepairMaterial.TWINE;

            CalculateRepairValue(inputs, outputSlot, mat, out float repairValue, out int matCostPerMatType, out int availableRepairMatCount);

            ItemSlot shoes = inputs.FirstOrDefault(slot => slot.Itemstack?.Collectible is ItemWearableAttachment);
            int repairCount = shoes.Itemstack.Attributes.GetInt("repairCount", -1);

            if (repairCount != -1 && !SnowshoesModSystem.GetInstance().config.unlimitedRepairs)
            {
                int maxRepairCount = shoes.Itemstack.Item.FirstCodePart(1) switch
                {
                    "wooden" => SnowshoesModSystem.GetInstance().config.maxRepairCountWood,
                    "metal" => SnowshoesModSystem.GetInstance().config.maxRepairCountMetal,
                    _ => SnowshoesModSystem.GetInstance().config.maxRepairCountWood
                };

                if (repairCount == maxRepairCount)
                {
                    outputSlot.Itemstack = null;
                    return false;
                }

                // If submitted materials for repairing don't exceed the max repair count, increment count by that amount
                if (repairCount + availableRepairMatCount < maxRepairCount)
                {
                    outputSlot.Itemstack.Attributes.SetInt("repairCount", repairCount + availableRepairMatCount);
                } 
                // ... otherwise, top up repair count to max
                else
                {
                    outputSlot.Itemstack.Attributes.SetInt("repairCount", maxRepairCount);
                }
            }

            outputSlot.Itemstack.Attributes.SetInt("durability", Math.Min(maxDur, (int)(curDur + maxDur * repairValue)));

            return true;
        }

        public void HandleHeldItemInfo(ItemSlot inSlot, StringBuilder dsc)
        {
            if (SnowshoesModSystem.GetInstance().config.unlimitedRepairs) return;

            int repairCount = inSlot.Itemstack.Attributes.GetInt("repairCount", -1);
            int maxRepairCount = inSlot.Itemstack.Item.FirstCodePart(1) switch
            {
                "wooden" => SnowshoesModSystem.GetInstance().config.maxRepairCountWood,
                "metal" => SnowshoesModSystem.GetInstance().config.maxRepairCountMetal,
                _ => SnowshoesModSystem.GetInstance().config.maxRepairCountWood
            };

            if (repairCount != -1)
            {
                string desc = dsc.ToString();
                string repairMaxPlaceholder = "";

                if (repairCount == maxRepairCount)
                    repairMaxPlaceholder = Lang.Get("snowshoes:repairdesc-max");

                string repairDesc = Lang.Get("snowshoes:repairdesc", repairCount, maxRepairCount, repairMaxPlaceholder) + "\n\n";

                dsc.Clear();
                dsc.Append(desc[..desc.IndexOf("Mod")] + repairDesc + desc[desc.IndexOf("Mod")..]);
            }
        }

        // Ensure "repairCount" attribute exists when repairs are limited
        public bool HandleAttributeAssignOnSlotChange(ItemSlot slot, ItemStack extractedStack)
        {
            if (extractedStack == null) return false;
            if (slot is ItemSlotUniversal || slot is ItemSlotCreative) return false;
            if (SnowshoesModSystem.GetInstance().config.unlimitedRepairs) return false;

            if (extractedStack.Attributes.GetInt("repairCount", -1) == -1)
            {
                slot.Itemstack.Attributes.SetInt("repairCount", 0);
            }

            return true;
        }

        public bool HandleConsumeCraftingIngredients(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe recipe)
        {
            // Consume as much materials in the input grid as needed
            if (recipe.Name.Path.Contains("repair"))
            {
                SnowshoeRepairMaterial mat = recipe.Name.ToString().Contains("crude")
                    ? SnowshoeRepairMaterial.ROPE
                    : recipe.Name.ToString().Contains("metal")
                    ? SnowshoeRepairMaterial.LEATHER : SnowshoeRepairMaterial.TWINE;

                CalculateRepairValue(inSlots, outputSlot, mat, out float repairValue, out int matCostPerMatType, out _);

                foreach (var islot in inSlots)
                {
                    if (islot.Empty) continue;

                    if (islot.Itemstack.Collectible is ItemWearableAttachment) {
                        islot.Itemstack = null; 
                        continue;
                    }

                    islot.TakeOut(matCostPerMatType);
                }

                return true;
            }

            return false;
        }

        public void CalculateRepairValue(ItemSlot[] inSlots, ItemSlot outputSlot, SnowshoeRepairMaterial mat, out float repairValue, out int matCostPerMatType, out int availableRepairMatCount)
        {
            var snowshoesSlot = inSlots.FirstOrDefault(slot => slot.Itemstack?.Collectible is ItemWearableAttachment);
            int curDur = outputSlot.Itemstack.Collectible.GetRemainingDurability(snowshoesSlot.Itemstack);
            int maxDur = outputSlot.Itemstack.Collectible.GetMaxDurability(outputSlot.Itemstack);

            int repairCount = snowshoesSlot.Itemstack.Attributes.GetInt("repairCount", 0);
            int maxRepairCount = snowshoesSlot.Itemstack.Item.FirstCodePart(1) switch
            {
                "wooden" => SnowshoesModSystem.GetInstance().config.maxRepairCountWood,
                "metal" => SnowshoesModSystem.GetInstance().config.maxRepairCountMetal,
                _ => SnowshoesModSystem.GetInstance().config.maxRepairCountWood
            };

            // How much 1x mat repairs in %
            float repairValuePerItem = mat switch
            {
                SnowshoeRepairMaterial.ROPE => SnowshoesModSystem.GetInstance().config.ropeRepairPercentage,
                SnowshoeRepairMaterial.TWINE => SnowshoesModSystem.GetInstance().config.flaxRepairPercentage,
                SnowshoeRepairMaterial.LEATHER => SnowshoesModSystem.GetInstance().config.leatherRepairPercentage,
                _ => SnowshoesModSystem.GetInstance().config.ropeRepairPercentage
            };

            // How much the mat repairs in durability
            float repairDurabilityPerItem = repairValuePerItem * maxDur;
            // Divide missing durability by repair per item = items needed for full repair 
            int fullRepairMatCount = (int)Math.Max(1, Math.Round((maxDur - curDur) / repairDurabilityPerItem));
            // Limit repair value to smallest stack size of all repair mats
            var minMatStackSize = GetInputRepairCount(inSlots);
            // Divide the cost amongst all mats
            var matTypeCount = GetRepairMatTypeCount(inSlots);

            int RepairMatOverflow(int cost)
            {
                return Math.Max(0, (repairCount + cost) - maxRepairCount);
            }

            /*
             * If the available materials exceed the current repair count, reduce the required amount to
             * consume just enough materials to reach the maximum.
             */
            availableRepairMatCount = Math.Min(fullRepairMatCount, minMatStackSize * matTypeCount);

            if(!SnowshoesModSystem.GetInstance().config.unlimitedRepairs)
                availableRepairMatCount -= RepairMatOverflow(availableRepairMatCount);

            matCostPerMatType = Math.Min(fullRepairMatCount, minMatStackSize);

            if (!SnowshoesModSystem.GetInstance().config.unlimitedRepairs)
                matCostPerMatType -= RepairMatOverflow(matCostPerMatType);

            // Repairing costs half as many materials as newly creating it
            repairValue = Math.Min(availableRepairMatCount * repairValuePerItem, 1.0f);
        }

        private int GetRepairMatTypeCount(ItemSlot[] slots)
        {
            List<ItemStack> stackTypes = new List<ItemStack>();
            foreach (var slot in slots)
            {
                if (slot.Empty) continue;
                bool found = false;
                if (slot.Itemstack.Collectible is ItemWearableAttachment) continue;

                foreach (var stack in stackTypes)
                {
                    if (slot.Itemstack.Satisfies(stack))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    stackTypes.Add(slot.Itemstack);
                }
            }

            return stackTypes.Count;
        }

        public int GetInputRepairCount(ItemSlot[] inputSlots)
        {
            OrderedDictionary<int, int> matcounts = new();
            foreach (var slot in inputSlots)
            {
                if (slot.Empty || slot.Itemstack.Collectible is ItemWearableAttachment) continue;
                var hash = slot.Itemstack.GetHashCode();
                matcounts.TryGetValue(hash, out int cnt);
                matcounts[hash] = cnt + slot.StackSize;
            }
            return matcounts.Values.Min();
        }
    }
}
