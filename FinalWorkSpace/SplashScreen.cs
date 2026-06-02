using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Peace_of_Tanks
{
    public partial class SplashScreen : Form
    {
        private readonly GameLobby lobby;
        private readonly Panel videoPanel;
        private readonly System.Windows.Forms.Timer fadeTimer;
        private WebBrowser videoBrowser;
        private int fadeCounter;
        private DialogResult lobbyResult = DialogResult.Cancel;

        public SplashScreen()
        {
            Text = "";
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Black;
            TopMost = true;
            ShowInTaskbar = false;

            videoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            Controls.Add(videoPanel);

            lobby = new GameLobby
            {
                Opacity = 0,
                Visible = false,
                TopMost = true,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                ShowInTaskbar = false
            };
            lobby.FormClosed += (s, e) =>
            {
                lobbyResult = lobby.DialogResult;
                Close();
            };

            fadeTimer = new System.Windows.Forms.Timer { Interval = 50 };
            fadeTimer.Tick += FadeTimer_Tick;

            Load += (s, e) =>
            {
                PlayIntroVideo();
                StartIntroTimeline();
            };
        }

        public GameLobby GetLobby()
        {
            return lobby;
        }

        public DialogResult LobbyResult
        {
            get { return lobbyResult; }
        }

        private void PlayIntroVideo()
        {
            string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Ui", "DongHua.mp4");
            if (!File.Exists(videoPath))
            {
                ShowFallbackTitle();
                return;
            }

            try
            {
                ConfigureWebBrowserEmulation();

                videoBrowser = new WebBrowser
                {
                    Dock = DockStyle.Fill,
                    ScriptErrorsSuppressed = true,
                    ScrollBarsEnabled = false,
                    IsWebBrowserContextMenuEnabled = false,
                    AllowWebBrowserDrop = false,
                    WebBrowserShortcutsEnabled = false
                };
                videoPanel.Controls.Add(videoBrowser);
                videoBrowser.DocumentText = BuildVideoHtml(videoPath);
            }
            catch
            {
                videoPanel.Controls.Clear();
                ShowFallbackTitle();
            }
        }

        private static string BuildVideoHtml(string videoPath)
        {
            string videoUri = new Uri(videoPath).AbsoluteUri;
            string escapedVideoUri = WebUtility.HtmlEncode(videoUri);

            StringBuilder html = new StringBuilder();
            html.Append("<!doctype html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />");
            html.Append("<style>html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#000;}video{width:100%;height:100%;object-fit:cover;background:#000;}</style>");
            html.Append("</head><body>");
            html.Append("<video id=\"intro\" autoplay muted playsinline oncanplay=\"this.play();\" onloadeddata=\"this.play();\">");
            html.Append("<source src=\"").Append(escapedVideoUri).Append("\" type=\"video/mp4\" />");
            html.Append("</video>");
            html.Append("<script>setTimeout(function(){var v=document.getElementById('intro');if(v){try{v.play();}catch(e){}}},100);</script>");
            html.Append("</body></html>");
            return html.ToString();
        }

        private static void ConfigureWebBrowserEmulation()
        {
            try
            {
                string executableName = Path.GetFileName(Application.ExecutablePath);
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    key?.SetValue(executableName, 11001, RegistryValueKind.DWord);
                }
            }
            catch
            {
            }
        }

        private void ShowFallbackTitle()
        {
            Label titleLabel = new Label
            {
                Text = "坦克大战",
                Font = new Font("Microsoft YaHei UI", 48, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            titleLabel.Location = new Point(
                (ClientSize.Width - titleLabel.Width) / 2,
                (ClientSize.Height - titleLabel.Height) / 2
            );
            videoPanel.Controls.Add(titleLabel);
        }

        private void StartIntroTimeline()
        {
            System.Windows.Forms.Timer finishTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            finishTimer.Tick += (s, e) =>
            {
                finishTimer.Stop();
                finishTimer.Dispose();
                StopIntroVideo();
                if (!lobby.IsDisposed && lobby.Visible)
                {
                    lobby.Opacity = 1.0;
                    lobby.Activate();
                    lobby.TopMost = false;
                }
            };

            System.Windows.Forms.Timer startFadeTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            startFadeTimer.Tick += (s, e) =>
            {
                startFadeTimer.Stop();
                startFadeTimer.Dispose();
                StartFadeIn();
            };

            finishTimer.Start();
            startFadeTimer.Start();
        }

        private void StartFadeIn()
        {
            if (lobby.IsDisposed)
            {
                return;
            }

            lobby.Bounds = Bounds;
            lobby.PrepareForIntroFade();
            lobby.Show(this);
            lobby.BringToFront();
            fadeCounter = 0;
            fadeTimer.Start();
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            fadeCounter++;
            double opacity = fadeCounter / 40.0;

            if (opacity >= 1.0)
            {
                lobby.Opacity = 1.0;
                fadeTimer.Stop();
            }
            else
            {
                lobby.Opacity = opacity;
            }
        }

        private void StopIntroVideo()
        {
            if (videoBrowser != null)
            {
                videoBrowser.Stop();
            }

            videoPanel.Visible = false;
        }
    }
}
