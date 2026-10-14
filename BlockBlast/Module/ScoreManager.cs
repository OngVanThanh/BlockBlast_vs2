using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlockBlast.Module
{
    // Dữ liệu 1 ván đấu của người chơi
    public class MatchRecord
    {
        public int MatchIndex { get; set; }
        public int Score { get; set; }
        public string RankTier { get; set; } // SSS, SS, S, A, B
        public DateTime Date { get; set; }
    }

    // Dữ liệu 1 Huân chương trong Tủ trưng bày
    public class Medal
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedDate { get; set; }
    }

    public static class ScoreManager
    {
        private static string GetAppDataFolder()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        // ================================================================
        //  1. QUẢN LÝ KỶ LỤC & CÀI ĐẶT ÂM THANH
        // ================================================================

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
            return false;
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

        // ================================================================
        //  2. LỊCH SỬ ĐẤU CÁ NHÂN (LEADERBOARD.DAT)
        // ================================================================

        public static void SaveMatchHistory(int score)
        {
            if (score <= 0) return;
            try
            {
                string rankTier = (score >= 30000) ? "SSS (Huyền Thoại)"
                                : (score >= 20000) ? "SS (Kiện Tướng)"
                                : (score >= 10000) ? "S (Cao Thủ)"
                                : (score >= 5000) ? "A (Chiến Binh)"
                                : (score >= 3000) ? "B (Tân Thủ)" : "C (Khởi Đầu)";

                string path = Path.Combine(GetAppDataFolder(), "leaderboard.dat");
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.WriteLine($"{score}|{rankTier}|{DateTime.Now:yyyy-MM-dd HH:mm}");
                }
            }
            catch { }
        }

        public static List<MatchRecord> LoadMatchHistory()
        {
            var list = new List<MatchRecord>();
            try
            {
                string path = Path.Combine(GetAppDataFolder(), "leaderboard.dat");
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    int count = 1;
                    foreach (string line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 3 && int.TryParse(parts[0], out int sc) && DateTime.TryParse(parts[2], out DateTime dt))
                        {
                            list.Add(new MatchRecord
                            {
                                MatchIndex = count++,
                                Score = sc,
                                RankTier = parts[1],
                                Date = dt
                            });
                        }
                    }
                }
            }
            catch { }
            // Trả về danh sách ván đấu đảo ngược (ván mới nhất nằm trên cùng)
            list.Reverse();
            return list;
        }

        // ================================================================
        //  3. BỘ SƯU TẬP 12 HUÂN CHƯƠNG CHIẾN TÍCH (4 CỘT x 3 DÒNG)
        // ================================================================

        private static readonly List<Medal> BaseMedals = new List<Medal>
        {
            // Hàng 1: 4 Mốc điểm khởi đầu & chinh phục
            new Medal { Id = "SCORE_3000", Title = "Khởi Đầu Đột Phá", Description = "Đạt mốc 3,000 điểm trong 1 ván đấu", Icon = "🥉" },
            new Medal { Id = "SCORE_5000", Title = "Chiến Binh Bền Bỉ", Description = "Đạt mốc 5,000 điểm trong 1 ván đấu", Icon = "🥈" },
            new Medal { Id = "SCORE_10000", Title = "Bậc Thầy Xếp Gạch", Description = "Đạt mốc 10,000 điểm trong 1 ván đấu", Icon = "🥇" },
            new Medal { Id = "SCORE_20000", Title = "Đại Kiện Tướng", Description = "Đạt mốc 20,000 điểm trong 1 ván đấu", Icon = "💎" },

            // Hàng 2: 2 Mốc điểm tối thượng + 2 Vũ khí công phá
            new Medal { Id = "SCORE_30000", Title = "Huyền Thoại Bất Bại", Description = "Đạt mốc 30,000 điểm siêu thử thách", Icon = "👑" },
            new Medal { Id = "SCORE_50000", Title = "Vị Thần Block Blast", Description = "Chạm đỉnh vinh quang 50,000 điểm", Icon = "🌌" },
            new Medal { Id = "WEAPON_BOMB", Title = "Bão Lửa 3x3", Description = "Bắn nổ tung cụm gạch bằng Bom 3x3", Icon = "💣" },
            new Medal { Id = "WEAPON_CROSS", Title = "Tia Sáng Hoàng Kim", Description = "Quét sạch 2 hàng & cột bằng Tia Chữ Thập", Icon = "✨" },

            // Hàng 3: 2 Vũ khí ma thuật + 2 Huân chương kỹ năng siêu cấp
            new Medal { Id = "WEAPON_LIGHTNING", Title = "Thiên Lôi Thần Phạt", Description = "Quét sạch 1 màu gạch bằng Sấm Sét", Icon = "⚡" },
            new Medal { Id = "WEAPON_GRAVITY", Title = "Bậc Thầy Trọng Lực", Description = "Kích hoạt Trọng Lực dồn gạch nổ dòng", Icon = "🌀" },
            new Medal { Id = "COMBO_MEGA", Title = "Siêu Combo Tứ Hàng", Description = "Làm nổ từ 4 dòng cùng lúc trong 1 nước đi", Icon = "🔥" },
            new Medal { Id = "WEAPON_MASTER", Title = "Bậc Thầy Chiến Thuật", Description = "Mở khóa và sử dụng thành thạo cả 4 vũ khí", Icon = "🛡️" }
        };

        public static List<Medal> LoadMedals()
        {
            var medals = BaseMedals.Select(m => new Medal
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Icon = m.Icon,
                IsUnlocked = false,
                UnlockedDate = null
            }).ToList();

            try
            {
                string path = Path.Combine(GetAppDataFolder(), "achievements.dat");
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            string id = parts[0];
                            if (DateTime.TryParse(parts[1], out DateTime dt))
                            {
                                var found = medals.FirstOrDefault(m => m.Id == id);
                                if (found != null)
                                {
                                    found.IsUnlocked = true;
                                    found.UnlockedDate = dt;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return medals;
        }

        public static bool UnlockMedal(string medalId)
        {
            try
            {
                var medals = LoadMedals();
                var medal = medals.FirstOrDefault(m => m.Id == medalId);
                if (medal != null && !medal.IsUnlocked)
                {
                    string path = Path.Combine(GetAppDataFolder(), "achievements.dat");
                    using (StreamWriter sw = new StreamWriter(path, true))
                    {
                        sw.WriteLine($"{medalId}|{DateTime.Now:yyyy-MM-dd HH:mm}");
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static void CheckScoreMedals(int score)
        {
            if (score >= 3000) UnlockMedal("SCORE_3000");
            if (score >= 5000) UnlockMedal("SCORE_5000");
            if (score >= 10000) UnlockMedal("SCORE_10000");
            if (score >= 20000) UnlockMedal("SCORE_20000");
            if (score >= 30000) UnlockMedal("SCORE_30000");
            if (score >= 50000) UnlockMedal("SCORE_50000");
        }

        public static void CheckWeaponMaster()
        {
            var m = LoadMedals();
            if (m.Any(x => x.Id == "WEAPON_BOMB" && x.IsUnlocked) &&
                m.Any(x => x.Id == "WEAPON_CROSS" && x.IsUnlocked) &&
                m.Any(x => x.Id == "WEAPON_LIGHTNING" && x.IsUnlocked) &&
                m.Any(x => x.Id == "WEAPON_GRAVITY" && x.IsUnlocked))
            {
                UnlockMedal("WEAPON_MASTER");
            }
        }

        public static void ResetAllHistory()
        {
            try
            {
                string[] files = { "highscore.dat", "leaderboard.dat", "achievements.dat", "tutorial_done.txt", "gamestate.json", "items.json" };
                foreach (var f in files)
                {
                    string p = Path.Combine(GetAppDataFolder(), f);
                    if (File.Exists(p)) File.Delete(p);
                }
            }
            catch { }
        }
    }
}