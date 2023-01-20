using beehivekiln.block;
using beehivekiln.blockentity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace beehivekiln
{
    class BeehiveKilnCore : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Logger.Notification("Loaded Beehive Kiln!");

            api.RegisterBlockClass("BlockFirebrickKilnFlue", typeof(BlockFirebrickKilnFlue));
            api.RegisterBlockEntityClass("BlockEntityBeehiveKiln", typeof(BlockEntityBeehiveKiln));
        }
    }
}
