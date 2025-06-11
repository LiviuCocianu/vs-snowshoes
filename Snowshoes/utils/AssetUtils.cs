using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Snowshoes.utils
{
    internal class AssetUtils
    {
        public static int GetSnowloggedBlockId(Block bl, int layer, string domain)
        {
            string firstCode = bl.FirstCodePart();

            string variantType = firstCode == "snowlayer" ? "height"
                : firstCode == "tallgrass" ? "cover"
                : "";

            string variantValue = firstCode == "snowlayer" ? layer.ToString()
                : firstCode == "tallgrass" ? ("snow" + (layer == 1 ? "" : layer))
                : "";

            if (variantType == "") return -1;

            AssetLocation gameSnowLayer = bl.CodeWithVariant(variantType, variantValue);
            gameSnowLayer.Domain = domain;

            return SnowshoesModSystem.api.World.BlockAccessor.GetBlock(gameSnowLayer).Id;
        }

        public static bool IsSnowloggable(Block bl)
        {
            string firstCode = bl.FirstCodePart();
            return firstCode == "snowlayer" || firstCode == "tallgrass";
        }

        public static int GetSnowloggedLayer(Block bl)
        {
            char lastChar = 'a';
            string firstCode = bl.FirstCodePart();

            if (firstCode == "tallgrass") lastChar = bl.FirstCodePart(2)[^1];

            return firstCode == "snowlayer" ? Int32.Parse(bl.FirstCodePart(1))
                : firstCode == "tallgrass" ? Int32.Parse(lastChar == 'w' ? "1" : lastChar == 'e' ? "-1" : lastChar + "")
                : -1;
        }
    }
}
