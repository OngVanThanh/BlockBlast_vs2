using System;
using System.IO;

namespace BlockBlast.Module
{
    public static class ScoreManager
    {
        private static string GetAppDataFolder()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockBlast");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        public static int LoadSavedHighScore()
        {
            try
            {
                string path = Path.Combine(GetAppDataFolder(), "highscore.dat");
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    if (int.TryParse(content.Trim(), out int saved) && saved > 0)
                        return saved;
                }
            }
            catch { }
            return 0;
        }

        public static void SaveHighScoreSilently(int score)
        {
            try
            {
                int currentHigh = LoadSavedHighScore();
                if (score > currentHigh)
                {
                    string path = Path.Combine(GetAppDataFolder(), "highscore.dat");
                    File.WriteAllText(path, score.ToString());
                }
            }
            catch { }
        }

        public static bool LoadAudioSettings()
        {
            try
            {
                string path = Path.Combine(GetAppDataFolder(), "settings.dat");
                if (File.Exists(path))
                {
                    string txt = File.ReadAllText(path).Trim();
                    return txt == "1" || txt.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false; // Mặc định bật âm thanh
        }

        public static void SaveAudioSettings(bool isMuted)
        {
            try
            {
                string path = Path.Combine(GetAppDataFolder(), "settings.dat");
                File.WriteAllText(path, isMuted ? "1" : "0");
            }
            catch { }
        }
    }
}