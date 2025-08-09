using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Snowshoes.src.itemtypes
{
    internal class SnowshoesItem : ItemWearable
    {
        // Patch old 1.x snowshoes into the new 2.x variant on item drop
        public override void OnGroundIdle(EntityItem entityItem)
        {
            string code = entityItem.Itemstack.Item.Code;

            if (!code.StartsWith("snowshoes:snowshoes-plain") && !code.StartsWith("snowshoes:snowshoes-fur")) return;

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if ((code.StartsWith("snowshoes:snowshoes-plain") && obj.Code.PathStartsWith("snowshoes-wooden-oak-plain-untreated"))
                    || (code.StartsWith("snowshoes:snowshoes-fur") && obj.Code.PathStartsWith("snowshoes-wooden-oak-fur-untreated")))
                {
                    entityItem.Itemstack = new(obj);
                    base.OnGroundIdle(entityItem);
                    return;
                }
            }

            base.OnGroundIdle(entityItem);
        }

        // Patch old 1.x snowshoes into the new 2.x variant on inventory movement
        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            if (extractedStack == null || extractedStack.Item == null)
            {
                base.OnModifiedInInventorySlot(world, slot, extractedStack);
                return;
            }

            string codeSecond = extractedStack.Item.FirstCodePart(1);

            if (codeSecond.Equals("plain") || codeSecond.Equals("fur"))
            {
                Item item = world.Items.FirstOrDefault(x => x.Code == $"snowshoes:snowshoes-wooden-oak-{codeSecond}-untreated");
                slot.Itemstack = new ItemStack(item);
                slot.MarkDirty();

                base.OnModifiedInInventorySlot(world, slot, slot.Itemstack);
                return;
            }

            base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }
    }
}
