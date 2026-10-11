using BlockBlast.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace BlockBlast
{
    public class GameManager
    {
        public GameBoard Board { get; private set; }
        public int CurrentScore { get; private set; }
        public int HighScore { get; set; }
        public bool IsPaused { get; set; }

        private readonly Random rand = new Random();
        public const int BOARD_SIZE = 8;

        public GameManager()
        {
            Board = new GameBoard();
            CurrentScore = 0;
            HighScore = 0;
        }

        public BlockPiece CreateRandomPiece()
        {
            int shapeIndex = rand.Next(BlockPiece.AllShapes.Count);
            int[,] shape = BlockPiece.AllShapes[shapeIndex];
            int colorIndex = rand.Next(1, 8); // Chọn màu ngẫu nhiên từ 1 đến 7
            return new BlockPiece(shape, colorIndex);
        }

        public void AddScore(int points)
        {
            CurrentScore += points;
            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
            }
        }

        public void ResetGame()
        {
            Board.Clear();
            CurrentScore = 0;
        }

        public bool CheckGameOver(List<BlockPiece> remainingPieces)
        {
            if (remainingPieces == null || remainingPieces.Count == 0) return false;

            foreach (var piece in remainingPieces)
            {
                for (int r = 0; r < GameBoard.BOARD_SIZE; r++)
                {
                    for (int c = 0; c < GameBoard.BOARD_SIZE; c++)
                    {
                        if (Board.CanPlaceShape(piece.Shape, r, c))
                            return false; // Vẫn còn vị trí đặt hợp lệ
                    }
                }
            }
            return true;
        }
        // Không còn chỗ đặt -> Game Over
        // ==========================================
        // LƯU & TẢI TRẠNG THÁI GAME CHI TIẾT (BÀN CỜ + BLOCK ĐANG TẠO Ở DOCK)
        // ==========================================
        private int[,] GetGameBoardMatrix(GameBoard board)
        {
            // Đảm bảo trong class GameBoard của bạn có hàm GetGrid() hoặc thuộc tính Grid
            return board.GetGrid();
        }
        public void SaveGameState(Panel pnl1, Panel pnl2, Panel pnl3)
        {
            try
            {
                int[,] boardGrid = GetGameBoardMatrix(Board);

                using (StreamWriter writer = new StreamWriter(GetSaveGameFilePath()))
                {
                    // 1. Lưu điểm hiện tại
                    writer.WriteLine(CurrentScore);

                    // 2. Lưu trạng thái lưới bàn cờ 8x8
                    for (int r = 0; r < BOARD_SIZE; r++)
                    {
                        int[] rowValues = new int[BOARD_SIZE];
                        for (int c = 0; c < BOARD_SIZE; c++)
                        {
                            rowValues[c] = boardGrid[r, c];
                        }
                        writer.WriteLine(string.Join(",", rowValues));
                    }

                    // 3. Lưu chi tiết trạng thái 3 ô Dock (hình dạng block, màu sắc, hiển thị)
                    Panel[] panels = { pnl1, pnl2, pnl3 };
                    for (int i = 0; i < panels.Length; i++)
                    {
                        if (panels[i] != null && panels[i].Tag is BlockPiece piece && piece.Shape != null)
                        {
                            // Đánh dấu ô này có block: SLOT:index:isVisible:colorIndex
                            writer.WriteLine($"SLOT:{i}:TRUE:{piece.ColorIndex}");

                            int rows = piece.Shape.GetLength(0);
                            int cols = piece.Shape.GetLength(1);
                            writer.WriteLine($"{rows},{cols}");

                            // Lưu từng hàng của ma trận block shape
                            for (int r = 0; r < rows; r++)
                            {
                                int[] shapeRow = new int[cols];
                                for (int c = 0; c < cols; c++)
                                {
                                    shapeRow[c] = piece.Shape[r, c];
                                }
                                writer.WriteLine(string.Join(",", shapeRow));
                            }
                        }
                        else
                        {
                            // Ô trống hoặc không có block
                            writer.WriteLine($"SLOT:{i}:FALSE:0");
                            writer.WriteLine("0,0");
                        }
                    }
                }
            }
            catch { }
        }
        private string GetSaveGameFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "BlockBlast");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "gamestate.dat");
        }
        public bool LoadGameState(Panel pnl1, Panel pnl2, Panel pnl3)
        {
            try
            {
                string path = GetSaveGameFilePath();
                if (!File.Exists(path)) return false;

                using (StreamReader reader = new StreamReader(path))
                {
                    // 1. Đọc điểm
                    if (!int.TryParse(reader.ReadLine(), out int score)) return false;
                    this.CurrentScore = score;

                    // 2. Đọc lưới bàn cờ 8x8
                    int[,] newGrid = new int[BOARD_SIZE, BOARD_SIZE];
                    for (int r = 0; r < BOARD_SIZE; r++)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line)) return false;
                        string[] parts = line.Split(',');
                        for (int c = 0; c < BOARD_SIZE; c++)
                        {
                            if (int.TryParse(parts[c], out int val))
                            {
                                newGrid[r, c] = val;
                            }
                        }
                    }
                    this.Board.SetGrid(newGrid);

                    // 3. Đọc chi tiết 3 ô Dock và phục hồi block
                    Panel[] panels = { pnl1, pnl2, pnl3 };
                    foreach (var p in panels) { p.Tag = null; p.Visible = false; }

                    for (int i = 0; i < 3; i++)
                    {
                        string slotHeader = reader.ReadLine();
                        string dimLine = reader.ReadLine();

                        if (string.IsNullOrEmpty(slotHeader) || string.IsNullOrEmpty(dimLine)) break;

                        string[] headerParts = slotHeader.Split(':');
                        if (headerParts.Length < 4) continue;

                        int slotIdx = int.Parse(headerParts[1]);
                        bool isVisible = bool.Parse(headerParts[2]);
                        int colorIndex = int.Parse(headerParts[3]);

                        string[] dims = dimLine.Split(',');
                        int rows = int.Parse(dims[0]);
                        int cols = int.Parse(dims[1]);

                        int[,] shape = null;
                        if (rows > 0 && cols > 0)
                        {
                            shape = new int[rows, cols];
                            for (int r = 0; r < rows; r++)
                            {
                                string rowLine = reader.ReadLine();
                                if (string.IsNullOrEmpty(rowLine)) break;
                                string[] vals = rowLine.Split(',');
                                for (int c = 0; c < cols; c++)
                                {
                                    if (int.TryParse(vals[c], out int v))
                                    {
                                        shape[r, c] = v;
                                    }
                                }
                            }
                        }

                        if (slotIdx >= 0 && slotIdx < panels.Length)
                        {
                            if (isVisible && shape != null && colorIndex > 0)
                            {
                                panels[slotIdx].Tag = new BlockPiece(shape, colorIndex);
                                panels[slotIdx].Visible = true;
                            }
                            else
                            {
                                panels[slotIdx].Tag = null;
                                panels[slotIdx].Visible = false;
                            }
                            panels[slotIdx].Invalidate();
                        }
                    }
                }

                // Xóa file save sau khi load thành công để tránh load trùng lặp
                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}