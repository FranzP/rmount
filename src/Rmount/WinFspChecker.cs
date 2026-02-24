using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Rmount
{
    /// <summary>
    /// Utility class to check for WinFsp installation
    /// </summary>
    public static class WinFspChecker
    {
        private const string WINFSP_DOWNLOAD_URL = "https://winfsp.dev/rel/";
        private const string WINFSP_REGISTRY_KEY = @"SOFTWARE\WinFsp";
        private const string WINFSP_REGISTRY_KEY_WOW64 = @"SOFTWARE\WOW6432Node\WinFsp";

        /// <summary>
        /// Check if WinFsp is installed on the system
        /// </summary>
        public static bool IsInstalled()
        {
            // Check registry (64-bit)
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(WINFSP_REGISTRY_KEY))
            {
                if (key != null)
                {
                    return true;
                }
            }

            // Check registry (32-bit on 64-bit Windows)
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(WINFSP_REGISTRY_KEY_WOW64))
            {
                if (key != null)
                {
                    return true;
                }
            }

            // Check for WinFsp DLL in system directories
            string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string winfspDll = Path.Combine(system32Path, "winfsp-x64.dll");
            if (File.Exists(winfspDll))
            {
                return true;
            }

            // Check 32-bit DLL
            winfspDll = Path.Combine(system32Path, "winfsp-x86.dll");
            if (File.Exists(winfspDll))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Show a dialog prompting the user to download WinFsp
        /// </summary>
        public static void ShowDownloadDialog()
        {
            DialogResult result = MessageBox.Show(
                "WinFsp is not installed on your system.\n\n" +
                "WinFsp is required for rclone to mount cloud storage as drives on Windows.\n\n" +
                "Would you like to download WinFsp now?",
                "WinFsp Not Found",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(WINFSP_DOWNLOAD_URL);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not open download page.\n\n" +
                        $"Please visit manually: {WINFSP_DOWNLOAD_URL}\n\n" +
                        $"Error: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        /// <summary>
        /// Check for WinFsp and show download dialog if not found
        /// Returns true if WinFsp is installed or user chose to continue anyway
        /// </summary>
        public static bool CheckAndPrompt()
        {
            if (IsInstalled())
            {
                return true;
            }

            ShowDownloadDialog();
            
            // Return false to indicate WinFsp is not installed
            // The application can continue running, but mounts will fail
            return false;
        }
    }
}
