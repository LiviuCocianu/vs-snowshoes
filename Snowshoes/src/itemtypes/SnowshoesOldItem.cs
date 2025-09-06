using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Snowshoes.src.itemtypes {
    internal class SnowshoesOldItem : ItemWearableAttachment {
        // Patch old 1.x snowshoes into the new 2.x variant on item drop
        public override void OnGroundIdle(EntityItem entityItem) {
            bool success = PatchSnowshoesToVersion2(entityItem.Itemstack, (placeholder) => {
                entityItem.Itemstack = new(placeholder);
                base.OnGroundIdle(entityItem);
            });

            if (!success) base.OnGroundIdle(entityItem);
        }

        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null) {
            if (extractedStack == null) return;
            if (slot is ItemSlotUniversal || slot is ItemSlotCreative) return;

            bool success = PatchSnowshoesToVersion2(extractedStack, (placeholder) => {
                slot.Itemstack = new(placeholder);

                base.OnModifiedInInventorySlot(world, slot, new(placeholder));
            });

            if(!success) base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }

        public static bool PatchSnowshoesToVersion2(ItemStack toCheck, Action<CollectibleObject> actionWithPlaceholder) {
            string checkStyle = toCheck.Item.FirstCodePart(1);
            Item[] replacement = SnowshoesModSystem.api.World.SearchItems($"snowshoes:snowshoes-wooden-oak-{checkStyle}-untreated");

            if(replacement.Length > 0) {
                actionWithPlaceholder.Invoke(replacement[0]);
                return true;
            }

            return false;
        }
    }
}
