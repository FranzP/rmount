using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Rmount
{
    /// <summary>
    /// Application context for the system tray application
    /// </summary>
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private RcloneManager rcloneManager;
        private Dictionary<string, string> mountedRemotes; // Remote -> Drive Letter
        private AppSettings settings;
        private AppConfig config;
        private bool disposed = false;

        public TrayApplicationContext(AppConfig appConfig)
        {
            config = appConfig;
            mountedRemotes = new Dictionary<string, string>();
            rcloneManager = new RcloneManager();
            settings = AppSettings.Load();

            // Create tray icon
            trayIcon = new NotifyIcon()
            {
                Icon = LoadIcon(),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "Rclone Tray Manager"
            };

            // Check if rclone is available
            if (!rcloneManager.IsRcloneAvailable())
            {
                MessageBox.Show(
                    "rclone.exe was not found!\n\n" +
                    "Please make sure rclone is installed and accessible.",
                    "Rclone Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            // Check for WinFsp
            WinFspChecker.CheckAndPrompt();

            BuildContextMenu();
            
            // Auto-mount saved connections
            AutoMountSavedConnections();
        }

        private Icon LoadIcon()
        {
            try
            {
                // Load the embedded icon from assembly resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Rmount.rclone.ico";
                
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading embedded icon: {ex.Message}");
            }

            // Fallback to system icon
            return SystemIcons.Application;
        }

        private void AutoMountSavedConnections()
        {
            foreach (var savedMount in settings.SavedMounts)
            {
                if (savedMount.AutoMount)
                {
                    // Check if drive letter is available
                    var availableDrives = rcloneManager.GetAvailableDriveLetters();
                    if (availableDrives.Contains(savedMount.DriveLetter))
                    {
                        MountRemote(savedMount.RemoteName, savedMount.DriveLetter, savedMount.MountOptions, false);
                    }
                }
            }
        }

        private void BuildContextMenu()
        {
            var contextMenu = trayIcon.ContextMenuStrip;
            contextMenu.Items.Clear();

            // Get remotes with error handling
            List<string> remotes;
            try
            {
                remotes = RcloneConfigParser.GetRemoteNames();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error reading rclone configuration:\n{ex.Message}",
                    "Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                remotes = new List<string>();
            }

            if (remotes.Count == 0)
            {
                contextMenu.Items.Add(new ToolStripMenuItem("No remotes configured") { Enabled = false });
            }
            else
            {
                // Create menu item for each remote
                foreach (string remote in remotes)
                {
                    var remoteItem = new ToolStripMenuItem(remote);
                    
                    if (rcloneManager.IsMounted(remote))
                    {
                        // Remote is mounted - show unmount option
                        string driveLetter = mountedRemotes.ContainsKey(remote) ? mountedRemotes[remote] : "?";
                        remoteItem.Text = $"{remote} ({driveLetter}:)";
                        
                        // Create bitmap for menu item (will be disposed with menu)
                        using (Icon icon = SystemIcons.Shield)
                        {
                            remoteItem.Image = new Bitmap(icon.ToBitmap());
                        }
                        
                        var unmountItem = new ToolStripMenuItem("Unmount", null, (s, e) => UnmountRemote(remote));
                        remoteItem.DropDownItems.Add(unmountItem);
                    }
                    else
                    {
                        // Remote is not mounted - show mount options
                        var availableDrives = rcloneManager.GetAvailableDriveLetters();
                        var savedMount = settings.GetSavedMount(remote);
                        
                        if (availableDrives.Count == 0)
                        {
                            var noDrivesItem = new ToolStripMenuItem("No free drive letters") { Enabled = false };
                            remoteItem.DropDownItems.Add(noDrivesItem);
                        }
                        else
                        {
                            // Show ALL available drive letters
                            foreach (string driveLetter in availableDrives)
                            {
                                string displayText = $"Mount as {driveLetter}:";
                                
                                // Mark the saved drive letter
                                if (savedMount != null && savedMount.DriveLetter == driveLetter)
                                {
                                    displayText += savedMount.AutoMount ? " [AutoMount]" : " [Saved]";
                                }
                                
                                var driveItem = new ToolStripMenuItem(
                                    displayText,
                                    null,
                                    (s, e) => MountRemote(remote, driveLetter)
                                );
                                remoteItem.DropDownItems.Add(driveItem);
                            }
                            
                            // Separator and settings
                            if (availableDrives.Count > 0)
                            {
                                remoteItem.DropDownItems.Add(new ToolStripSeparator());
                                
                                // Option to save/change default drive
                                var settingsItem = new ToolStripMenuItem("Settings...", null, (s, e) => ShowMountSettings(remote));
                                remoteItem.DropDownItems.Add(settingsItem);
                            }
                        }
                    }

                    contextMenu.Items.Add(remoteItem);
                }

                contextMenu.Items.Add(new ToolStripSeparator());

                // Unmount All Option
                var unmountAllItem = new ToolStripMenuItem("Unmount All", null, (s, e) => UnmountAll());
                unmountAllItem.Enabled = mountedRemotes.Count > 0;
                contextMenu.Items.Add(unmountAllItem);
            }

            contextMenu.Items.Add(new ToolStripSeparator());

            // Refresh Option
            contextMenu.Items.Add(new ToolStripMenuItem("Refresh", null, (s, e) => BuildContextMenu()));

            contextMenu.Items.Add(new ToolStripSeparator());

            // Save Config Option
            contextMenu.Items.Add(new ToolStripMenuItem("Save Configuration", null, (s, e) => SaveConfiguration()));

            contextMenu.Items.Add(new ToolStripSeparator());

            // Exit Option
            contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, Exit));
        }

        private void MountRemote(string remoteName, string driveLetter, string mountOptions = "", bool showNotification = true)
        {
            if (rcloneManager.MountRemote(remoteName, driveLetter, mountOptions))
            {
                mountedRemotes[remoteName] = driveLetter;
                if (showNotification)
                {
                    trayIcon.ShowBalloonTip(
                        2000,
                        "Rclone Mount",
                        $"{remoteName} was mounted as {driveLetter}:",
                        ToolTipIcon.Info
                    );
                }
                BuildContextMenu();
            }
            else
            {
                MessageBox.Show(
                    $"Error mounting {remoteName} as {driveLetter}:",
                    "Mount Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ShowMountSettings(string remoteName)
        {
            var savedMount = settings.GetSavedMount(remoteName);
            var availableDrives = rcloneManager.GetAvailableDriveLetters();
            
            if (availableDrives.Count == 0)
            {
                MessageBox.Show(
                    "No free drive letters available.",
                    "No Drives",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            // Simple selection dialog
            Form settingsForm = new Form
            {
                Text = $"Settings for {remoteName}",
                Width = 400,
                Height = 280,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblDrive = new Label
            {
                Text = "Drive letter:",
                Left = 20,
                Top = 20,
                Width = 150
            };

            ComboBox cbDrive = new ComboBox
            {
                Left = 20,
                Top = 45,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            foreach (string drive in availableDrives)
            {
                cbDrive.Items.Add(drive);
            }
            
            if (savedMount != null && availableDrives.Contains(savedMount.DriveLetter))
            {
                cbDrive.SelectedItem = savedMount.DriveLetter;
            }
            else if (cbDrive.Items.Count > 0)
            {
                cbDrive.SelectedIndex = 0;
            }

            CheckBox chkAutoMount = new CheckBox
            {
                Text = "Auto-mount on startup",
                Left = 20,
                Top = 80,
                Width = 300
            };
            
            if (savedMount != null)
            {
                chkAutoMount.Checked = savedMount.AutoMount;
            }

            Label lblMountOptions = new Label
            {
                Text = "Mount options (optional):",
                Left = 20,
                Top = 110,
                Width = 350
            };

            TextBox txtMountOptions = new TextBox
            {
                Left = 20,
                Top = 135,
                Width = 340
            };

            if (savedMount != null && !string.IsNullOrEmpty(savedMount.MountOptions))
            {
                txtMountOptions.Text = savedMount.MountOptions;
            }

            Button btnSave = new Button
            {
                Text = "Save",
                Left = 180,
                Top = 175,
                Width = 80,
                DialogResult = DialogResult.OK
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Left = 280,
                Top = 175,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            settingsForm.Controls.Add(lblDrive);
            settingsForm.Controls.Add(cbDrive);
            settingsForm.Controls.Add(chkAutoMount);
            settingsForm.Controls.Add(lblMountOptions);
            settingsForm.Controls.Add(txtMountOptions);
            settingsForm.Controls.Add(btnSave);
            settingsForm.Controls.Add(btnCancel);

            settingsForm.AcceptButton = btnSave;
            settingsForm.CancelButton = btnCancel;

            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                if (cbDrive.SelectedItem != null)
                {
                    string selectedDrive = cbDrive.SelectedItem.ToString();
                    string mountOptions = txtMountOptions.Text.Trim();
                    settings.AddOrUpdateMount(remoteName, selectedDrive, chkAutoMount.Checked, mountOptions);
                    settings.Save();
                    
                    string optionsDisplay = string.IsNullOrEmpty(mountOptions) ? "None" : mountOptions;
                    MessageBox.Show(
                        $"Settings for {remoteName} saved.\n\n" +
                        $"Drive: {selectedDrive}:\n" +
                        $"AutoMount: {(chkAutoMount.Checked ? "Yes" : "No")}\n" +
                        $"Mount Options: {optionsDisplay}",
                        "Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    
                    BuildContextMenu();
                }
            }

            settingsForm.Dispose();
        }

        private void UnmountRemote(string remoteName)
        {
            rcloneManager.UnmountRemote(remoteName);
            if (mountedRemotes.ContainsKey(remoteName))
            {
                string driveLetter = mountedRemotes[remoteName];
                mountedRemotes.Remove(remoteName);
                trayIcon.ShowBalloonTip(
                    2000,
                    "Rclone Unmount",
                    $"{remoteName} ({driveLetter}:) was unmounted",
                    ToolTipIcon.Info
                );
            }
            BuildContextMenu();
        }

        private void UnmountAll()
        {
            rcloneManager.UnmountAll();
            mountedRemotes.Clear();
            trayIcon.ShowBalloonTip(
                2000,
                "Rclone Unmount",
                "All mounts have been removed",
                ToolTipIcon.Info
            );
            BuildContextMenu();
        }

        private void SaveConfiguration()
        {
            try
            {
                // Save the current application configuration
                config.Save();
                
                trayIcon.ShowBalloonTip(
                    2000,
                    "Configuration Saved",
                    "Application configuration has been saved successfully.",
                    ToolTipIcon.Info
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving configuration:\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            // Remove all mounts before exiting
            rcloneManager.UnmountAll();
            
            // Hide tray icon
            trayIcon.Visible = false;
            
            // Dispose resources
            Dispose();
            
            // Exit application
            Application.Exit();
        }

        /// <summary>
        /// Dispose of all resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    rcloneManager?.Dispose();
                    trayIcon?.Dispose();
                }
                disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
