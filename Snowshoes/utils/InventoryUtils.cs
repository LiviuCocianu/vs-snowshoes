using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Snowshoes.utils
{
    internal class InventoryUtils
    {
        public static Tuple<bool, ItemStack> AreSnowshoesEquipped(IPlayer pl)
        {
            InventoryCharacter inv = (InventoryCharacter)pl.InventoryManager.GetInventory(pl.InventoryManager.GetInventoryName("character"));
            ItemSlot slotBoots = inv.ElementAt(4);
            bool equipped = !slotBoots.Empty && slotBoots.Itemstack.Item.CodeWithoutParts(1).Equals("snowshoes");

            return new Tuple<bool, ItemStack>(equipped, slotBoots.Itemstack);
        }

        public static float GetShoesCondition(IPlayer pl)
        {
            Tuple<bool, ItemStack> res = AreSnowshoesEquipped(pl);

            if(!res.Item1) return -1;

            float result = res.Item2.Attributes.GetFloat("condition", -1);

            return result;
        }
    }
}
