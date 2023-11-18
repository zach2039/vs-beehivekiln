
namespace beehivekiln
{
    public class BeehiveKilnConfig 
    {
        public static BeehiveKilnConfig Loaded { get; set; } = new BeehiveKilnConfig();

        public double FiringTimeHours { get; set; } = 12.0;
        public int MinimumFiringTemperatureCelsius { get; set; } = 500;
        public int TemperatureGainPerHourCelsius { get; set; } = 250;
    }
}