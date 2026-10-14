using System;
using System.Collections.Generic;
using System.IO;

namespace BlockBlast.Module
{
    // Class chứa dữ liệu của từng màn Hướng dẫn / Puzzle (Mô hình 2 Pha)
    public class TutorialStep
    {
        public string Title { get; set; }
        public string Instruction { get; set; }

        // Pha 1: Bố cục gạch và khối cần đặt để nổ combo
        public int[,] BoardLayout { get; set; }
        public int[,] DockPieceShape { get; set; }
        public int ColorIndex { get; set; }
        public int TargetRow { get; set; } = -1;
        public int TargetCol { get; set; } = -1;
    
 
        // Item được thưởng khi làm nổ combo thành công
        public ItemType? RewardedItem { get; set; } = null;
        public ItemType? RequiredItem => RewardedItem;


        // Pha 2: Bố cục mục tiêu sinh ra ngay sau khi nổ để người chơi kéo ná bắn thực hành
        public int[,] TargetBoardLayout { get; set; } = null;
        public string PracticeInstruction { get; set; } = null;

    }

    public class TutorialManager
    {
        public int CurrentLevelIndex { get; private set; } = 0;
        public bool IsTutorialActive { get; set; } = true;

        // Pha hiện tại của màn chơi: 1 = Đặt gạch tạo Combo nhận Item; 2 = Kéo ná bắn Item thực hành
        public int CurrentPhase { get; set; } = 1;

        private readonly List<TutorialStep> levels = new List<TutorialStep>();

        private string GetTutorialDoneFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tutorial_done.txt");
        }

        // Kiểm tra đã hoàn thành Tutorial trước đó chưa (Chống lỗi lặp Tutorial)
        public bool IsTutorialCompleted => File.Exists(GetTutorialDoneFilePath());

        public void MarkTutorialCompleted()
        {
            try
            {
                File.WriteAllText(GetTutorialDoneFilePath(), "completed");
            }
            catch { }
        }

        public TutorialManager()
        {
            InitPuzzleLevels();
        }

