using System;
using System.Collections.Generic;

namespace Rmount
{
    /// <summary>
    /// Manages saved mount settings stored in rclone.conf
    /// </summary>
    public class AppSettings
    {
        // Variables with _ prefix to avoid conflicts
        private const string DRIVE_LETTER_KEY = "_rmount_drive_letter";
        private const string AUTO_MOUNT_KEY = "_rmount_auto_mount";
        private const string MOUNT_OPTIONS_KEY = "_rmount_mount_options";

        public List<SavedMount> SavedMounts { get; set; }

        public AppSettings()
        {
            SavedMounts = new List<SavedMount>();
        }

        /// <summary>
        /// Load settings from rclone.conf
        /// </summary>
        public static AppSettings Load()
        {
            var settings = new AppSettings();
            var remotes = RcloneConfigParser.GetRemoteNames();

            foreach (var remote in remotes)
            {
                // Read saved values from rclone.conf
                string driveLetter = RcloneConfigParser.GetRemoteProperty(remote, DRIVE_LETTER_KEY);
                string autoMountStr = RcloneConfigParser.GetRemoteProperty(remote, AUTO_MOUNT_KEY);
                string mountOptions = RcloneConfigParser.GetRemoteProperty(remote, MOUNT_OPTIONS_KEY);

                if (!string.IsNullOrEmpty(driveLetter))
                {
                    bool autoMount = false;
                    if (!string.IsNullOrEmpty(autoMountStr))
                    {
                        bool.TryParse(autoMountStr, out autoMount);
                    }

                    settings.SavedMounts.Add(new SavedMount
                    {
                        RemoteName = remote,
                        DriveLetter = driveLetter,
                        AutoMount = autoMount,
                        MountOptions = mountOptions ?? ""
                    });
                }
            }

            return settings;
        }

        /// <summary>
        /// Save settings (settings are saved directly in rclone.conf by AddOrUpdateMount)
        /// </summary>
        public void Save()
        {
            // Settings are saved directly in rclone.conf
            // Nothing to do here as AddOrUpdateMount saves directly
        }

        /// <summary>
        /// Add or update a mount setting
        /// </summary>
        public void AddOrUpdateMount(string remoteName, string driveLetter, bool autoMount, string mountOptions = "")
        {
            var existing = SavedMounts.Find(m => m.RemoteName == remoteName);
            if (existing != null)
            {
                existing.DriveLetter = driveLetter;
                existing.AutoMount = autoMount;
                existing.MountOptions = mountOptions;
            }
            else
            {
                SavedMounts.Add(new SavedMount
                {
                    RemoteName = remoteName,
                    DriveLetter = driveLetter,
                    AutoMount = autoMount,
                    MountOptions = mountOptions
                });
            }

            // Save directly in rclone.conf with _ prefix
            RcloneConfigParser.SetRemoteProperty(remoteName, DRIVE_LETTER_KEY, driveLetter);
            RcloneConfigParser.SetRemoteProperty(remoteName, AUTO_MOUNT_KEY, autoMount.ToString());
            
            if (!string.IsNullOrEmpty(mountOptions))
            {
                RcloneConfigParser.SetRemoteProperty(remoteName, MOUNT_OPTIONS_KEY, mountOptions);
            }
            else
            {
                RcloneConfigParser.RemoveRemoteProperty(remoteName, MOUNT_OPTIONS_KEY);
            }
        }

        /// <summary>
        /// Get saved mount for a remote
        /// </summary>
        public SavedMount GetSavedMount(string remoteName)
        {
            return SavedMounts.Find(m => m.RemoteName == remoteName);
        }

        /// <summary>
        /// Remove mount settings for a remote
        /// </summary>
        public void RemoveMount(string remoteName)
        {
            SavedMounts.RemoveAll(m => m.RemoteName == remoteName);

            // Remove from rclone.conf
            RcloneConfigParser.RemoveRemoteProperty(remoteName, DRIVE_LETTER_KEY);
            RcloneConfigParser.RemoveRemoteProperty(remoteName, AUTO_MOUNT_KEY);
            RcloneConfigParser.RemoveRemoteProperty(remoteName, MOUNT_OPTIONS_KEY);
        }
    }

    /// <summary>
    /// Represents a saved mount configuration
    /// </summary>
    [Serializable]
    public class SavedMount
    {
        public string RemoteName { get; set; }
        public string DriveLetter { get; set; }
        public bool AutoMount { get; set; }
        public string MountOptions { get; set; }
    }
}
