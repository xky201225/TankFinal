using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;

namespace Peace_of_Tanks
{
    internal sealed class NetworkVoiceChat
    {
        private readonly Action<byte[]> sendVoiceClip;
        private string recordAlias;
        private string recordPath;
        private bool capturing;

        public NetworkVoiceChat(Action<byte[]> sendVoiceClip)
        {
            this.sendVoiceClip = sendVoiceClip;
        }

        public bool StartCapture()
        {
            if (capturing)
            {
                return true;
            }

            recordAlias = "voice_" + Guid.NewGuid().ToString("N");
            recordPath = Path.Combine(Path.GetTempPath(), recordAlias + ".wav");

            try
            {
                if (Mci("open new type waveaudio alias " + recordAlias) != 0)
                {
                    return false;
                }

                Mci("set " + recordAlias + " time format ms bitspersample 16 samplespersec 8000 channels 1 bytespersec 16000 alignment 2");
                if (Mci("record " + recordAlias) != 0)
                {
                    CloseRecorder();
                    return false;
                }

                capturing = true;
                return true;
            }
            catch
            {
                CloseRecorder();
                return false;
            }
        }

        public void StopCapture()
        {
            if (!capturing)
            {
                return;
            }

            capturing = false;
            try
            {
                Mci("stop " + recordAlias);
                Mci("save " + recordAlias + " \"" + recordPath + "\"");
                CloseRecorder();

                if (File.Exists(recordPath))
                {
                    byte[] bytes = File.ReadAllBytes(recordPath);
                    if (bytes.Length > 44)
                    {
                        sendVoiceClip(bytes);
                    }
                }
            }
            catch
            {
                CloseRecorder();
            }
            finally
            {
                TryDelete(recordPath);
                recordPath = null;
                recordAlias = null;
            }
        }

        public void Play(byte[] wavBytes)
        {
            if (wavBytes == null || wavBytes.Length == 0)
            {
                return;
            }

            string path = Path.Combine(Path.GetTempPath(), "voice_play_" + Guid.NewGuid().ToString("N") + ".wav");
            try
            {
                File.WriteAllBytes(path, wavBytes);
                SoundPlayer player = new SoundPlayer(path);
                player.LoadCompleted += (s, e) =>
                {
                    try
                    {
                        player.Play();
                    }
                    catch
                    {
                    }
                };
                player.LoadAsync();
            }
            catch
            {
                TryDelete(path);
            }
        }

        private void CloseRecorder()
        {
            if (!string.IsNullOrEmpty(recordAlias))
            {
                Mci("close " + recordAlias);
            }
        }

        private static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static int Mci(string command)
        {
            return mciSendString(command, null, 0, IntPtr.Zero);
        }

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string command, string returnValue, int returnLength, IntPtr callback);
    }
}
