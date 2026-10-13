using System.Collections.Generic;

namespace BlockBlast.Module
{
    // Class chứa dữ liệu của từng màn Hướng dẫn / Puzzle
    public class TutorialStep
    {
        public string Title { get; set; }
        public string Instruction { get; set; }
        public int[,] BoardLayout { get; set; }
        public int[,] DockPieceShape { get; set; }
        public int ColorIndex { get; set; }

        // Tọa độ bắt buộc phải đặt khối (nếu = -1 là không khóa tọa độ cố định)
        public int TargetRow { get; set; } = -1;
        public int TargetCol { get; set; } = -1;

        // Nếu màn này dạy dùng Item thì gán loại Item cần cấp cho người chơi thử
        public ItemType? RequiredItem { get; set; } = null;
    }

    // Class quản lý tiến trình các màn chơi
    public class TutorialManager
    {
        public int CurrentLevelIndex { get; private set; } = 0;
        public bool IsTutorialActive { get; set; } = true;

        private readonly List<TutorialStep> levels = new List<TutorialStep>();

        public TutorialManager()
        {
            InitPuzzleLevels();
        }

        private void InitPuzzleLevels()
        {
            // ==========================================
            // MÀN 1: HÌNH CHỮ THẬP (Thả khối 2x2 vào tâm nổ 2 hàng + 2 cột)
            // ==========================================
            int[,] board1 = new int[8, 8];
            for (int i = 0; i < 8; i++)
            {
                board1[3, i] = 1; // Hàng 3
                board1[4, i] = 1; // Hàng 4
                board1[i, 3] = 1; // Cột 3
                board1[i, 4] = 1; // Cột 4
            }
            // Đục lỗ ở giữa tâm (2x2)
            board1[3, 3] = 0; board1[3, 4] = 0;
            board1[4, 3] = 0; board1[4, 4] = 0;

            levels.Add(new TutorialStep
            {
                Title = "Màn 1: Chữ Thập Combo",
                Instruction = "Kéo khối 2x2 thả vào TÂM chữ thập để nổ Combo!",
                BoardLayout = board1,
                DockPieceShape = new int[,] { { 1, 1 }, { 1, 1 } },
                ColorIndex = 2,
                TargetRow = 3,
                TargetCol = 3
            });

            // ==========================================
            // MÀN 2: LẤP ĐẦY KHUNG (Thả khối 1x4 để ăn dòng ngang)
            // ==========================================
            int[,] board2 = new int[8, 8];
            for (int c = 0; c < 8; c++)
            {
                if (c < 2 || c > 5) board2[2, c] = 3;
            }

            levels.Add(new TutorialStep
            {
                Title = "Màn 2: Lấp Đầy Khe Mới",
                Instruction = "Đặt khối 1x4 vào khe hở để hoàn thành hàng ngang!",
                BoardLayout = board2,
                DockPieceShape = new int[,] { { 1, 1, 1, 1 } },
                ColorIndex = 4,
                TargetRow = 2,
                TargetCol = 2
            });

            // ==========================================
            // MÀN 3: SIÊU COMBO 3 HÀNG (Thả khối 3x3 vào góc [1,1])
            // ==========================================
            int[,] board3 = new int[8, 8];
            for (int r = 1; r <= 3; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    // Đổ đầy 3 hàng (hàng 1, 2, 3), CHỈ để trống vùng 3x3 từ hàng 1-3, cột 1-3
                    if (!(c >= 1 && c <= 3))
                    {
                        board3[r, c] = 5;
                    }
                }
            }

