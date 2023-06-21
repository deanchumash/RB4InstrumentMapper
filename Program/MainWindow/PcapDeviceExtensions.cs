using SharpPcap;

namespace RB4InstrumentMapper
{
    public static class PcapDeviceExtensions
    {
        /// <summary>
        /// Determines whether or not a capture device is an Xbox One receiver.
        /// </summary>
        public static bool IsXboxOneReceiver(this ILiveDevice device)
        {
            // Depending on the receiver, there are two ways of detection:
            // - Description of "MT7612US_RL"
            // - Empty device properties
            return device.Description == "MT7612US_RL" ||
                (string.IsNullOrWhiteSpace(device.Description) &&
                string.IsNullOrWhiteSpace(device.Filter) &&
                (device.MacAddress == null || device.MacAddress.GetAddressBytes() == null ||
                    device.MacAddress.GetAddressBytes().Length == 0));
        }
    }
}