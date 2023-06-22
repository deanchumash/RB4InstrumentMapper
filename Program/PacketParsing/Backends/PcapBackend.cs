using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using SharpPcap;

namespace RB4InstrumentMapper.Parsing
{
    public delegate void PacketReceivedHandler(DateTime timestamp, ReadOnlySpan<byte> data);

    /// <summary>
    /// A standard IEEE 802.11 QoS header.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct QoSHeader
    {
        ushort frameControl;
        ushort durationId;
        fixed byte receiverAddress[6];
        fixed byte transmitterAddress[6];
        fixed byte destinationAddress[6];
        ushort sequenceControl;
        ushort qosControl;

        public byte FrameType => (byte)((frameControl & 0xC) >> 2);
        public byte FrameSubtype => (byte)((frameControl & 0xF0) >> 4);

        public ulong DeviceId
        {
            get
            {
                fixed (byte* ptr = transmitterAddress)
                {
                    // Read a ulong starting from deviceId_1
                    ulong deviceId = *(ulong*)ptr;
                    // Last 2 bytes aren't part of the device ID
                    deviceId &= 0x0000FFFF_FFFFFFFF;
                    return deviceId;
                }
            }
        }
    }

    public static class PcapBackend
    {
        public static bool LogPackets = false;

        private static ILiveDevice captureDevice = null;
        private static readonly Dictionary<ulong, XboxDevice> devices = new Dictionary<ulong, XboxDevice>();

        public static event Action OnCaptureStop;

        /// <summary>
        /// Starts capturing packets from the given device.
        /// </summary>
        public static void StartCapture(ILiveDevice device)
        {
            // Open the device
            device.Open(new DeviceConfiguration()
            {
                Snaplen = 45, // Capture small packets
                Mode = DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, // Promiscuous mode with maximum speed
                ReadTimeout = 50 // Read timeout
            });

            // Configure packet receive event handler
            device.OnPacketArrival += OnPacketArrival;
            
            // Start capture
            device.StartCapture();
            captureDevice = device;
        }

        /// <summary>
        /// Stops capturing packets.
        /// </summary>
        public static void StopCapture()
        {
            if (captureDevice != null)
            {
                captureDevice.OnPacketArrival -= OnPacketArrival;
                captureDevice.Close();
                captureDevice = null;
            }

            // Clean up devices
            foreach (XboxDevice device in devices.Values)
            {
                device.Dispose();
            }
            devices.Clear();
        }

        /// <summary>
        /// Handles captured packets.
        /// </summary>
        private static unsafe void OnPacketArrival(object sender, PacketCapture packet)
        {
            // Ignore invalid packets or packets with only a header
            if (packet.Data.Length <= sizeof(QoSHeader))
            {
                return;
            }

            // Split header and packet data, and read header out
            var headerData = packet.Data.Slice(0, sizeof(QoSHeader));
            var packetData = packet.Data.Slice(sizeof(QoSHeader));
            if (!MemoryMarshal.TryRead(packet.Data, out QoSHeader header))
            {
                return;
            }

            // Ensure type and subtype are Data, QoS Data respectively
            // Other frame types are irrelevant for our purposes
            if (header.FrameType != 2 || header.FrameSubtype != 8)
            {
                return;
            }

            // Debugging (if enabled)
            if (LogPackets)
            {
                string packetLogString = $"{packet.Header.Timeval.Date:yyyy-MM-dd hh:mm:ss.fff} [{packet.Data.Length}] " +
                    $"{BitConverter.ToString(headerData.ToArray())} | {BitConverter.ToString(packetData.ToArray())}";
                Console.WriteLine(packetLogString);
                Logging.Packet_WriteLine(packetLogString);
            }

            // Check if device ID has been encountered yet
            ulong deviceId = header.DeviceId;
            if (!devices.TryGetValue(deviceId, out var device))
            {
                device = new XboxDevice();
                devices.Add(deviceId, device);
                Console.WriteLine($"Encountered new device with ID {deviceId:X12}");
            }

            try
            {
                device.HandlePacket(packetData);
            }
            catch (ThreadAbortException)
            {
                // Don't log thread aborts, just return
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while handling packet: {ex.GetFirstLine()}");
                Logging.Main_WriteException(ex, "Context: Unhandled error during packet handling");

                // Stop capture
                StopCapture();
                OnCaptureStop?.Invoke();
                return;
            }
        }
    }
}