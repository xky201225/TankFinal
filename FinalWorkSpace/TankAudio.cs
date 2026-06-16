using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    internal static class TankAudio
    {
        private static readonly List<object> ActivePlayers = new List<object>();
        private static string boomSoundPath;

        public static void PlayTankDestroyed()
        {
            string soundPath = GetBoomSoundPath();
            if (string.IsNullOrEmpty(soundPath))
            {
                return;
            }

            PlayMp3(soundPath);
        }

        private static string GetBoomSoundPath()
        {
            if (!string.IsNullOrEmpty(boomSoundPath) && File.Exists(boomSoundPath))
            {
                return boomSoundPath;
            }

            try
            {
                boomSoundPath = AppPaths.FindContentPath("Resources", "Tanks", "boom.mp3");
            }
            catch (FileNotFoundException)
            {
                boomSoundPath = AppPaths.GetContentPath("Resources", "Tanks", "boom.mp3");
            }

            return File.Exists(boomSoundPath) ? boomSoundPath : string.Empty;
        }

        private static void PlayMp3(string soundPath)
        {
            object player = null;
            try
            {
                Type playerType = Type.GetTypeFromProgID("WMPlayer.OCX");
                if (playerType == null)
                {
                    Debug.WriteLine("Failed to play tank destruction sound: Windows Media Player is unavailable.");
                    return;
                }

                player = Activator.CreateInstance(playerType);
                dynamic mediaPlayer = player;
                mediaPlayer.settings.volume = 100;
                mediaPlayer.settings.autoStart = true;
                mediaPlayer.URL = soundPath;

                lock (ActivePlayers)
                {
                    ActivePlayers.Add(player);
                }

                mediaPlayer.controls.play();
                ReleaseAfterDelay(player, 2500);
            }
            catch (Exception ex)
            {
                ReleasePlayer(player);
                Debug.WriteLine("Failed to play tank destruction sound: " + ex.Message);
            }
        }

        private static void ReleaseAfterDelay(object player, int delayMilliseconds)
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = delayMilliseconds;
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                ReleasePlayer(player);
            };
            timer.Start();
        }

        private static void ReleasePlayer(object player)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                dynamic mediaPlayer = player;
                mediaPlayer.controls.stop();
                mediaPlayer.close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to release tank destruction sound player: " + ex.Message);
            }

            lock (ActivePlayers)
            {
                ActivePlayers.Remove(player);
            }

            if (Marshal.IsComObject(player))
            {
                Marshal.FinalReleaseComObject(player);
            }
        }
    }
}
