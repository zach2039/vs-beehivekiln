using beehivekiln.block;
using beehivekiln.blockentity;
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
