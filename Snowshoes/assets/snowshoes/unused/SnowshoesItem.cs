using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Snowshoes.assets.snowshoes.unused
{
    internal class SnowshoesItem : ItemWearable
    {
        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            // Called when shoes are placed in feet slot
            if(slot.GetType() == typeof(ItemSlotCharacter) && extractedStack == null)
            {
                if (world.GetType() == typeof(ClientMain))
                {
                    ClientMain cm = (ClientMain)world;
                    EntityPlayer en = cm.Player?.Entity;

                    if (en != null)
                    {
                        en.AnimManager.OnStartAnimation += (ref AnimationMetaData meta, ref EnumHandling handling) => {
                            handling = EnumHandling.PreventDefault;
                            return false;
                        };
                    }
                }
            }
            
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }
    }
}
