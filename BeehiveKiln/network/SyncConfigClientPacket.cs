using ProtoBuf;

namespace beehivekiln.network
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SyncConfigClientPacket
    {
        public double FiringTimeHours;
        public int MinimumFiringTemperatureCelsius;
        public int TemperatureGainPerHourCelsius;
    }
}