# RB4InstrumentMapper

A program that maps packets from Xbox One instrument peripherals to virtual controllers, for use in games such as [Clone Hero](https://clonehero.net/).

![RB4InstrumentMapper main menu](Docs/Images/Readme/program-screenshot.png "RB4InstrumentMapper main menu")

All Xbox One instruments are supported (RB4 guitars/drums, GHL guitar), along with the RB4 wireless legacy adapter.

## Table of Contents

- [Installation](#installation)
   - [ViGEmBus Setup](#vigembus-setup)
   - [vJoy Setup](#vjoy-setup)
- [Using Rock Band 4 Wireless Guitars and Drums](#using-rock-band-4-wireless-guitars-and-drums)
   - [Setup](#setup)
   - [Jaguar Guitar Firmware Update](#jaguar-guitar-firmware-update)
   - [Usage](#usage)
   - [Having Sync Issues?](#having-sync-issues)
- [Using Riffmasters, Guitar Hero Live Guitars, and RB4 Wireless Legacy Adapters](#using-riffmasters-guitar-hero-live-guitars-and-rb4-wireless-legacy-adapters)
   - [Setup](#setup-1)
   - [Usage](#usage-1)
- [Mapping your Controls](#mapping-your-controls)
   - [Clone Hero](#clone-hero)
   - [YARG](#yarg)
- [Packet Logs](#packet-logs)
- [Error Logs](#error-logs)
- [Building](#building)
- [References](#references)
- [License](#license)

## Installation

You will need:

- Windows 10/11 64-bit
- At least one of the following:
  - [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest)
  - [vJoy](https://github.com/jshafer817/vJoy/releases/latest)

ViGEmBus is recommended, as it is significantly easier to use and requires no configuration. vJoy is supported as an alternative on the off-chance you run into issues with ViGEmBus. Both can be installed simultaneously if desired, however RB4InstrumentMapper will only use one of them at a time.

### ViGEmBus Setup

1. Download and install [ViGEmBus](https://github.com/ViGEm/ViGEmBus/releases/latest).
2. That's all.

### vJoy Setup

1. Download and install [vJoy](https://github.com/jshafer817/vJoy/releases/latest).
2. After installing, open your Start menu, find the `vJoy` folder, and open the `Configure vJoy` program inside it.
3. Configure one device for each one of your controllers, using these settings:
   - Number of Buttons: 16
   - POV Hat Switch: Continuous, POVs: 1
   - Axes: `X`, `Y`, `Z`

   ![vJoy configuration screenshot](Docs/Images/Readme/vjoy-configuration.png "vJoy configuration screenshot")

4. Click Apply.

---

## Using Rock Band 4 Wireless Guitars and Drums

![Rock Band 4 Stratocaster](Docs/Images/Peripherals/rock-band-4-stratocaster.png "Rock Band 4 Stratocaster")
![Rock Band 4 Jaguar](Docs/Images/Peripherals/rock-band-4-jaguar.png "Rock Band 4 Jaguar")
![Rock Band 4 drumkit](Docs/Images/Peripherals/rock-band-4-drums.png "Rock Band 4 drumkit")

*Note that RB4InstrumentMapper is NOT required to use guitars in Fortnite!*

*For Riffmasters, please refer to [this section](#using-riffmasters-guitar-hero-live-guitars-and-rb4-wireless-legacy-adapters).*

### Setup

For wireless Rock Band 4 guitars and drums, you will need an Xbox One wireless receiver. Both versions of the official one are shown here.

![Xbox One receiver, original](Docs/Images/Peripherals/xbox-one-receiver-gen-1.png "Xbox One receiver, original")
![Xbox One receiver, revision](Docs/Images/Peripherals/xbox-one-receiver-gen-2.png "Xbox One receiver, revision")

- This is *not* the same as an Xbox 360 wireless receiver! You must get an Xbox One (or just "Xbox") receiver, as shown above.
- Third-party receivers are untested, and they will not be deliberately supported.

You will also need to install [WinPCap](https://www.winpcap.org/install/bin/WinPcap_4_1_3.exe). This is used to read inputs from the Xbox One receiver.

#### Jaguar Guitar Firmware Update

![Rock Band 4 Jaguar](Docs/Images/Peripherals/rock-band-4-jaguar-small.png "Rock Band 4 Jaguar")

Jaguar guitars require a firmware update in order to connect to Xbox One receivers.

- [Instructions](https://bit.ly/2UHzonU)
- [Clone Hero wiki re-host](https://wiki.clonehero.net/link/61), in case the above link goes down again.

### Usage

1. Check the `Enable` checkbox under the `Pcap` group.

   ![Pcap enable checkbox](Docs/Images/Readme/pcap-checkbox.png)

2. Xbox One receivers should be detected under the device dropdown automatically. If they are not, click the `Auto-Detect Pcap` button and follow its instructions.
   - If needed, the receiver can also be selected manually. However, this is not recommended, as it is often difficult to identify the correct device to use. Xbox One receivers have either no device name (and display only the device path), or a name of `MT7612US_RL` (displays the device path afterwards in parentheses).

     ![Pcap device selection](Docs/Images/Readme/pcap-dropdown.png)

   - KNOWN ISSUE: On some systems, the receiver will not be picked up whatsoever. This issue is out of RB4InstrumentMapper's control, and I unfortunately don't have any clue on what the cause is. If the auto-detect process doesn't pick up anything, you're most likely afflicted by this issue.
3. **Do not connect your instruments yet!** They may not function correctly otherwise.
4. In the `Controller Emulation Mode` dropdown, select the controller emulation mode you want to use.

   ![Controller emulation mode selection](Docs/Images/Readme/controller-emulation-mode.png)

5. Hit the `Start` button to begin reading inputs.
6. Now you may connect your instruments. They will be picked up and read automatically until you hit `Stop` or close the program.
7. [Map your controls in the game you'll be playing](#mapping-your-controls).

#### Having Sync Issues?

Some guitars/drumkits might not sync properly when using just the sync button. This includes the PDP drumkit and occasionally the Jaguar guitar. Follow these steps to sync your device correctly:

1. Go to Windows settings > Devices > Bluetooth & other devices
2. Click `Add Bluetooth or other device` and pick the `Everything else` option.
3. Press and hold the sync button until the Xbox button light flashes quickly.
4. Select `Xbox compatible game controller` from the list once it appears.
5. If that doesn't work, restart your PC and try again.

---

## Using Riffmasters, Guitar Hero Live Guitars, and RB4 Wireless Legacy Adapters

![Riffmaster guitar](Docs/Images/Peripherals/riffmaster.png "Riffmaster guitar")
![Guitar Hero Live guitar](Docs/Images/Peripherals/guitar-hero-live-guitar.png "Guitar Hero Live guitar")
![Rock Band 4 wireless legacy adapter](Docs/Images/Peripherals/rock-band-4-wireless-legacy.png "Rock Band 4 wireless legacy adapter")

*Note that RB4InstrumentMapper is NOT required to use the Riffmaster in Fortnite!*

### Setup

You will need to install the WinUSB driver onto the Riffmaster dongle, Guitar Hero Live dongle, or Rock Band 4 wireless legacy adapter before using it. RB4InstrumentMapper is capable of doing this directly, through the `Configure Devices` button on its main menu:

1. Check the `Enable` checkbox under the `USB` group, then click the `Configure Devices` button underneath it.

   ![USB enable checkbox](Docs/Images/Readme/usb-checkbox.png)

2. Find the device you want to use on the left side of the menu.

   ![WinUSB configuration, left side](Docs/Images/Readme/usb-configure-left.png)

3. Click the `Switch Driver` button and wait for it to switch the driver. The device will show up on the right side of the menu once it's done.

   ![WinUSB configuration, right side](Docs/Images/Readme/usb-configure-right.png)

   - Note that games that natively support the device will no longer work directly with it until you uninstall the WinUSB driver, you will have to use RB4InstrumentMapper.
   - If you want to remove this limitation, you can hit the `Revert Driver` button on the device, after which those games will work with it again.
   - After you do this, RB4InstrumentMapper will no longer be able to make it usable in other games until you switch the driver back over.

If you run into any issues with this process, you may try [installing the driver manually](Docs/WinUSB/manual-winusb-install.md). This is not recommended for normal use, it should only be used if the Configure Devices menu is not working.
- This also covers [uninstalling manually](Docs/WinUSB/manual-winusb-install.md#remove-winusb), in case a device gets stuck with the driver installed and RB4InstrumentMapper stops picking it up.

### Usage

1. Check the `Enable` checkbox under the `USB` group.

   ![USB enable checkbox](Docs/Images/Readme/usb-checkbox.png)

2. **Ensure you have [installed WinUSB](#setup-1) on the devices you want to use! They will not be recognized otherwise!**
3. In the `Controller Type` dropdown, select the controller emulation mode you want to use.

   ![Controller emulation mode selection](Docs/Images/Readme/controller-emulation-mode.png)

4. Hit the `Start` button to begin reading inputs.
5. [Map your controls in the game you'll be playing](#mapping-your-controls).

Devices will be detected automatically as they are connected/removed, though they will not be read until you hit Start.

---

## Mapping your Controls

Now that RB4InstrumentMapper is set up and running, map your controls for each instrument in the game you'll be playing.

### Clone Hero

1. Press Space on the main menu.

   !["Press Space For Controls" label](Docs/Images/Readme/clone-hero-press-space.png)

2. Click Assign Controller and press a button on the device for it to be assigned.

   !["Assign Controller" button](Docs/Images/Readme/clone-hero-assign-controller.png)

3. Click the slots in the `Controller` column to map each of the controls.

   ![Controls bound under the "Controller" column](Docs/Images/Readme/clone-hero-bind-controls.png)

4. Repeat for each connected device.
5. Click `Done`.

### YARG

Refer to the [official documentation](https://docs.yarg.in/en/profiles).

## Packet Logs

RB4InstrumentMapper is capable of logging packets to a file for debugging purposes. To do so, enable both the `Show Packets (for debugging)` and `Log packets to file` checkboxes, then hit Start. Packet logs get saved to a `RB4InstrumentMapper` > `PacketLogs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for packet parsing issues.

Note that these settings are meant for debugging purposes only, leaving them enabled can reduce the performance of the program somewhat.

## Error Logs

In the case that the program crashes, an error log is saved to a `RB4InstrumentMapper` > `Logs` folder inside your Documents folder. Make sure to include it when getting help or creating an issue report for the crash.

## Building

To build this program, you will need:

- Visual Studio, or MSBuild/[the .NET SDK](https://dotnet.microsoft.com/en-us/download) + your code editor of choice.
- [WiX Toolset v4](https://wixtoolset.org/) if you wish to build the installer.

## References

Predecessors:

- [GuitarSniffer repository](https://github.com/artman41/guitarsniffer)
- [DrumSniffer repository](https://github.com/Dunkalunk/guitarsniffer)

Packet data:

- [GuitarSniffer guitar packet logs](https://1drv.ms/f/s!AgQGk0OeTMLwhA-uDO9IQHEHqGhv)
- GuitarSniffer guitar packet spreadsheets: [New](https://docs.google.com/spreadsheets/d/1ITZUvRniGpfS_HV_rBpSwlDdGukc3GC1CeOe7SavQBo/edit?usp=sharing), [Old](https://1drv.ms/x/s!AgQGk0OeTMLwg3GBDXFUC3Erj4Wb)
- [rb4.app's Javascript source](https://rb4.app/js/app.js)
- Original research, found in the [PlasticBand documentation repository](https://github.com/TheNathannator/PlasticBand).

## License

This program is licensed under the MIT license. See [LICENSE](LICENSE) for details.
