using beehivekiln;
using beehivekiln.block;
using beehivekiln.blockentity;
using beehivekiln.network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace beehivekiln
{
    class BeehiveKilnCore : ModSystem
    {
        private IServerNetworkChannel serverChannel;
        private ICoreAPI api;

        public override void StartPre(ICoreAPI api)
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
            this.api = api;
            base.Start(api);

            api.Logger.Notification("Loaded Beehive Kiln!");

            api.RegisterBlockClass("BlockFirebrickKilnFlue", typeof(BlockFirebrickKilnFlue));
            api.RegisterBlockEntityClass("BlockEntityBeehiveKiln", typeof(BlockEntityBeehiveKiln));
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            // Send connecting players config settings
            this.serverChannel.SendPacket(
                new SyncConfigClientPacket {
                    FiringTimeHours = BeehiveKilnConfig.Loaded.FiringTimeHours,
                    MinimumFiringTemperatureCelsius = BeehiveKilnConfig.Loaded.MinimumFiringTemperatureCelsius, 
                    TemperatureGainPerHourCelsius = BeehiveKilnConfig.Loaded.TemperatureGainPerHourCelsius
                }, player);
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            sapi.Event.PlayerJoin += this.OnPlayerJoin; 
            
            // Create server channel for config data sync
            this.serverChannel = sapi.Network.RegisterChannel("beehivekiln")
                .RegisterMessageType<SyncConfigClientPacket>()
                .SetMessageHandler<SyncConfigClientPacket>((player, packet) => {});
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            // Sync config settings with clients
            capi.Network.RegisterChannel("beehivekiln")
                .RegisterMessageType<SyncConfigClientPacket>()
                .SetMessageHandler<SyncConfigClientPacket>(p => {
                    this.Mod.Logger.Event("Received config settings from server");
                    BeehiveKilnConfig.Loaded.FiringTimeHours = p.FiringTimeHours;
                    BeehiveKilnConfig.Loaded.MinimumFiringTemperatureCelsius = p.MinimumFiringTemperatureCelsius;
                    BeehiveKilnConfig.Loaded.TemperatureGainPerHourCelsius = p.TemperatureGainPerHourCelsius;
                });
        }
        
        public override void Dispose()
        {
            if (this.api is ICoreServerAPI sapi)
            {
                sapi.Event.PlayerJoin -= this.OnPlayerJoin;
            }
        }
    }
}