            levels.Add(new TutorialStep
            {
                Title = "Màn 3: Siêu Combo Mega",
                Instruction = "Thả khối 3x3 vào góc trống để nổ tung 3 hàng cùng lúc!",
                BoardLayout = board3,
                DockPieceShape = new int[,] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } },
                ColorIndex = 1,
                TargetRow = 1,
                TargetCol = 1
            });

            // ==========================================
            // MÀN 4: THỬ ITEM ĐỔI GẠCH (RerollDock)
            // ==========================================
            int[,] board4 = new int[8, 8];
            for (int c = 0; c < 8; c++)
            {
                if (c != 3 && c != 4) board4[4, c] = 2; // Hàng 4 chừa trống 2 ô nhưng khay chưa có gạch
            }
            levels.Add(new TutorialStep
            {
                Title = "Màn 4/8: Item Đổi Gạch",
                Instruction = "Khay đang trống! Hãy CLICK vào 'Đổi Gạch' ở cột phải để lấy gạch mới rồi đặt lên bàn cờ!",
                BoardLayout = board4,
                DockPieceShape = null, // Khay trống để người chơi bấm Đổi Gạch
                ColorIndex = 2,
                RequiredItem = ItemType.RerollDock
            });

            // ==========================================
            // MÀN 5: THỬ ITEM BOM 3x3 (Kéo ná cao su)
            // ==========================================
            int[,] board5 = new int[8, 8];
            for (int r = 2; r <= 4; r++)
                for (int c = 2; c <= 4; c++)
                    board5[r, c] = 1; // Cụm gạch 3x3 giữa bàn cờ

            levels.Add(new TutorialStep
            {
                Title = "Màn 5/8: Bắn Bom 3x3",
                Instruction = "Nhấn giữ 'Bom 3x3', KÉO CĂNG NÁ SANG PHẢI để đường đạn vàng trỏ vào cụm gạch rồi thả tay!",
                BoardLayout = board5,
                DockPieceShape = null,
                RequiredItem = ItemType.Bomb3x3
            });

            // ==========================================
            // MÀN 6: THỬ ITEM TIA CHỮ THẬP
            // ==========================================
            int[,] board6 = new int[8, 8];
            for (int i = 1; i < 7; i++)
            {
                board6[3, i] = 4; // Hàng 3
                board6[i, 3] = 4; // Cột 3
            }
            levels.Add(new TutorialStep
            {
                Title = "Màn 6/8: Bắn Tia Chữ Thập",
                Instruction = "Kéo căng ná bắn 'Tia Chữ Thập' trúng tâm chữ thập trên bàn cờ để quét ngang & dọc!",
                BoardLayout = board6,
                DockPieceShape = null,
                RequiredItem = ItemType.CrossClear
            });

            // ==========================================
            // MÀN 7: THỬ ITEM SẤM SÉT
            // ==========================================
            int[,] board7 = new int[8, 8];
            board7[2, 2] = 1; board7[2, 5] = 1;
            board7[3, 3] = 1; board7[3, 4] = 1;
            board7[4, 3] = 1; board7[4, 4] = 1;
            board7[5, 2] = 1; board7[5, 5] = 1;

            levels.Add(new TutorialStep
            {
                Title = "Màn 7/8: Bắn Sấm Sét",
                Instruction = "Kéo ná ngắm bắn 'Sấm Sét' trúng đúng 1 viên gạch ĐỎ để tiêu diệt toàn bộ gạch cùng màu!",
                BoardLayout = board7,
                DockPieceShape = null,
                RequiredItem = ItemType.ColorLightning
            });

            // ==========================================
            // MÀN 8: THỬ ITEM TRỌNG LỰC
            // ==========================================
            int[,] board8 = new int[8, 8];
            for (int c = 0; c < 8; c++)
            {
                board8[c % 4, c] = 3; // 8 viên gạch nằm lơ lửng ở 8 cột, khi rơi xuống đáy sẽ nổ 1 hàng ngang!
            }
            levels.Add(new TutorialStep
            {
                Title = "Màn 8/8: Bắn Trọng Lực",
                Instruction = "Kéo ná bắn 'Trọng Lực' vào bàn cờ để dồn các viên gạch lơ lửng rơi xuống đáy và nổ dòng!",
                BoardLayout = board8,
                DockPieceShape = null,
                RequiredItem = ItemType.GravityFall
            });
        }

        public TutorialStep GetCurrentLevel()
        {
            if (CurrentLevelIndex >= 0 && CurrentLevelIndex < levels.Count)
                return levels[CurrentLevelIndex];
            return null;
        }

        public bool NextLevel()
        {
            CurrentLevelIndex++; // Chú ý chữ C viết hoa

            // Kiểm tra nếu đã vượt qua màn hướng dẫn cuối cùng
            if (CurrentLevelIndex >= levels.Count) // Sửa tutorialLevels -> levels
            {
                IsTutorialActive = false; // Tắt hoàn toàn chế độ Tutorial
                return false;             // Báo hiệu đã hết màn hướng dẫn
            }

            return true; // Tiếp tục sang màn hướng dẫn tiếp theo
        }

        // Hàm hỗ trợ reset tutorial từ đầu nếu người chơi muốn chơi lại
        public void ResetTutorial()
        {
            CurrentLevelIndex = 0;
            IsTutorialActive = true;
        }
    }
}