using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snowshoes.src.config {
    public class SnowshoesConfig {
        /*
         * Check radius for snow layers. A value of 1 means a 3x3 area check at the player's location.
         * 
         * You can increase this value if you notice players still sink in the snow when moving fast (e.g. sprinting)
         * 
         * Side: Server
         */
        public int checkRadius = 1;

        /*
         * How often should blocks be checked for snow layers around players that wear snowshoes (in millis)
         * 
         * Side: Server
         */
        public int radiusCheckFrequency = 50;

        /*
         * Amount of durability damage snowshoes will get while used
         * 
         * Side: Server
         */
        public int durabilityAmount = 1;

        /*
         * After how many seconds of movement should the snowshoes start losing 1 point of durability.
         * 
         * This value is influenced by timeReductionMultiplier.
         * By default, timeReductionMultiplier reduces this value to 4 seconds.
         * 
         * Side: Server
         */
        public int secondsBeforeDepletion = 5;

        /*
         * Percentage used to reduce secondsBeforeDepletion when players walk on non-snow layer blocks.
         * 
         * By default, when players don't walk on snow layers with snowshoes, the seconds before depletion
         * get reduced by 20%, effectively making durability deplete faster.
         * 
         * The calculated seconds with timeReductionMultiplier cannot go below 1, and will also be rounded!
         * 
         * If you don't want this penalty, set this value to 1.
         * 
         * Side: Server
         */
        public float timeReductionMultiplier = 0.8f;

        /*
         * What percentage of max durability will be added to the current durability when repairing snowshoes with flax twine in the crafting grid.
         * 
         * For example, treated snowshoes have 450 max durability, so by default, 135 durability will be added per flax twine.
         */
        public float flaxRepairPercentage = 0.3f;

        /*
         * What percentage of max durability will be added to the current durability when repairing snowshoes with rope in the crafting grid.
         * Used by crude snowshoes only.
         */
        public float ropeRepairPercentage = 0.3f;

        /*
         * What percentage of max durability will be added to the current durability when repairing snowshoes with leather in the crafting grid.
         * Used by metal snowshoes only.
         */
        public float leatherRepairPercentage = 0.3f;

        /*
         * Let players repair snowshoes as much as they want or limit how many times snowshoes
         * can be repaired, up to a max repair count value.
         */
        public bool unlimitedRepairs = true;

        /*
         * How many times can snowshoes be repaired before they become unrepairable.
         * 
         * Counter increases relative to how many materials have been used in total to repair
         * the item, not relative to each repair in the grid!
         */
        public int maxRepairCountWood = 10;
        public int maxRepairCountMetal = 10;
    }
}
