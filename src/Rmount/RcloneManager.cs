using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rmount
{
    /// <summary>
    /// Manager for rclone mount/unmount operations
    /// </summary>
    public class RcloneManager : IDisposable
    {
        private Dictionary<string, Process> mountedDrives = new Dictionary<string, Process>();
        private string rclonePath;
        private static AppConfig _config;
        private bool disposed = false;

        /// <summary>
        /// Initialize the manager with configuration
        /// </summary>
        public static void Initialize(AppConfig config)
        {
            _config = config;
        }

        public RcloneManager()
        {
            if (_config != null)
            {
                rclonePath = _config.Rclone;
            }
            else
            {
                // Fallback: try to find rclone.exe
                rclonePath = FindRcloneExecutable();
            }
        }

        /// <summary>
        /// Try to find rclone executable
        /// </summary>
        private string FindRcloneExecutable()
        {
            // Check if rclone is in PATH
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                string[] pathDirs = pathEnv.Split(';');
                foreach (string dir in pathDirs)
                {
                    string fullPath = Path.Combine(dir.Trim(), "rclone.exe");
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            // Check in program directory
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rclone.exe");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            // Fallback to PATH
            return "rclone.exe";
        }

        /// <summary>
        /// Check if rclone is available
        /// </summary>
        public bool IsRcloneAvailable()
        {
            Process process = null;
            try
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = rclonePath,
                        Arguments = "version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                bool exited = process.WaitForExit(5000);
                return exited && process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// Mount a remote as a drive letter
        /// </summary>
        public bool MountRemote(string remoteName, string driveLetter, string additionalOptions = "")
        {
            if (mountedDrives.ContainsKey(remoteName))
            {
                return false; // Already mounted
            }

            try
            {
                // Build arguments with custom options
                string arguments = $"mount {remoteName}: {driveLetter}: --vfs-cache-mode full";
                
                if (!string.IsNullOrWhiteSpace(additionalOptions))
                {
                    arguments += " " + additionalOptions.Trim();
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = rclonePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                mountedDrives[remoteName] = process;
                
                // Short delay to ensure mount is successful
                System.Threading.Thread.Sleep(1000);

                return !process.HasExited;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mounting {remoteName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unmount a remote
        /// </summary>
        public void UnmountRemote(string remoteName)
        {
            if (!mountedDrives.ContainsKey(remoteName))
            {
                return;
            }

            try
            {
                Process process = mountedDrives[remoteName];
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                process.Dispose();
                mountedDrives.Remove(remoteName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unmounting {remoteName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a remote is currently mounted
        /// </summary>
        public bool IsMounted(string remoteName)
        {
            if (!mountedDrives.ContainsKey(remoteName))
            {
                return false;
            }

            Process process = mountedDrives[remoteName];
            return !process.HasExited;
        }

        /// <summary>
        /// Unmount all mounted remotes
        /// </summary>
        public void UnmountAll()
        {
            foreach (var remoteName in mountedDrives.Keys.ToList())
            {
                UnmountRemote(remoteName);
            }
        }

        /// <summary>
        /// Get a list of available drive letters
        /// </summary>
        public List<string> GetAvailableDriveLetters()
        {
            var availableLetters = new List<string>();
            var usedDrives = Directory.GetLogicalDrives().Select(d => d[0]).ToList();

            for (char letter = 'D'; letter <= 'Z'; letter++)
            {
                if (!usedDrives.Contains(letter))
                {
                    availableLetters.Add(letter.ToString());
                }
            }

            return availableLetters;
        }

        /// <summary>
        /// Dispose of all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Unmount all drives and dispose processes
                    UnmountAll();
                }
                disposed = true;
            }
        }
    }
}
