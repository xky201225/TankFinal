using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 初始化资源管理器
            ResourceManager.Initialize();

            // 加载玩家数据
            DataManager.LoadData();

            // 游戏循环：开场动画 -> 大厅 -> 游戏 -> 大厅
            while (true)
            {
                GameLobby lobby;

                // 首次启动显示开场动画
                SplashScreen splash = new SplashScreen();
                splash.ShowDialog();
                lobby = splash.GetLobby();

                DialogResult result = splash.LobbyResult;

                if (result == DialogResult.OK)
                {
                    // 玩家选择了坦克和模式，启动游戏
                    MainWindow mainWindow = new MainWindow(lobby.SelectedMode, lobby.SelectedTank);
                    mainWindow.ShowDialog();

                    // 检查是否要返回大厅
                    if (mainWindow.ReturnToLobby)
                    {
                        continue;  // 返回大厅
                    }
                    else
                    {
                        break;  // 游戏结束，退出程序
                    }
                }
                else
                {
                    // 用户在大厅点击退出
                    break;
                }
            }
        }
    }
}
