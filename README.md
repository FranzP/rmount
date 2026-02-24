# Rmount - Rclone Mount Manager

A tray application for managing rclone mounts.

> **Note**: This app was vibe coded with [opencode](https://opencode.ai/)

## Features

- **Portable-first**: Works fully portable when rclone.exe and rclone.conf are in the same folder
- Runs as System Tray Icon
- Right-click on the icon shows all configured rclone remotes
- Mount rclone remotes as Windows drives
- **AutoMount function**: Saved connections are automatically mounted on startup
- **Custom mount options**: Configure per-remote mount options (read-only, cache settings, etc.)
- **Windows autostart**: Optionally start with Windows (controlled via INI file)
- **WinFsp detection**: Automatically checks for WinFsp and offers download if not installed
- Unmount mounted drives (individually or all)
- Automatic unmount when exiting the application
- **Settings are stored directly in rclone.conf** (variables with `_` prefix to avoid conflicts)

## Prerequisites

- rclone.exe (portable or installed)
- WinFsp (will be prompted to download if not installed)
- Rclone configuration (optional - can be configured later)

## Configuration File Locations

The application searches for files in the following order (portable-first):

### rclone.exe
1. Program directory (same folder as Rmount.exe)
2. System PATH

### rclone.conf
1. Program directory (same folder as Rmount.exe)
2. `C:\Users\<USERNAME>\.config\rclone\rclone.conf`
3. `%APPDATA%\rclone\rclone.conf`

This allows the application to work in fully portable mode when both files are in the program directory.

## Configuration

### Application Settings (Rmount.ini)

The application reads settings from `Rmount.ini` (located next to the executable):

```ini
# Path to rclone.exe (can be absolute or relative)
Rclone=rclone.exe

# Path to rclone.conf configuration file
Config=rclone.conf

# Enable autostart with Windows (true/false)
AutoStart=true
```

### Mount Settings (rclone.conf)

Per-remote settings are stored directly in rclone.conf with the following variables:
- `_rmount_drive_letter` - Saved drive letter
- `_rmount_auto_mount` - AutoMount enabled (true/false)
- `_rmount_mount_options` - Additional mount options (optional)
- Variables begin with `_` to avoid conflicts with rclone properties

### Example rclone.conf with saved settings

```ini
[myremote]
_rmount_drive_letter = Z
_rmount_auto_mount = true
_rmount_mount_options = --read-only --vfs-cache-max-size 2G
```

## Notes

- The application uses `rclone mount` with `--vfs-cache-mode full` for better performance
