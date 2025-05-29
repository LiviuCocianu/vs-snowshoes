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

namespace Snowshoes.classes
{
    internal class SnowshoesItem : ItemWearable
    {
        private static void AlterAnimation(EntityPlayer en, string animationName)
        {
            if (animationName == "idle")
            {
                if (en.AnimManager.GetAnimationState(animationName + "1") == null
                    && en.AnimManager.GetAnimationState(animationName + "1-fp") == null) return;
            }

            if (en.AnimManager.GetAnimationState(animationName) == null
                && en.AnimManager.GetAnimationState(animationName + "-fp") == null) return;

            Animation customAnim = en.AnimManager.GetAnimationState(animationName).Animation;

            List<string> keys = en.AnimManager.ActiveAnimationsByAnimCode.Keys.ToList();
            en.World.Logger.Notification(String.Join(", ", keys.ToArray(), 0, keys.Count - 1) + ", and " + keys.LastOrDefault());
            
            int i = 0;
            foreach (AnimationKeyFrame frame in customAnim.KeyFrames)
            {
                // Bring model slightly up
                if (frame.Elements.ContainsKey("LowerTorso"))
                {
                    frame.Elements.Get("LowerTorso").OriginY += 1.0;
                    frame.Elements.Get("LowerTorso").OffsetY += 1.0;
                }

                // TODO add these elements for idle animation!

                // Spread legs to accomodate shoes
                if (frame.Elements.ContainsKey("UpperFootL") && frame.Elements.Get("UpperFootL").RotationX != null) 
                    frame.Elements.Get("UpperFootL").RotationX = -8.0;
                if (frame.Elements.ContainsKey("LowerFootL") && frame.Elements.Get("LowerFootL").RotationX != null) 
                    frame.Elements.Get("LowerFootL").RotationX = 6.0;
                if (frame.Elements.ContainsKey("UpperFootR") && frame.Elements.Get("UpperFootR").RotationX != null) 
                    frame.Elements.Get("UpperFootR").RotationX = 8.0;
                if (frame.Elements.ContainsKey("LowerFootR") && frame.Elements.Get("LowerFootR").RotationX != null) 
                    frame.Elements.Get("LowerFootR").RotationX = -6.0;

                i++;
            }
        }

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
                        AlterAnimation(en, "walk");
                        AlterAnimation(en, "idle");
                        AlterAnimation(en, "sprint");
                    }
                }
            }
            
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
        }
    }
}
