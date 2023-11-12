using beehivekiln.block;
using beehivekiln.blockentity;
using Vintagestory.API.Common;

namespace beehivekiln
{
    class BeehiveKilnCore : ModSystem
    {
        private IServerNetworkChannel serverChannel;

        pubkic override void StartPre(ICoreAPI api)
        {
            string cfgFileName = "BeehiveKiln.json";

            try 
            {
                BeehiveKilnConfig cfgFromDisk;
                if ((cfgFromDisk = api.LoadModConfig<BeehiveKilnConfig>(cfgFileName)) == null)
                {
                    api.StoreModConfig(BeehiveKilnConfig.Loaded, cfgFileName);
                }
                else
                {
                    BeehiveKilnConfig.Loaded = cfgFromDisk;
                }
            } 
            catch 
            {
                api.StoreModConfig(BeehiveKilnConfig.Loaded, cfgFileName);
            }

            base.StartPre(api);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.Logger.Notification("Loaded Beehive Kiln!");

            api.RegisterBlockClass("BlockFirebrickKilnFlue", typeof(BlockFirebrickKilnFlue));
            api.RegisterBlockEntityClass("BlockEntityBeehiveKiln", typeof(BlockEntityBeehiveKiln));
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            // Sync config settings with clients
            capi.Network.RegisterChannel("beehivekiln")
                .RegisterMessageType<SyncConfigClientPacket>()
                .SetMessageHandler<SyncConfigClientPacket>(p => {
                    this.Mod.Logger.Event("Received config settings from server");
                    BeehiveModConfig.Loaded.FiringTimeHours = p.FiringTimeHours;
                    BeehiveModConfig.Loaded.MinimumFiringTemperatureCelsius = p.MinimumFiringTemperatureCelsius;
                    BeehiveModConfig.Loaded.TemperatureGainPerHourCelsius = p.TemperatureGainPerHourCelsius;
                });
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            // Send connecting players config settings
            this.serverChannel.SendPacket(new SyncConfigClientPacket {
                FiringTimeHours = ModConfig.Loaded.FiringTimeHours;
                MinimumFiringTemperatureCelsius = ModConfig.Loaded.MinimumFiringTemperatureCelsius;
                TemperatureGainPerHourCelsius = ModConfig.Loaded.TemperatureGainPerHourCelsius;
            }, player);
        }
    }
}
