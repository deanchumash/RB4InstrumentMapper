using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SharpGameInput;

namespace RB4InstrumentMapper.Parsing
{
    public static class GameInputBackend
    {
        private static IGameInput gameInput;

        private static bool inputsEnabled = false;

        private static readonly ConcurrentDictionary<IGameInputDevice, GameInputBackendDevice> devices
            = new ConcurrentDictionary<IGameInputDevice, GameInputBackendDevice>();

        private static GameInputCallbackToken deviceCallbackToken;

        public static int DeviceCount => devices.Count;

        public static event Action DeviceCountChanged;

        public static bool Initialized { get; private set; } = false;

        public static void Initialize()
        {
            if (Initialized)
                return;

            if (!GameInput.Create(out gameInput, out int result))
            {
                PacketLogging.PrintMessage($"Failed to create GameInput instance: 0x{result:X8}");
                return;
            }

            if (!gameInput.RegisterDeviceCallback(
                null,
                GameInputKind.RawDeviceReport,
                GameInputDeviceStatus.Connected,
                GameInputEnumerationKind.AsyncEnumeration,
                null,
                OnDeviceStatusChange,
                out deviceCallbackToken,
                out result
            ))
            {
                gameInput?.Dispose();
                PacketLogging.PrintMessage($"Failed to register GameInput device callback: 0x{result:X8}");
                return;
            }

            PacketLogging.PrintMessage("Initialized GameInput backend.");
        }

        public static void Uninitialize()
        {
            if (!Initialized)
                return;

            deviceCallbackToken?.Unregister(1_000_000);
            deviceCallbackToken = null;

            foreach (var pair in devices)
            {
                pair.Key.Dispose();
                pair.Value.Dispose();
            }
            devices.Clear();

            gameInput?.Dispose();
            gameInput = null;
        }

        public static void StartCapture()
        {
            inputsEnabled = true;
            foreach (var device in devices.Values)
            {
                device.EnableInputs(true);
            }
        }

        public static void StopCapture()
        {
            inputsEnabled = false;
            foreach (var device in devices.Values)
            {
                device.EnableInputs(false);
            }
        }

        internal static async void QueueForRemoval(GameInputBackendDevice device)
        {
            // Force ourselves out of the device read thread/callback and defer to later
            await Task.Yield();

            if (!devices.TryRemove(device.Device, out _))
                return;

            PacketLogging.PrintMessage($"Removing GameInput device {DeviceInfoToString(device.Device.DeviceInfo)}");
            device.Dispose();
            DeviceCountChanged?.Invoke();
        }

        private static void OnDeviceStatusChange(
            LightGameInputCallbackToken callbackToken,
            object context,
            LightIGameInputDevice device,
            ulong timestamp,
            GameInputDeviceStatus currentStatus,
            GameInputDeviceStatus previousStatus
        )
        {
            // Ignore if connection status hasn't changed
            if ((currentStatus & GameInputDeviceStatus.Connected) == (previousStatus & GameInputDeviceStatus.Connected))
                return;

            ref readonly var info = ref device.DeviceInfo;

            // We only cover Xbox One devices
            if (info.deviceFamily != GameInputDeviceFamily.XboxOne)
                return;

            // We only support devices with raw reports
            if ((info.supportedInput & GameInputKind.RawDeviceReport) == 0)
                return;

            if ((currentStatus & GameInputDeviceStatus.Connected) != 0)
            {
                if (!MapperFactory.IsSupportedByHardwareIds(info.vendorId, info.productId))
                    return;

                var permaDevice = device.ToSafeHandle();
                var backendDevice = new GameInputBackendDevice(gameInput, permaDevice);
                backendDevice.EnableInputs(inputsEnabled);

                if (devices.TryAdd(permaDevice, backendDevice))
                {
                    PacketLogging.PrintMessage($"GameInput device {DeviceInfoToString(info)} connected");
                    DeviceCountChanged?.Invoke();
                }
            }
            else
            {
                if (!devices.TryRemove(device.ToSafeHandle(), out var backendDevice))
                    return;

                backendDevice.Dispose();
                PacketLogging.PrintMessage($"GameInput device {DeviceInfoToString(info)} disconnected");
                DeviceCountChanged?.Invoke();
            }
        }

        private static unsafe string DeviceInfoToString(in GameInputDeviceInfo info)
        {
            if (info.displayName != null)
                return $"{info.displayName->ToString()} ({info.deviceId})";
            else
                return $"{info.deviceId}";
        }
    }
}