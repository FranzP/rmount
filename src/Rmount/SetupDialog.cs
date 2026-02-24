using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Rmount
{
    /// <summary>
    /// Setup dialog for initial configuration
    /// </summary>
    public class SetupDialog : Form
    {
        private TextBox txtRclonePath;
        private TextBox txtConfigPath;
        private Button btnBrowseRclone;
        private Button btnBrowseConfig;
        private Button btnOk;
        private Button btnCancel;
        private Label lblInfo;

        public string RcloneExePath { get; private set; }
        public string RcloneConfigPath { get; private set; }

        public SetupDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Rclone Tray App - Setup";
            this.Width = 550;
            this.Height = 280;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Info label
            lblInfo = new Label
            {
                Text = "Please configure the paths to rclone.exe and rclone.conf:\n" +
                       "If you haven't configured rclone yet, run 'rclone config' first.",
                Top = 15,
                Left = 20,
                Width = 500,
                Height = 40
            };

            // Rclone exe path
            Label lblRclone = new Label
            {
                Text = "Path to rclone.exe:",
                Top = 70,
                Left = 20,
                Width = 150
            };

            txtRclonePath = new TextBox
            {
                Top = 68,
                Left = 180,
                Width = 270,
                Text = "rclone.exe"
            };

            btnBrowseRclone = new Button
            {
                Text = "Browse...",
                Top = 67,
                Left = 460,
                Width = 70
            };
            btnBrowseRclone.Click += BtnBrowseRclone_Click;

            // Config path
            Label lblConfig = new Label
            {
                Text = "Path to rclone.conf:",
                Top = 110,
                Left = 20,
                Width = 150
            };

            txtConfigPath = new TextBox
            {
                Top = 108,
                Left = 180,
                Width = 270,
                Text = GetDefaultConfigPath()
            };

            btnBrowseConfig = new Button
            {
                Text = "Browse...",
                Top = 107,
                Left = 460,
                Width = 70
            };
            btnBrowseConfig.Click += BtnBrowseConfig_Click;

            // Status info
            Label lblStatus = new Label
            {
                Text = "Note: If rclone.exe is in your PATH, you can just enter 'rclone.exe'",
                Top = 150,
                Left = 20,
                Width = 500,
                Height = 40,
                ForeColor = Color.Gray
            };

            // Buttons
            btnOk = new Button
            {
                Text = "OK",
                Top = 200,
                Left = 360,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Top = 200,
                Left = 450,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            // Add controls
            this.Controls.Add(lblInfo);
            this.Controls.Add(lblRclone);
            this.Controls.Add(txtRclonePath);
            this.Controls.Add(btnBrowseRclone);
            this.Controls.Add(lblConfig);
            this.Controls.Add(txtConfigPath);
            this.Controls.Add(btnBrowseConfig);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private string GetDefaultConfigPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "rclone", "rclone.conf");
        }

        private void BtnBrowseRclone_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select rclone.exe";
                ofd.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                ofd.FileName = "rclone.exe";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtRclonePath.Text = ofd.FileName;
                }
            }
        }

        private void BtnBrowseConfig_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select rclone.conf";
                ofd.Filter = "Config Files (*.conf)|*.conf|All Files (*.*)|*.*";
                ofd.FileName = "rclone.conf";
                ofd.CheckFileExists = false; // Allow selecting non-existing file

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtConfigPath.Text = ofd.FileName;
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(txtRclonePath.Text))
            {
                MessageBox.Show(
                    "Please specify the path to rclone.exe",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(txtConfigPath.Text))
            {
                MessageBox.Show(
                    "Please specify the path to rclone.conf",
                    "Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            RcloneExePath = txtRclonePath.Text.Trim();
            RcloneConfigPath = txtConfigPath.Text.Trim();

            // Check if rclone exists
            if (!File.Exists(RcloneExePath) && RcloneExePath != "rclone.exe")
            {
                var result = MessageBox.Show(
                    $"The file '{RcloneExePath}' does not exist.\n\nContinue anyway?",
                    "File Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
