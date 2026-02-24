using System;
using System.Windows.Forms;

namespace Rmount
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load configuration
            AppConfig config = AppConfig.Load();

            // Check if configuration is valid
            if (!config.IsValid())
            {
                // Show setup dialog
                using (SetupDialog setupDialog = new SetupDialog())
                {
                    if (setupDialog.ShowDialog() != DialogResult.OK)
                    {
                        // User cancelled setup
                        MessageBox.Show(
                            "Application cannot start without proper configuration.",
                            "Setup Required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        return;
                    }

                    // Save configuration
                    config.Rclone = setupDialog.RcloneExePath;
                    config.Config = setupDialog.RcloneConfigPath;
                    config.Save();
                }

                // Validate again after setup
                if (!config.IsValid())
                {
                    MessageBox.Show(
                        "The configuration is still invalid. Please check your settings.",
                        "Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
            }

            // Initialize managers with configuration
            RcloneManager.Initialize(config);
            RcloneConfigParser.Initialize(config);

            // Apply autostart setting from config
            AutoStartManager.ApplyFromConfig(config);

            // Run application
            Application.Run(new TrayApplicationContext(config));
        }
    }
}
