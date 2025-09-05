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
    internal partial class SnowshoesPlainItem : ItemWearableAttachment
    {
        private static readonly SnowshoesItem snowshoes = new(SnowshoesModSystem.api);

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            snowshoes.HandleHeldItemInfo(inSlot, dsc);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe) {
            if (outputSlot is DummySlot) return;

            if (byRecipe.Name.Path.Contains("repair")) {
                snowshoes.HandleOnCreatedByCraftingRepair(allInputslots, ref outputSlot, byRecipe);
            }
        }

        // Add new attributes to existing snowshoes if not exist
        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            if (!snowshoes.HandleAttributeAssignOnSlotChange(slot, extractedStack)) return;
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }

        public override bool ConsumeCraftingIngredients(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe recipe) {
            return snowshoes.HandleConsumeCraftingIngredients(inSlots, outputSlot, recipe);
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
    }
}
