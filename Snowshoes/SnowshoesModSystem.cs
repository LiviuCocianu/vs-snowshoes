using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snowshoes.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Snowshoes
{
    public class SnowshoesModSystem : ModSystem
    {
        private static ICoreAPI api;
        public ICoreClientAPI capi;
        public ICoreServerAPI sapi;
        private ILogger logger;

        private List<string> movementAnimations = new() { "walk", "walk-fp", "sprint", "sprint-fp" };
        private List<string> animations = new() { "idle1", "idle1-fp" };

        private Dictionary<string, int> cachedAnimationIndexes = new();

        public static SnowshoesModSystem GetInstance() => api.ModLoader.GetModSystem<SnowshoesModSystem>();

        public ILogger Logger {
            get {  return logger; }
        }

        public override void Start(ICoreAPI api)
        {
            //api.RegisterItemClass(Mod.Info.ModID + ".snowshoes", typeof(SnowshoesItem));
            logger = Mod.Logger;
            SnowshoesModSystem.api = api;

            api.Event.OnEntityLoaded += (en) => {
                if(en.GetType() == typeof(IPlayer)) {
                    en.AnimManager.StopAnimation("walk");

                    AnimationMetaData meta = en.AnimManager.Animator.Animations.ToList().Find(ran => ran.Animation.Code == "walk-snowshoes").meta.Clone();
                    meta.Animation = "walk";
                    meta.Code = "walk";

                    en.AnimManager.StartAnimation(meta);

                    // Handle idle and idle-like animations
                    //en.AnimManager.OnStartAnimation += (ref AnimationMetaData meta, ref EnumHandling handling) => {
                    //    logger.Notification("started 3: " + meta.Code);
                    //    return true;
                    //};
                }
            };
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            //api.Event.PlayerJoin += (IServerPlayer byPlayer) => {
            //    // Handle all movement animations
            //    byPlayer.InWorldAction += (EnumEntityAction action, bool on, ref EnumHandling handled) => {
            //        if (!on) return;

            //        if (action == EnumEntityAction.Forward
            //            || action == EnumEntityAction.Backward
            //            || action == EnumEntityAction.Left
            //            || action == EnumEntityAction.Right
            //        ) {
            //            if (byPlayer.Entity.AnimManager.IsAnimationActive("walk")) logger.Notification("works");
            //            if (byPlayer.Entity.AnimManager.IsAnimationActive("walk-snowshoes")) logger.Notification("works ss");

            //            // Go through all movement animations and replace them with mine
            //            foreach (string anim in movementAnimations) {
            //                // No reason to play my animation if it's already playing..
            //                if (byPlayer.Entity.AnimManager.GetAnimationState(anim + "-snowshoes") != null) continue;

            //                if (byPlayer.Entity.AnimManager.IsAnimationActive(anim)) {
            //                    InventoryCharacter inv = (InventoryCharacter)byPlayer.InventoryManager.GetInventory(byPlayer.InventoryManager.GetInventoryName("character"));
            //                    ItemSlot slotBoots = inv.ElementAt(4);

            //                    // If snowshoes are equipped
            //                    if (!slotBoots.Empty && slotBoots.Itemstack.Item.CodeEndWithoutParts(1).Equals("snowshoes")) {
            //                        // Load the animations from my custom seraph shape file
            //                        byPlayer.Entity.AnimManager.StopAnimation(anim);
            //                        byPlayer.Entity.AnimManager.StartAnimation(anim + "-snowshoes");
            //                    }
            //                }
            //            }
            //        }
            //    };
            //};
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            api.Event.PlayerJoin += (IClientPlayer byPlayer) => {
                // Handle idle and idle-like animations
                byPlayer.WorldData.EntityPlayer.AnimManager.OnStartAnimation += (ref AnimationMetaData meta, ref EnumHandling handling) => {
                    logger.Notification("started: " + meta.Code);
                    return true;
                };
            };
        }
    }
}
