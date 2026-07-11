<h1 align="center">
  CPDLC Plugin
</h1>

<h3 align="center">
  A vatSys plugin enabling support for CPDLC via a relay server.

  [![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/yukitsune/CPDLCPlugin/build.yml?branch=main)](https://github.com/YuKitsune/CPDLCPlugin/actions/workflows/build.yml)
  [![License](https://img.shields.io/github/license/YuKitsune/CPDLCPlugin)](https://github.com/YuKitsune/CPDLCPlugin/blob/main/LICENSE)
  [![Latest Release](https://img.shields.io/github/v/release/YuKitsune/CPDLCPlugin?include_prereleases)](https://github.com/YuKitsune/CPDLCPlugin/releases)

  <img src="./README.png" />
</h3>

## Overview

This plugin enables CPDLC (Controller-Pilot Data Link Communications) functionality in vatSys, allowing controllers to send and receive datalink messages with aircraft on the VATSIM network.

> [!NOTE]
> This plugin requires a CPDLC Server to function. For information on how the system works and how to configure the server, see the [Server Documentation](https://cpdlc.eoinmotherway.dev).

## Server Deployment

Deploy your own CPDLC Server instance:

[![Deploy to DigitalOcean](https://www.deploytodo.com/do-btn-blue.svg)](https://cloud.digitalocean.com/apps/new?repo=https://github.com/YuKitsune/CPDLCPlugin/tree/main)

After deployment, configure the following environment variables:
- `Acars__0__StationIdentifier`: Your station identifier (e.g., YZZZ)
- `Acars__0__AuthenticationCode`: Your Hoppie ACARS authentication code

## Installation

Before installing the CPDLC Plugin, ensure you have the following:

- [vatSys](https://virtualairtrafficsystem.com/) (version 1.4.20 or later)
- .NET Framework 4.7.2 or later

### Installing from GitHub

1. Download the [latest release from GitHub](https://github.com/YuKitsune/CPDLCPlugin/releases).
2. Extract the `CPDLCPlugin.zip` file into your vatSys plugins directory (`Documents\vatSys Files\Profiles\<Profile Name>\Plugins\CPDLCPlugin`).
3. Run the `unblock-dlls.bat` helper script (included in the `CPDLCPlugin.zip` file) to unblock all the `.dll` files.

### Verifying Installation

1. Open vatSys.
2. Look for the `CPDLC` menu item in the vatSys menu bar.

> [!TIP]
> If you do not see the `CPDLC` menu item after restarting vatSys, refer to the [Troubleshooting](#troubleshooting) section below.

### Configuring Labels and Strips

The plugin provides custom label and strip items that must be added to your profile's `Labels.xml` and `Strips.xml` files.

#### Automatic (recommended)

After launching vatSys with the plugin installed, use the `CPDLC > Install label & strip items` menu option to add the custom label and strip items. Restart vatSys to apply the changes.

To revert, use `CPDLC > Uninstall label & strip items`, which restores the original files from a backup.

If your `Labels.xml` or `Strips.xml` files have been customised, the automatic installer will fail. Apply the changes manually using the diffs below.

> [!NOTE]
> When bundling the plugin in a vatSys profile, you can hide the install and uninstall menu items by setting `ShowInstallationMenuItems` to `false` in `CPDLC.json`.

#### Manual

See [Labels.xml.diff](Labels.xml.diff) and [Strips.xml.diff](Strips.xml.diff) for complete examples.

##### Labels.xml

Replace each occurrence of:
```xml
<Item Type="LABEL_ITEM_CPDLC" Colour="" BackgroundColour="CPDLCDownlink" LeftClick="Label_CPDLC_Menu" MiddleClick="Label_CPDLC_Message_Toggle" RightClick="Label_CPDLC_Editor" />
```

With:
```xml
<!-- CPDLC Plugin: CPDLC Status -->
<Item Type="CPDLCPLUGIN_CPDLCSTATUS" />
<Item Type="CPDLCPLUGIN_CPDLCSTATUS_BG" BackgroundColour="Custom" />

<!-- CPDLC Plugin: Text Status -->
<Item Type="CPDLCPLUGIN_TEXTSTATUS" />
<Item Type="CPDLCPLUGIN_TEXTSTATUS_BG" BackgroundColour="Custom" />
```

##### Strips.xml

Replace:
```xml
<StripItem Type="CPDLCStatus" LeftClick="Label_CPDLC_Editor" MinLength="1" />
```

With:
```xml
<!-- CPDLC Plugin: CPDLC Status -->
<StripItem Type="CPDLCPLUGIN_CPDLCSTATUS" MinLength="1" />

<!-- CPDLC Plugin: Text Status -->
<StripItem Type="CPDLCPLUGIN_TEXTSTATUS" />
```

## Troubleshooting

### CPDLC menu item not appearing

If the CPDLC menu item does not appear, it's likely that the `.dll` files for the plugin have been blocked by Windows.
This is a security feature in Windows that blocks files downloaded from the internet to protect your computer from potentially harmful software.

1. Locate the `unblock-dlls.bat` file (included in the `CPDLCPlugin.zip` file).
2. Ensure the file is located in the same folder as the `.dll` files, or in one of the folders above it.
3. Run the script by double-clicking it. You will be shown a list of all the `.dll` files the script will unblock. Press `Y` to continue, or `N` to exit.
4. Restart vatSys once the script has completed.

This script will search for any `.dll` files in the current folder or sub-folders and ensure they are unblocked.

### DPI Awareness

If you are using a high-resolution display (4K monitor, high-DPI laptop screen, etc.) and experience graphical issues after launching vatSys, you may need to run the `dpiawarefix.bat` script.

1. Locate the `dpiawarefix.bat` file (included in the `CPDLCPlugin.zip` file).
2. Run the script by double-clicking it.
3. Restart vatSys.

This script adjusts Windows DPI settings for vatSys, making it compatible with high-resolution displays.
