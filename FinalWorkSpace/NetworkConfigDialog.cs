using System;
using System.Drawing;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    internal sealed class NetworkConfigDialog : Form
    {
        private readonly TextBox txtHost;
        private readonly TextBox txtPort;
        private readonly TextBox txtName;
        private readonly ComboBox cboDifficulty;

        private NetworkConfigDialog()
        {
            Text = "联机设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(320, 220);

            Label lblHost = new Label { Text = "服务器地址", Left = 20, Top = 20, Width = 90 };
            Label lblPort = new Label { Text = "端口", Left = 20, Top = 60, Width = 90 };
            Label lblName = new Label { Text = "玩家名称", Left = 20, Top = 100, Width = 90 };
            Label lblDifficulty = new Label { Text = "难度", Left = 20, Top = 140, Width = 90 };

            txtHost = new TextBox { Left = 120, Top = 18, Width = 170, Text = "127.0.0.1" };
            txtPort = new TextBox { Left = 120, Top = 58, Width = 170, Text = "51888" };
            string defaultPlayerName = DataManager.CurrentPlayer != null && !string.IsNullOrWhiteSpace(DataManager.CurrentPlayer.UserName)
                ? DataManager.CurrentPlayer.UserName
                : "玩家" + Environment.UserName;
            txtName = new TextBox { Left = 120, Top = 98, Width = 170, Text = defaultPlayerName };
            cboDifficulty = new ComboBox
            {
                Left = 120,
                Top = 138,
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboDifficulty.Items.AddRange(new object[] { "简单", "中等", "困难" });
            cboDifficulty.SelectedIndex = 0;

            Button btnOk = new Button
            {
                Text = "连接",
                Left = 120,
                Top = 175,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            Button btnCancel = new Button
            {
                Text = "取消",
                Left = 210,
                Top = 175,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[]
            {
                lblHost, lblPort, lblName, lblDifficulty,
                txtHost, txtPort, txtName, cboDifficulty,
                btnOk, btnCancel
            });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private NetworkConfig BuildConfig()
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text))
            {
                throw new InvalidOperationException("请输入服务器地址。");
            }

            int port;
            if (!int.TryParse(txtPort.Text, out port) || port <= 0 || port > 65535)
            {
                throw new InvalidOperationException("请输入有效端口。");
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                throw new InvalidOperationException("请输入玩家名称。");
            }

            return new NetworkConfig
            {
                ServerHost = txtHost.Text.Trim(),
                ServerPort = port,
                PlayerName = txtName.Text.Trim(),
                TankType = DataManager.CurrentPlayer.SelectedTank,
                Difficulty = (NetworkDifficulty)cboDifficulty.SelectedIndex
            };
        }

        public static bool ShowDialog(IWin32Window owner, out NetworkConfig config)
        {
            config = null;
            using (NetworkConfigDialog dialog = new NetworkConfigDialog())
            {
                while (true)
                {
                    if (dialog.ShowDialog(owner) != DialogResult.OK)
                    {
                        return false;
                    }

                    try
                    {
                        config = dialog.BuildConfig();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(owner, ex.Message, "联机设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
    }
}
