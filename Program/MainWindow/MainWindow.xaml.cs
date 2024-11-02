using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RB4InstrumentMapper.Parsing;
using RB4InstrumentMapper.Properties;
using RB4InstrumentMapper.Vigem;
using RB4InstrumentMapper.Vjoy;

namespace RB4InstrumentMapper
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Dispatcher to send changes to UI.
        /// </summary>
        private static Dispatcher uiDispatcher = null;

        /// <summary>
        /// Whether or not packet capture is active.
        /// </summary>
        private bool packetCaptureActive = false;

        /// <summary>
        /// Whether or not packets should be logged to a file.
        /// </summary>
        private bool packetDebugLog = false;

        /// <summary>
        /// Available controller emulation types.
        /// </summary>
        private enum ControllerType
        {
            None = -1,
            vJoy = 0,
            ViGEmBus = 1,
            RPCS3 = 2
        }

        public MainWindow()
        {
            InitializeComponent();

            var version = Assembly.GetEntryAssembly().GetName().Version;
            versionLabel.Content = $"v{version}";
#if DEBUG
            versionLabel.Content += " Debug";
#endif

            // Capture Dispatcher object for use in callbacks
            uiDispatcher = Dispatcher;
        }

        /// <summary>
        /// Called when the window loads.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Connect to console
            var textboxConsole = new TextBoxWriter(messageConsole);
            Console.SetOut(textboxConsole);

            // Check for vJoy
            bool vjoyFound = VjoyClient.Enabled;
            if (vjoyFound)
            {
                // Log vJoy driver attributes (Vendor Name, Product Name, Version Number)
                Console.WriteLine($"vJoy found! - Vendor: {VjoyClient.Manufacturer}, Product: {VjoyClient.Product}, Version Number: {VjoyClient.SerialNumber}");

                if (VjoyClient.GetAvailableDeviceCount() > 0)
                {
                    vjoyDeviceTypeOption.IsEnabled = true;
                }
                else
                {
                    Console.WriteLine("No vJoy devices found. vJoy selection will be unavailable.");
                    vjoyDeviceTypeOption.IsEnabled = false;
                    vjoyDeviceTypeOption.IsSelected = false;
                }
            }
            else
            {
                Console.WriteLine("No vJoy driver found, or vJoy is disabled. vJoy selection will be unavailable.");
                vjoyDeviceTypeOption.IsEnabled = false;
                vjoyDeviceTypeOption.IsSelected = false;
            }

            // Check for ViGEmBus
            bool vigemFound = VigemClient.TryInitialize();
            if (vigemFound)
            {
                Console.WriteLine("ViGEmBus found!");
                vigemDeviceTypeOption.IsEnabled = true;
                rpcs3DeviceTypeOption.IsEnabled = true;
            }
            else
            {
                Console.WriteLine("ViGEmBus not found. ViGEmBus selection will be unavailable.");
                vigemDeviceTypeOption.IsEnabled = false;
                vigemDeviceTypeOption.IsSelected = false;
                rpcs3DeviceTypeOption.IsEnabled = false;
                rpcs3DeviceTypeOption.IsSelected = false;
            }

            // Exit if neither ViGEmBus nor vJoy are installed
            if (!vjoyFound && !vigemFound)
            {
                MessageBox.Show("No controller emulators found! Please install vJoy or ViGEmBus.\nThe program will now shut down.", "No Controller Emulators Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Load console/log settings
            SetPacketDebug(Settings.Default.packetDebug);
            SetPacketDebugLog(Settings.Default.packetDebugLog);
            SetVerboseLogging(Settings.Default.verboseLogging);

            // Load backend settings
            // Done after initializing virtual controller clients
            SetDeviceType((ControllerType)Settings.Default.controllerDeviceType);
            SetUsbEnabled(Settings.Default.usbEnabled);

            // Initialize GameInput
            GameInputBackend.DeviceCountChanged += GameInputDeviceCountChanged;
            GameInputBackend.Initialize();
            SetGameInputInitialized(GameInputBackend.Initialized);

            // Start capture if option set
            if (Settings.Default.startCapture == 1)
            {
                StartCapture();
            }
        }

        /// <summary>
        /// Called when the window has closed.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Shut down
            if (packetCaptureActive)
            {
                StopCapture();
            }

            GameInputBackend.Uninitialize();
            GameInputBackend.DeviceCountChanged -= GameInputDeviceCountChanged;

            WinUsbBackend.Uninitialize();
            WinUsbBackend.DeviceCountChanged -= WinUsbDeviceCountChanged;

            // Clean up
            Settings.Default.Save();
            Logging.CloseAll();
            VigemClient.Dispose();
        }

        private void SetStartButtonState()
        {
            bool startEnabled = true;

            // Emulation type must be selected
            bool emulationTypeSelected = BackendSettings.MapperMode != MappingMode.NotSet;
            controllerDeviceTypeLabel.FontWeight = !emulationTypeSelected &&
                controllerDeviceTypeLabel.IsEnabled ? FontWeights.Bold : FontWeights.Normal;
            startEnabled &= emulationTypeSelected;

            // Enable start button if all the conditions above pass
            startButton.IsEnabled = startEnabled;

            // Display a message explaining the current start button state
            if (startEnabled)
                startStatusLabel.Content = "Ready to run!";
            else if (!emulationTypeSelected)
                startStatusLabel.Content = "Please select a controller emulation mode.";
        }

        private void GameInputDeviceCountChanged()
        {
            uiDispatcher.Invoke(() => gameInputDeviceCountLabel.Content = $"Count: {GameInputBackend.DeviceCount}");
        }

        private void WinUsbDeviceCountChanged()
        {
            uiDispatcher.Invoke(() => usbDeviceCountLabel.Content = $"Count: {WinUsbBackend.DeviceCount}");
        }

        /// <summary>
        /// Configures the Pcap device and controller devices, and starts packet capture.
        /// </summary>
        private async void StartCapture()
        {
            await WinUsbBackend.StartCapture();
            GameInputBackend.StartCapture();

            // Enable packet capture active flag
            packetCaptureActive = true;

            // Set window controls
            usbEnabledCheckBox.IsEnabled = false;
            usbConfigureDevicesButton.IsEnabled = false;

            controllerDeviceTypeCombo.IsEnabled = false;

            packetDebugCheckBox.IsEnabled = false;
            packetLogCheckBox.IsEnabled = false;
            verboseLogCheckBox.IsEnabled = false;

            startStatusLabel.Content = "Running...";
            startButton.Content = "Stop";

            // Initialize packet log
            if (packetDebugLog)
            {
                if (!Logging.CreatePacketLog())
                {
                    packetDebugLog = false;
                    // Remaining context for this message is inside of the log creation
                    Console.WriteLine("Disabled packet logging for this capture session.");
                }
            }
        }

        /// <summary>
        /// Stops packet capture/mapping and resets Pcap/controller objects.
        /// </summary>
        private async void StopCapture()
        {
            await WinUsbBackend.StopCapture();
            GameInputBackend.StopCapture();

            // Store whether or not the packet log was created
            bool doPacketLogMessage = Logging.PacketLogExists;
            // Close packet log file
            Logging.ClosePacketLog();

            // Disable packet capture active flag
            packetCaptureActive = false;

            // Set window controls
            usbEnabledCheckBox.IsEnabled = true;
            SetUsbEnabled(Settings.Default.usbEnabled);

            packetDebugCheckBox.IsEnabled = true;
            packetLogCheckBox.IsEnabled = true;
            verboseLogCheckBox.IsEnabled = true;

            controllerDeviceTypeCombo.IsEnabled = true;

            startButton.Content = "Start";

            // Force a refresh of the controller textbox
            controllerDeviceTypeCombo_SelectionChanged(null, null);

            Console.WriteLine("Stopped capture.");
            if (doPacketLogMessage)
            {
                Console.WriteLine($"Packet logs may be found in {Logging.PacketLogFolderPath}");
            }
        }

        private void SetGameInputInitialized(bool enabled)
        {
            gameInputDeviceCountLabel.IsEnabled = enabled;
            gameInputRefreshButton.Content = enabled ? "Refresh" : "Initialize";
        }

        private void SetUsbEnabled(bool enabled)
        {
            if (usbEnabledCheckBox.IsChecked != enabled)
            {
                usbEnabledCheckBox.IsChecked = enabled;
                return;
            }

            Settings.Default.usbEnabled = enabled;

            usbDeviceCountLabel.IsEnabled = enabled;
            usbConfigureDevicesButton.IsEnabled = enabled;

            if (WinUsbBackend.Initialized != enabled)
            {
                if (enabled)
                {
                    WinUsbBackend.DeviceCountChanged += WinUsbDeviceCountChanged;
                    WinUsbBackend.Initialize();
                }
                else
                {
                    WinUsbBackend.Uninitialize();
                    WinUsbBackend.DeviceCountChanged -= WinUsbDeviceCountChanged;
                }
            }

            SetStartButtonState();
        }

        private void SetPacketDebug(bool enabled)
        {
            if (packetDebugCheckBox.IsChecked != enabled)
            {
                packetDebugCheckBox.IsChecked = enabled;
                return;
            }

            Settings.Default.packetDebug = enabled;

            BackendSettings.LogPackets = enabled;
            packetLogCheckBox.IsEnabled = enabled;
            packetDebugLog = enabled && packetLogCheckBox.IsChecked.GetValueOrDefault();
        }

        private void SetPacketDebugLog(bool enabled)
        {
            if (packetLogCheckBox.IsChecked != enabled)
            {
                packetLogCheckBox.IsChecked = enabled;
                return;
            }

            packetDebugLog = Settings.Default.packetDebugLog = enabled;
        }

        private void SetVerboseLogging(bool enabled)
        {
            if (verboseLogCheckBox.IsChecked != enabled)
            {
                verboseLogCheckBox.IsChecked = enabled;
                return;
            }

            Settings.Default.verboseLogging = enabled;
            Logging.PrintVerbose = enabled;
        }

        private void SetDeviceType(ControllerType type)
        {
            int typeInt = (int)type;
            if (controllerDeviceTypeCombo.SelectedIndex != typeInt)
            {
                // Set device type selection to the correct thing
                // Setting this fires off the handler, so we need to return
                // and let the second call set things
                controllerDeviceTypeCombo.SelectedIndex = typeInt;
                return;
            }

            Settings.Default.controllerDeviceType = typeInt;

            switch (type)
            {
                case ControllerType.vJoy:
                    if (vjoyDeviceTypeOption.IsEnabled && VjoyClient.GetAvailableDeviceCount() > 0)
                    {
                        BackendSettings.MapperMode = MappingMode.vJoy;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        BackendSettings.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.ViGEmBus:
                    if (vigemDeviceTypeOption.IsEnabled && VigemClient.Initialized)
                    {
                        BackendSettings.MapperMode = MappingMode.ViGEmBus;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        BackendSettings.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.RPCS3:
                    if (rpcs3DeviceTypeOption.IsEnabled && VigemClient.Initialized)
                    {
                        BackendSettings.MapperMode = MappingMode.RPCS3;
                    }
                    else
                    {
                        // Reset device type selection
                        // Setting this fires off the handler again, no extra handling is needed
                        BackendSettings.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                        return;
                    }
                    break;

                case ControllerType.None:
                    BackendSettings.MapperMode = MappingMode.NotSet;
                    break;

                default:
                    BackendSettings.MapperMode = MappingMode.NotSet;
                        controllerDeviceTypeCombo.SelectedIndex = -1;
                    break;
            }

            SetStartButtonState();
        }

        /// <summary>
        /// Handles the click of the Start button.
        /// </summary>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (!packetCaptureActive)
            {
                StartCapture();
            }
            else
            {
                StopCapture();
            }
        }

        /// <summary>
        /// Handles the verbose error checkbox being checked.
        /// </summary>
        private void usbEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool usbEnabled = usbEnabledCheckBox.IsChecked.GetValueOrDefault();
            SetUsbEnabled(usbEnabled);
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked/unchecked.
        /// </summary>
        private void packetDebugCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool packetDebug = packetDebugCheckBox.IsChecked.GetValueOrDefault();
            SetPacketDebug(packetDebug);
        }

        /// <summary>
        /// Handles the packet debug checkbox being checked.
        /// </summary>
        private void packetLogCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool packetDebugLog = packetLogCheckBox.IsChecked.GetValueOrDefault();
            SetPacketDebugLog(packetDebugLog);
        }

        /// <summary>
        /// Handles the verbose error checkbox being checked.
        /// </summary>
        private void verboseLogCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool verboseErrors = verboseLogCheckBox.IsChecked.GetValueOrDefault();
            SetVerboseLogging(verboseErrors);
        }

        /// <summary>
        /// Handles the click of the GameInput Refresh button.
        /// </summary>
        private void gameInputRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameInputBackend.Initialized)
            {
                GameInputBackend.Refresh();
            }
            else
            {
                GameInputBackend.Initialize();
                SetGameInputInitialized(GameInputBackend.Initialized);
            }
        }

        /// <summary>
        /// Handles the controller type setting being changed.
        /// </summary>
        private void controllerDeviceTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDeviceType((ControllerType)controllerDeviceTypeCombo.SelectedIndex);
        }

        /// <summary>
        /// Handles the click of the USB Configure Devices button.
        /// </summary>
        private void usbConfigureDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable GameInput to prevent weird issues
            // Otherwise, devices switched over aren't disconnected on the GameInput side,
            // and devices reverted aren't picked up unless it's plugged in before RB4IM starts.
            // Both require a restart of the PC or GameInput service to fix.
            GameInputBackend.Uninitialize();

            var window = new UsbDeviceListWindow();
            window.ShowDialog();

            GameInputBackend.Initialize();
        }
    }
}
