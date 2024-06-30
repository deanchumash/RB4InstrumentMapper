using System;
using System.Diagnostics;

namespace RB4InstrumentMapper.Parsing
{
    internal static class PacketLogging
    {
        public static void WritePacket(XboxPacket packet)
        {
            if (!BackendSettings.LogPackets)
                return;

            WriteLine(packet.ToString());
        }

        public static void WriteLine(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
            Logging.Packet_WriteLine(message);
        }
    }
}