using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RB4InstrumentMapper.Parsing;
using RB4InstrumentMapper.Properties;

namespace RB4InstrumentMapper
{
    public class Program
    {
        private const string WinUsbOption = "--winusb";
        private const string RevertOption = "--revert";
        private const string ReplayOption = "--replay";
        private const string MappingModeOption = "--mappingMode";
        private const string StartCaptureOption = "--startCapture";

        private enum ControllerType
        {
            None = -1,
            vJoy = 0,
            ViGEmBus = 1,
            RPCS3 = 2
        }

        [STAThread]
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            int exitCode = 0;
            bool run = true;
            Console.WriteLine(args);
            for (int i = 0; i < args.Length; i++)
            {
                string type = args[i];
                string path;

                switch (type)
                {
                    case MappingModeOption:
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Not enough arguments!");
                            exitCode = 1;
                            run = false;
                            break;
                        }
                        i++;
                        if (Array.Exists(Enum.GetNames(typeof(ControllerType)), mappingMode => mappingMode == args[i]))
                        {
                            Settings.Default.controllerDeviceType = (int)Enum.Parse(typeof(ControllerType), args[i]);
                        }
                        else
                        {
                            Console.WriteLine($"Mapping mode {type} is invalid");
                            exitCode = 1;
                            run = false;
                        }
                        break;
                    case StartCaptureOption:
                        Settings.Default.startCapture = 1;
                        break;
                    case WinUsbOption:
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Not enough arguments!");
                            exitCode = 1;
                            run = false;
                            break;
                        }
                        i++;
                        path = args[i];
                        exitCode = WinUsbBackend.SwitchDeviceToWinUSB(path) ? 0 : -1;
                        run = false;
                        break;
                    case RevertOption:
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Not enough arguments!");
                            exitCode = 1;
                            run = false;
                            break;
                        }
                        i++;
                        path = args[i];
                        exitCode = WinUsbBackend.RevertDevice(path) ? 0 : -1;
                        run = false;
                        break;
                    case ReplayOption:
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("Not enough arguments!");
                            exitCode = 1;
                            run = false;
                            break;
                        }
                        i++;
                        path = args[i];
                        exitCode = ReplayBackend.ReplayLog(path) ? 0 : -1;
                        run = false;
                        break;
                    default:
                        Console.WriteLine($"Invalid option '{type}'!");
                        exitCode = -1;
                        run = false;
                        break;
                }
            }

            // Regular app startup
            if (run)
            {
                App.Main();
                return 0;
            }


            Logging.CloseAll();
            return exitCode;
        }

        public static Task<bool> StartWinUsbProcess(string instanceId)
        {
            string args = $"{WinUsbOption} {instanceId}";
            return RunElevated(args);
        }

        public static Task<bool> StartRevertProcess(string instanceId)
        {
            string args = $"{RevertOption} {instanceId}";
            return RunElevated(args);
        }

        private static async Task<bool> RunElevated(string args)
        {
            string location = Assembly.GetEntryAssembly().Location;
            var processInfo = new ProcessStartInfo()
            {
                Verb = "runas", // Run as admin
                FileName = location,
                Arguments = args,
                CreateNoWindow = true,
            };

            try
            {
                var process = Process.Start(processInfo);
                await Task.Run(process.WaitForExit);
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to create elevated process!");
                Debug.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Logs unhandled exceptions to a file and prompts the user with the exception message.
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            // The unhandled exception
            var unhandledException = args.ExceptionObject as Exception;

            // MessageBox message
            var message = new StringBuilder();
            message.AppendLine("An unhandled error has occured:");
            message.AppendLine();
            message.AppendLine(unhandledException.GetFirstLine());
            message.AppendLine();

            // Create log if it hasn't been created yet
            Logging.CreateMainLog();
            // Use an alternate message if log couldn't be created
            if (Logging.MainLogExists)
            {
                // Log exception
                Logging.Main_WriteLine("-------------------");
                Logging.Main_WriteLine("UNHANDLED EXCEPTION");
                Logging.Main_WriteLine("-------------------");
                Logging.Main_WriteException(unhandledException, "Unhandled exception!");

                // Complete the message buffer
                message.AppendLine("A log of the error has been created, do you want to open it?");

                // Display message
                var result = MessageBox.Show(message.ToString(), "Unhandled Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                // If user requested to, open the log
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(Logging.LogFolderPath);
                }
            }
            else
            {
                // Complete the message buffer
                message.AppendLine("An error log was unable to be created.");

                // Display message
                MessageBox.Show(message.ToString(), "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Close the log files
            Logging.CloseAll();
            // Save settings
            Settings.Default.Save();
        }
    }
}