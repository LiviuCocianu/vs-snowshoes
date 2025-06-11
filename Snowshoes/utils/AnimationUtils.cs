using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Snowshoes.utils {
    internal class AnimationUtils {
#nullable enable
        public static Animation? GetAnimation(ICoreServerAPI sapi, Dictionary<string, int> indexCache, string animCode) {
#nullable disable
            sapi.Logger.Notification(GetAllPlayerAnimations(sapi).Last().Code);
            return GetAllPlayerAnimations(sapi)[indexCache[animCode]];
        }

        public static List<Animation> GetAllPlayerAnimations(ICoreServerAPI sapi) {
            JObject seraphAsset = sapi.Assets.Get(new AssetLocation("snowshoes:shapes/entity/humanoid/seraph-faceless.json")).ToObject<JObject>();
            IEnumerable<JToken> janims = seraphAsset["animations"].AsEnumerable();
            List<Animation> anims = janims.Select((token) => token.ToObject<Animation>()).ToList();

            return anims;
        }
    }
}
