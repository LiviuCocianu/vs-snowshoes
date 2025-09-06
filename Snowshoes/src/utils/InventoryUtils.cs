using Snowshoes.src.itemtypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Snowshoes.src.utils
{
    internal class InventoryUtils
    {
        public static ItemSlot GetFootwareSlot(IPlayer pl) {
            InventoryCharacter inv = (InventoryCharacter)pl.InventoryManager.GetInventory(pl.InventoryManager.GetInventoryName("character"));

            if(inv == null) return null;

            ItemSlot slotBoots = inv.ElementAt(4);

            return slotBoots;
        }

        public static ItemStack GetSnowshoes(IPlayer pl)
        {
            ItemSlot slotBoots = GetFootwareSlot(pl);

            if (slotBoots == null) return null;

            return slotBoots.Itemstack;
        }

        public static Tuple<bool, ItemStack> AreSnowshoesEquipped(IPlayer pl)
        {
            ItemSlot slotBoots = GetFootwareSlot(pl);

            if(slotBoots == null || slotBoots.Itemstack == null || slotBoots.Itemstack.Item == null)
                return new Tuple<bool, ItemStack>(false, null);

            Item shoesItem = slotBoots.Itemstack.Item;

            bool codeIsSnowshoes1 = Regex.IsMatch(shoesItem.Code, @"snowshoes-.*-plain-.*");
            bool codeIsSnowshoes2 = shoesItem.FirstCodePart(3) == null 
                ? AreOldSnowshoesEquipped(pl)
                : SnowshoesFurItem.VARIANTS.ContainsKey(shoesItem.FirstCodePart(3));

            return new Tuple<bool, ItemStack>(!slotBoots.Empty && (codeIsSnowshoes1 || codeIsSnowshoes2), slotBoots.Itemstack);
        }

        public static bool AreOldSnowshoesEquipped(IPlayer pl)
        {
            ItemSlot slotBoots = GetFootwareSlot(pl);

            if (slotBoots == null || slotBoots.Itemstack == null || slotBoots.Itemstack.Item == null)
                return false;

            Item shoesItem = slotBoots.Itemstack.Item;
            bool codeIsSnowshoes = Regex.IsMatch(shoesItem.Code, @"snowshoes-(plain|fur)");

            return !slotBoots.Empty && codeIsSnowshoes;
        }

        public static void MarkSnowshoesSlotDirty(IServerPlayer pl)
        {
            IPlayerInventoryManager im = pl.InventoryManager;
            ((InventoryCharacter) im.GetInventory(im.GetInventoryName("character")))[4].MarkDirty();
        }

        public static float GetShoesCondition(IPlayer pl)
        {
            Tuple<bool, ItemStack> res = AreSnowshoesEquipped(pl);

            if(!res.Item1) return -1;

            float result = res.Item2.Attributes.GetFloat("condition", -1);

            return result;
        }

        public static void SetFootAndMarkDirty(IServerPlayer pl, ItemStack toSet)
        {
            ItemSlot feet = GetFootwareSlot(pl);

            if (feet == null) return;

            feet.Itemstack = toSet;
            feet.MarkDirty();
        }
    }
}
