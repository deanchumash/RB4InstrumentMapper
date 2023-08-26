﻿# RB4InstrumentMapper

A program that maps packets from Xbox One instrument peripherals to virtual controllers, for use in games such as [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper Application Screenshot](/Docs/Images/ProgramScreenshot.png "RB4InstrumentMapper Application Screenshot")

## Requirements

### Software

- Windows 10 64-bit
- [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe) (for wireless peripherals)
- [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest) or [vJoy](https://github.com/jshafer817/vJoy/releases/latest)

### Hardware

- Xbox One wireless receiver (for wireless peripherals)
  - This is *not* the same as Xbox 360 wireless receivers! You must get an Xbox One (or just "Xbox") receiver, such as [this one](https://amzn.to/2W7qQbt). Third-party receivers are untested, and they will not be deliberately supported.

## Hardware Notes

All Xbox One instruments are supported (RB4 guitars/drums, GHL guitar), along with the RB4 wireless legacy adapter.

Jaguar guitars require a firmware update in order to connect to Xbox One receivers.

- [Instructions](https://bit.ly/2UHzonU)
- [Firmware installer backup](https://drive.google.com/file/d/1DQxkkbBfi-UOqdX6vp5TaX6F2N2OBDra/view?usp=drivesdk)

Some guitars/drumkits might not sync properly when using just the sync button. This includes the PDP drumkit and occasionally the Jaguar guitar. Follow these steps to sync your device correctly:

1. Go to Windows settings > Devices > Bluetooth & other devices
2. Click `Add Bluetooth or other device` and pick the `Everything else` option.
3. Press and hold the sync button until the Xbox button light flashes quickly.
4. Select `Xbox compatible game controller` from the list once it appears.
5. If that doesn't work, restart your PC and try again.

## Installation

### If you are using wireless peripherals:

1. Install [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe)

### If you are using wired (USB) peripherals:

1. Download [Zadig](https://zadig.akeo.ie/) and run it.
2. Under Options, select `List All Devices`.
3. Select your device, then change the box to the right of the green/orange arrow to the `WinUSB` driver. Things should look like the example below:

   ![Zadig Example](/Docs/Images/Zadig.png "Zadig Example")

4. Hit `Replace Driver`, and repeat for any additional peripherals you wish to use.

- A future version will make this a little more streamlined and allow you to swap the driver directly in the program.

### Next, install a controller emulation driver:

- Both of these can be installed simultaneously if desired, however RB4InstrumentMapper will only use one of them at a time.
- Option 1: [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest)
  - Recommended, as it requires no configuration and is significantly easier to use. All device outputs will match those of their Xbox 360 counterparts.
- Option 2: [vJoy](https://github.com/jshafer817/vJoy/releases/latest)
  - Supported as an alternative to ViGEmBus, in case of issues. Requires some setup:
  1. Download and install vJoy.
  2. After installing, open your Start menu, find the `vJoy` folder, and open the `Configure vJoy` program inside it.
  3. Configure one device for each one of your controllers, using these settings:
     - Number of Buttons: 16
     - POV Hat Switch: Continuous, POVs: 1
     - Axes: `X`, `Y`, `Z`

     ![vJoy Configuration Screenshot](/Docs/Images/vJoyConfiguration.png "vJoy Configuration Screenshot")

  4. Click Apply.

### Finally, restart your PC.

Things will probably work fine if you don't, but it's generally safest to restart regardless.

## Usage

### If you are using wireless devices (Pcap):

- Ensure the `Enable` checkbox under the Pcap group is checked.
- Select your Xbox One receiver from the dropdown menu.
  - Xbox receivers should be detected automatically. If they are not, click the `Auto-Detect Pcap` button and follow its instructions.
- **Do not connect your instruments yet!** In order for things to work correctly, you must hit Start before connecting them.
  - A best effort is made for devices connected before starting, but correct behavior cannot be ensured for them.

### If you are using wired devices (USB):

- Ensure the `Enable` checkbox under the USB group is checked.
- **Ensure you have installed WinUSB on the devices you want to use! They will not be recognized otherwise!**
- Devices will be detected automatically as they are connected/removed.

### Starting the program

Select either ViGEmBus or vJoy in the Controller Type dropdown, then hit the Start button.

- You may now connect any wireless devices you wish to use.

### Mapping your controls in Clone Hero

Now that the program is running, map your controls for each instrument in Clone Hero:

1. Press Space on the main menu.
2. Click Assign Controller and press a button on the device for it to be assigned.
3. Click the slots in the Controller column to map each of the controls.
4. Repeat for each connected device.
5. Click `Done`.

## Packet Logs

RB4InstrumentMapper is capable of logging packets to a file for debugging purposes. To do so, enable both the `Show Packets (for debugging)` and `Log packets to file` checkboxes, then hit Start. Packet logs get saved to a `RB4InstrumentMapper` > `PacketLogs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for packet parsing issues.

Note that these settings are meant for debugging purposes only, leaving them enabled can reduce the performance of the program somewhat.

## Error Logs

In the case that the program crashes, an error log is saved to a `RB4InstrumentMapper` > `Logs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for the crash.

## References

Predecessors:

- [GuitarSniffer repository](https://github.com/artman41/guitarsniffer)
- [DrumSniffer repository](https://github.com/Dunkalunk/guitarsniffer)

Packet data:

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)
- [rb4.app's Javascript source](https://rb4.app/js/app.js)
- Original research, found in the [PlasticBand documentation repository](https://github.com/TheNathannator/PlasticBand).

## Building

To build this program, you will need:

- Visual Studio, or MSBuild/[the .NET SDK](https://dotnet.microsoft.com/en-us/download) + your code editor of choice.
- [WiX Toolset v4](https://wixtoolset.org/) if you wish to build the installer.

## License

This program is licensed under the MIT license. See [LICENSE](LICENSE) for details.
