using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlockBlast.Module
{
    // Cấu trúc một dòng dữ liệu trong bảng xếp hạng
    public class LeaderboardEntry
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public DateTime Date { get; set; }
    }

    public static class ScoreManager
    {
        private static string GetAppDataFolder()
        {
            // Trả về thư mục chạy hiện tại của ứng dụng
            return AppDomain.CurrentDomain.BaseDirectory;
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

        public static void ResetAllHistory()
        {
            try
            {
                string highPath = Path.Combine(GetAppDataFolder(), "highscore.dat");
                if (File.Exists(highPath)) File.Delete(highPath);

                string lbPath = Path.Combine(GetAppDataFolder(), "leaderboard.dat");
                if (File.Exists(lbPath)) File.Delete(lbPath);

                string tutPath = Path.Combine(GetAppDataFolder(), "tutorial_done.txt");
                if (File.Exists(tutPath)) File.Delete(tutPath);

                if (File.Exists("tutorial_done.txt")) File.Delete("tutorial_done.txt");

                string saveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockBlast", "gamestate.dat");
                if (File.Exists(saveFilePath)) File.Delete(saveFilePath);

                string saveJsonPath = Path.Combine(GetAppDataFolder(), "gamestate.json");
                if (File.Exists(saveJsonPath)) File.Delete(saveJsonPath);
            }
            catch { }
        }

        // --- BỔ SUNG: QUẢN LÝ BẢNG XẾP HẠNG TOP 10 (LEADERBOARD) ---

        public static void SaveLeaderboardScore(string playerName, int score)
        {
            try
            {
                var scores = LoadLeaderboardScores();

                // Thêm kết quả mới
                scores.Add(new LeaderboardEntry
                {
                    PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Người chơi" : playerName.Trim(),
                    Score = score,
                    Date = DateTime.Now
                });

                // Sắp xếp giảm dần theo điểm và lấy tối đa 10 dòng cao nhất
                scores = scores.OrderByDescending(s => s.Score).Take(10).ToList();

                string path = Path.Combine(GetAppDataFolder(), "leaderboard.dat");
                using (StreamWriter writer = new StreamWriter(path))
                {
                    foreach (var entry in scores)
                    {
                        writer.WriteLine($"{entry.PlayerName}|{entry.Score}|{entry.Date:yyyy-MM-dd HH:mm:ss}");
                    }
                }
            }
            catch { }
        }

        public static List<LeaderboardEntry> LoadLeaderboardScores()
        {
            var scores = new List<LeaderboardEntry>();
            try
            {
                string path = Path.Combine(GetAppDataFolder(), "leaderboard.dat");
                if (File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split('|');
                            if (parts.Length >= 3 && int.TryParse(parts[1], out int score) && DateTime.TryParse(parts[2], out DateTime date))
                            {
                                scores.Add(new LeaderboardEntry
                                {
                                    PlayerName = parts[0],
                                    Score = score,
                                    Date = date
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            return scores.OrderByDescending(s => s.Score).Take(10).ToList();
        }

        // --- QUẢN LÝ ÂM THANH ---

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