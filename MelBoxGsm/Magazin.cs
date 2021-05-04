
namespace MelBoxGsm
{
    public static partial class Gsm
    {
        public enum Modem
        {
            SignalQuality,
            BitErrorRate,
            OwnPhoneNumber,
            OwnName,
            ServiceCenterNumber,
            NetworkRegistration,
            ProviderName,
            IncomingCall
        }

    }

    public static class GsmStatus
    {
        const string init = "-unbekannt-";
        public static int SignalQuality { get; set; } = -1;
        public static double SignalErrorRate { get; set; } = -1;
        public static string OwnName { get; set; } = init;
        public static string OwnNumber { get; set; } = init;
        public static string NetworkRegistration { get; set; } = init;
        public static string ServiceCenterNumber { get; set; } = init;
        public static string ProviderName { get; set; } = init;
    }
}
