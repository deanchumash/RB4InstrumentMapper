using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpGameInput;

namespace RB4InstrumentMapper.Parsing
{
    internal class GameInputBackendDevice : IDisposable, IBackendClient
    {
        private const int E_NOTIMPL = unchecked((int)0x80004001);

        private IGameInput gameInput;
        private IGameInputDevice device;

        private DeviceMapper deviceMapper;
        private volatile bool inputsEnabled = false;

        private GameInputCallbackToken readingToken;

        private Thread readThread;
        private EventWaitHandle threadStop;
        private volatile bool ioError = false;

        public ushort VendorId { get; }
        public ushort ProductId { get; }

        public bool MapGuideButton => false;

        public IGameInputDevice Device => device;

        public GameInputBackendDevice(IGameInput gameInput, IGameInputDevice device)
        {
            this.gameInput = gameInput.Duplicate();
            this.device = device.Duplicate();

            ref readonly var info = ref device.DeviceInfo;
            VendorId = info.vendorId;
            ProductId = info.productId;

            if (!gameInput.RegisterReadingCallback(
                device,
                GameInputKind.RawDeviceReport,
                0,
                null,
                OnDeviceReading,
                out readingToken,
                out int result
            ))
            {
                // RegisterReadingCallback is not implemented at the time of writing,
                // so we fall back to polling as a stopgap until it is implemented
                if (result != E_NOTIMPL)
                    PacketLogging.PrintMessage($"Couldn't register reading callback, falling back to manual polling. Error result: 0x{result:X4}");
                threadStop = new EventWaitHandle(false, EventResetMode.ManualReset);
                readThread = new Thread(ReadThread) { IsBackground = true };
                readThread.Start();
            }
        }

        // No finalizer, all resources are managed

        public void Dispose()
        {
            readingToken?.Unregister(5000);
            readingToken = null;

            threadStop?.Set();
            readThread?.Join();
            readThread = null;

            threadStop?.Dispose();
            threadStop = null;

            deviceMapper?.Dispose();
            deviceMapper = null;

            device?.Dispose();
            device = null;

            gameInput?.Dispose();
            gameInput = null;
        }

        public void EnableInputs(bool enabled)
        {
            // Defer to read thread/callback
            inputsEnabled = enabled;
        }

        private void OnDeviceReading(
            LightGameInputCallbackToken callbackToken,
            object context,
            LightIGameInputReading reading,
            bool hasOverrunOccurred
        )
        {
            using (reading)
            {
                if (!HandleReading(reading))
                    callbackToken.Stop();
            }
        }

        private unsafe void ReadThread()
        {
            ulong lastTimestamp = 0;
            while (!threadStop.WaitOne(0))
            {
                int hResult = gameInput.GetCurrentReading(GameInputKind.RawDeviceReport, device, out var reading);
                if (hResult < 0)
                {
                    if (hResult == (int)GameInputResult.ReadingNotFound)
                        continue;

                    if (hResult != (int)GameInputResult.DeviceDisconnected)
                        PacketLogging.PrintVerbose($"Failed to get current reading: 0x{hResult:X8}");
                    break;
                }

                using (reading)
                {
                    // Ignore unchanged reports
                    ulong timestamp = reading.GetTimestamp();
                    if (lastTimestamp == timestamp)
                        continue;
                    lastTimestamp = timestamp;

                    if (!HandleReading(reading))
                        break;
                }
            }
        }

        private unsafe bool HandleReading(LightIGameInputReading reading)
        {
            bool enabled = inputsEnabled;
            if (enabled != (deviceMapper != null))
            {
                deviceMapper?.Dispose();
                deviceMapper = null;

                if (enabled)
                {
                    deviceMapper = MapperFactory.GetByHardwareIds(this);
                    if (deviceMapper == null)
                    {
                        GameInputBackend.QueueForRemoval(this);
                        return false;
                    }
                }
            }

            if (!enabled)
                return true;

            if (!reading.GetRawReport(out var rawReport))
            {
                PacketLogging.PrintVerbose("Could not get raw report!");
                return false;
            }

            using (rawReport)
            {
                uint reportId = rawReport.ReportInfo.id;
                UIntPtr size = rawReport.GetRawDataSize();

                byte* buffer = stackalloc byte[(int)size];
                UIntPtr readSize = rawReport.GetRawData(size, buffer);
                Debug.Assert(size == readSize);

                var data = new ReadOnlySpan<byte>(buffer, (int)size);
                var packet = new XboxPacket(data, directionIn: true);
                PacketLogging.LogPacket(packet);

                var result = deviceMapper.HandleMessage((byte)reportId, data);
                if (result == XboxResult.Disconnected)
                    return false;
            }

            return true;
        }

        public XboxResult SendMessage(XboxMessage message)
        {
            return SendMessage(message.Header, message.Data);
        }

        public XboxResult SendMessage<T>(XboxMessage<T> message)
            where T : unmanaged
        {
            return SendMessage(message.Header, ref message.Data);
        }

        public XboxResult SendMessage(XboxCommandHeader header)
        {
            return SendMessage(header, Span<byte>.Empty);
        }

        public unsafe XboxResult SendMessage<T>(XboxCommandHeader header, ref T data)
            where T : unmanaged
        {
            // Create a byte buffer for the given data
            var writeBuffer = new Span<byte>(Unsafe.AsPointer(ref data), sizeof(T));
            return SendMessage(header, writeBuffer);
        }

        public unsafe XboxResult SendMessage(XboxCommandHeader header, Span<byte> data)
        {
            var xboxPacket = new XboxPacket(data, directionIn: false);
            PacketLogging.LogPacket(xboxPacket);

            if (ioError)
                return XboxResult.Disconnected;

            const int retryThreshold = 3;
            for (int tryCount = 0; tryCount < retryThreshold; tryCount++,
                PacketLogging.PrintVerbose($"Error while sending report! (Attempt {tryCount})"))
            {
                int hResult = device.CreateRawDeviceReport(header.CommandId, GameInputRawDeviceReportKind.Output, out var report);
                if (hResult < 0)
                {
                    if (hResult == (int)GameInputResult.DeviceDisconnected)
                        return XboxResult.Disconnected;

                    PacketLogging.PrintVerbose($"Failed to create raw report: 0x{hResult:X8}");
                    continue;
                }

                fixed (byte* ptr = data)
                {
                    if (!report.SetRawData((UIntPtr)data.Length, ptr))
                    {
                        PacketLogging.PrintVerbose("Failed to set raw report data!");
                        continue;
                    }
                }

                hResult = device.SendRawDeviceOutput(report);
                if (hResult < 0)
                {
                    // This call is not implemented as of the time of writing,
                    // and treat as success
                    if (hResult == E_NOTIMPL)
                        return XboxResult.Success;

                    if (hResult == (int)GameInputResult.DeviceDisconnected)
                        return XboxResult.Disconnected;

                    PacketLogging.PrintVerbose($"Failed to send raw report: 0x{hResult:X8}");
                    continue;
                }

                return XboxResult.Success;
            }

            ioError = true;
            return XboxResult.Disconnected;
        }
    }
}