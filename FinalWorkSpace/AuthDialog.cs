using System;
using System.Drawing;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    internal sealed class AuthDialog : Form
    {
        private readonly TextBox userNameTextBox;
        private readonly TextBox passwordTextBox;
        private readonly TextBox confirmPasswordTextBox;
        private readonly TextBox hostTextBox;
        private readonly TextBox portTextBox;
        private readonly CheckBox registerCheckBox;
        private readonly Label confirmPasswordLabel;

        private AuthDialog(string defaultHost, int defaultPort)
        {
            Text = "用户登录";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 250);

            registerCheckBox = new CheckBox
            {
                Text = "注册新用户",
                Left = 120,
                Top = 18,
                Width = 120
            };
            registerCheckBox.CheckedChanged += (s, e) => UpdateMode();

            Label userNameLabel = new Label { Text = "用户名", Left = 24, Top = 55, Width = 90, AutoSize = true };
            userNameTextBox = new TextBox { Left = 120, Top = 51, Width = 220, Text = "玩家" + Environment.UserName };

            Label passwordLabel = new Label { Text = "密码", Left = 24, Top = 88, Width = 90, AutoSize = true };
            passwordTextBox = new TextBox { Left = 120, Top = 84, Width = 220, UseSystemPasswordChar = true };

            confirmPasswordLabel = new Label { Text = "确认密码", Left = 24, Top = 121, Width = 90, AutoSize = true };
            confirmPasswordTextBox = new TextBox { Left = 120, Top = 117, Width = 220, UseSystemPasswordChar = true };

            Label hostLabel = new Label { Text = "服务器地址", Left = 24, Top = 154, Width = 90, AutoSize = true };
            hostTextBox = new TextBox { Left = 120, Top = 150, Width = 220, Text = defaultHost };

            Label portLabel = new Label { Text = "端口", Left = 24, Top = 187, Width = 90, AutoSize = true };
            portTextBox = new TextBox { Left = 120, Top = 183, Width = 90, Text = defaultPort.ToString() };

            Button okButton = new Button { Text = "登录", Left = 220, Top = 215, Width = 80, DialogResult = DialogResult.OK };
            Button cancelButton = new Button { Text = "取消", Left = 305, Top = 215, Width = 60, DialogResult = DialogResult.Cancel };

            registerCheckBox.CheckedChanged += (s, e) => okButton.Text = registerCheckBox.Checked ? "注册" : "登录";

            Controls.AddRange(new Control[]
            {
                registerCheckBox,
                userNameLabel,
                userNameTextBox,
                passwordLabel,
                passwordTextBox,
                confirmPasswordLabel,
                confirmPasswordTextBox,
                hostLabel,
                hostTextBox,
                portLabel,
                portTextBox,
                okButton,
                cancelButton
            });

            AcceptButton = okButton;
            CancelButton = cancelButton;
            UpdateMode();
        }

        public static bool ShowDialog(string defaultHost, int defaultPort, out AuthRequest request)
        {
            request = null;
            using (AuthDialog dialog = new AuthDialog(defaultHost, defaultPort))
            {
                while (dialog.ShowDialog() == DialogResult.OK)
                {
                    string userName = dialog.userNameTextBox.Text.Trim();
                    string password = dialog.passwordTextBox.Text;
                    string confirmPassword = dialog.confirmPasswordTextBox.Text;
                    string host = dialog.hostTextBox.Text.Trim();
                    int port;

                    if (userName.Length == 0 || userName.Length > 50)
                    {
                        MessageBox.Show(dialog, "用户名长度必须为 1 到 50 个字符。", "用户登录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (password.Length < 6 || password.Length > 50)
                    {
                        MessageBox.Show(dialog, "密码长度必须为 6 到 50 个字符。", "用户登录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (dialog.registerCheckBox.Checked && password != confirmPassword)
                    {
                        MessageBox.Show(dialog, "两次输入的密码不一致。", "用户注册", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (host.Length == 0)
                    {
                        MessageBox.Show(dialog, "请输入服务器地址。", "用户登录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (!int.TryParse(dialog.portTextBox.Text.Trim(), out port) || port <= 0 || port > 65535)
                    {
                        MessageBox.Show(dialog, "请输入有效端口。", "用户登录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    request = new AuthRequest
                    {
                        IsRegister = dialog.registerCheckBox.Checked,
                        UserName = userName,
                        Password = password,
                        ServerHost = host,
                        ServerPort = port
                    };
                    return true;
                }
            }

            return false;
        }

        private void UpdateMode()
        {
            confirmPasswordLabel.Visible = registerCheckBox.Checked;
            confirmPasswordTextBox.Visible = registerCheckBox.Checked;
        }
    }

    internal sealed class AuthRequest
    {
        public bool IsRegister { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ServerHost { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 51888;
    }
}
