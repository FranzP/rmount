using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Rmount
{
    /// <summary>
    /// Manages Windows autostart functionality
    /// </summary>
    public static class AutoStartManager
    {
        private const string REGISTRY_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "Rmount";

        /// <summary>
        /// Check if autostart is enabled
        /// </summary>
        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, false))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(APP_NAME);
                        return value != null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking autostart: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Enable autostart
        /// </summary>
        public static bool Enable()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                
                // Replace .dll with .exe if running from .NET Core/5+
                if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    exePath = exePath.Substring(0, exePath.Length - 4) + ".exe";
                }

                // For .NET Framework, use the executable path
                if (!File.Exists(exePath))
                {
                    exePath = Application.ExecutablePath;
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true))
                {
                    if (key != null)
                    {
                        key.SetValue(APP_NAME, $"\"{exePath}\"");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling autostart: {ex.Message}");
                MessageBox.Show(
                    $"Could not enable autostart.\n\nError: {ex.Message}",
                    "Autostart Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            return false;
        }

        /// <summary>
        /// Disable autostart
        /// </summary>
        public static bool Disable()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(APP_NAME, false);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling autostart: {ex.Message}");
                MessageBox.Show(
                    $"Could not disable autostart.\n\nError: {ex.Message}",
                    "Autostart Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            return false;
        }

        /// <summary>
        /// Apply autostart setting from configuration
        /// </summary>
        public static void ApplyFromConfig(AppConfig config)
        {
            bool currentlyEnabled = IsEnabled();

            if (config.AutoStart && !currentlyEnabled)
            {
                Enable();
            }
            else if (!config.AutoStart && currentlyEnabled)
            {
                Disable();
            }
        }
    }
}
