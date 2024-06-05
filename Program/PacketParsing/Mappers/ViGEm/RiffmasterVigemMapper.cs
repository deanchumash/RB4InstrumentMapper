using System;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace RB4InstrumentMapper.Parsing
{
    /// <summary>
    /// Maps Riffmaster guitar inputs to a ViGEmBus device.
    /// </summary>
    internal class RiffmasterVigemMapper : VigemMapper
    {
        public RiffmasterVigemMapper(IBackendClient client)
            : base(client)
        {
        }

        protected override XboxResult OnMessageReceived(byte command, ReadOnlySpan<byte> data)
        {
            switch (command)
            {
                case XboxRiffmasterInput.CommandId:
                    return ParseInput(data);

                default:
                    return XboxResult.Success;
            }
        }

        private unsafe XboxResult ParseInput(ReadOnlySpan<byte> data)
        {
            if (!ParsingUtils.TryRead(data, out XboxRiffmasterInput guitarReport))
                return XboxResult.InvalidMessage;

            HandleReport(device, guitarReport);

            // Send data
            return SubmitReport();
        }

        /// <summary>
        /// Maps guitar input data to an Xbox 360 controller.
        /// </summary>
        internal static void HandleReport(IXbox360Controller device, in XboxRiffmasterInput report)
        {
            // Guitar inputs
            GuitarVigemMapper.HandleReport(device, report.Base);

            // Joystick
            device.SetAxisValue(Xbox360Axis.LeftThumbX, report.JoystickX);
            device.SetAxisValue(Xbox360Axis.LeftThumbY, report.JoystickY);
            // Note: overwrites the solo fret flag
            device.SetButtonState(Xbox360Button.LeftThumb, report.JoystickClick);
        }
    }
}