        private void InitPuzzleLevels()
        {
            // ==========================================
            // MÀN 1: TẠO & BẮN TIA CHỮ THẬP (CROSS CLEAR)
            // ==========================================
            int[,] board1 = new int[8, 8];
            for (int i = 0; i < 8; i++)
            {
                board1[3, i] = 1; board1[4, i] = 1;
                board1[i, 3] = 1; board1[i, 4] = 1;
            }
            board1[3, 3] = 0; board1[3, 4] = 0;
            board1[4, 3] = 0; board1[4, 4] = 0;

            // Mục tiêu thực hành bắn chữ thập
            int[,] target1 = new int[8, 8];
            for (int i = 0; i < 8; i++) { target1[3, i] = 4; target1[i, 3] = 4; }

            levels.Add(new TutorialStep
            {
                Title = "Màn 1/4: Tạo & Dùng Tia Chữ Thập",
                Instruction = "Thả khối 2x2 vào TÂM chữ thập để nổ 2 hàng & 2 cột -> Nhận TIA CHỮ THẬP!",
                BoardLayout = board1,
                DockPieceShape = new int[,] { { 1, 1 }, { 1, 1 } },
                ColorIndex = 2,
                TargetRow = 3,
                TargetCol = 3,
                RewardedItem = ItemType.CrossClear,
                TargetBoardLayout = target1,
                PracticeInstruction = "★ ĐÃ NHẬN TIA CHỮ THẬP! Kéo căng ná bắn trúng tâm để quét sạch 2 hàng gạch!"
            });

            // ==========================================
            // MÀN 2: TẠO & BẮN BOM 3x3 (BOMB 3x3)
            // ==========================================
            int[,] board2 = new int[8, 8];
            for (int r = 1; r <= 3; r++)
                for (int c = 0; c < 8; c++)
                    if (!(c >= 1 && c <= 3)) board2[r, c] = 5;

            // Cụm gạch 3x3 mục tiêu sinh ra để bắn bom
            int[,] target2 = new int[8, 8];
            for (int r = 2; r <= 4; r++)
                for (int c = 2; c <= 4; c++) target2[r, c] = 1;

            levels.Add(new TutorialStep
            {
                Title = "Màn 2/4: Tạo & Dùng Bom 3x3",
                Instruction = "Đặt khối 3x3 vào góc trống để nổ 3 hàng cùng lúc -> Nhận BOM 3x3!",
                BoardLayout = board2,
                DockPieceShape = new int[,] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } },
                ColorIndex = 1,
                TargetRow = 1,
                TargetCol = 1,
                RewardedItem = ItemType.Bomb3x3,
                TargetBoardLayout = target2,
                PracticeInstruction = "★ ĐÃ NHẬN BOM 3X3! Nhấn giữ Bom ở cột phải, kéo ná bắn tung cụm gạch giữa bàn cờ!"
            });

            // ==========================================
            // MÀN 3: NỔ 2 HÀNG ĐÔI ĐỂ NHẬN ITEM ĐỔI GẠCH (REROLL)
            // ==========================================
            int[,] board3 = new int[8, 8];
            for (int c = 0; c < 8; c++)
            {
                if (c < 3 || c > 4) { board3[3, c] = 3; board3[4, c] = 3; }
            }

            // Mục tiêu thực hành Pha 2: Bàn cờ sinh ra 1 hàng gạch có khe trống 1x2 ở giữa
            int[,] target3 = new int[8, 8];
            for (int c = 0; c < 8; c++)
            {
                if (c != 3 && c != 4) target3[4, c] = 3;
            }

            levels.Add(new TutorialStep
            {
                Title = "Màn 3/4: Tạo & Dùng Đổi Gạch",
                Instruction = "Đặt khối 2x2 vào giữa để nổ 2 hàng đôi -> Nhận ITEM ĐỔI GẠCH!",
                BoardLayout = board3,
                DockPieceShape = new int[,] { { 1, 1 }, { 1, 1 } },
                ColorIndex = 6,
                TargetRow = 3,
                TargetCol = 3,
                RewardedItem = ItemType.RerollDock,
                TargetBoardLayout = target3,
                PracticeInstruction = "★ ĐÃ NHẬN ĐỔI GẠCH! Khay đang trống, hãy CLICK 'Đổi Gạch' ở cột phải để lấy gạch mới!"
            });

            // ==========================================
            // MÀN 4: SIÊU COMBO 4 HÀNG -> NHẬN SẤM SÉT (LIGHTNING)
            // ==========================================
            int[,] board4 = new int[8, 8];
            for (int r = 2; r <= 5; r++)
                for (int c = 0; c < 8; c++)
                    if (c != 4) board4[r, c] = 2;

            int[,] target4 = new int[8, 8];
            target4[1, 1] = 1; target4[1, 6] = 1; target4[3, 3] = 1; target4[5, 5] = 1; target4[6, 2] = 1;

            levels.Add(new TutorialStep
            {
                Title = "Màn 4/4: Siêu Combo 4 Hàng & Sấm Sét",
                Instruction = "Thả thanh 4x1 vào khe dọc để nổ 4 hàng cùng lúc -> Nhận SẤM SÉT!",
                BoardLayout = board4,
                DockPieceShape = new int[,] { { 1 }, { 1 }, { 1 }, { 1 } },
                ColorIndex = 4,
                TargetRow = 2,
                TargetCol = 4,
                RewardedItem = ItemType.ColorLightning,
                TargetBoardLayout = target4,
                PracticeInstruction = "★ ĐÃ NHẬN SẤM SÉT! Bắn sét trúng 1 viên gạch ĐỎ để quét sạch toàn bộ gạch cùng màu!"
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
            CurrentLevelIndex++;
            CurrentPhase = 1;

            if (CurrentLevelIndex >= levels.Count)
            {
                IsTutorialActive = false;
                MarkTutorialCompleted(); // Đánh dấu hoàn thành vĩnh viễn
                return false;
            }
            return true;
        }

        public void ResetTutorial()
        {
            CurrentLevelIndex = 0;
            CurrentPhase = 1;
            IsTutorialActive = true;
        }
    }
}