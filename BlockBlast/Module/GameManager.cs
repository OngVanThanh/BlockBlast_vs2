using BlockBlast.Module;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BlockBlast
{
    // Cấu trúc mô hình dữ liệu JSON cho trạng thái game
    public class GameSaveModel
    {
        public int CurrentScore { get; set; }
        public int HighScore { get; set; }
        public bool IsNewGame { get; set; } // true: ván mới hoàn toàn -> ẩn nút Tiếp tục; false: đang chơi dở -> hiện nút Tiếp tục
        public int[][] BoardMatrix { get; set; }
        public List<DockPieceModel> DockPieces { get; set; }
    }

    public class DockPieceModel
    {
        public bool IsVisible { get; set; }
        public int ColorIndex { get; set; }
        public int[][] ShapeMatrix { get; set; }
    }

    public class GameManager
    {
        // === Các biến toàn cục / thuộc tính quản lý trạng thái game ===
        public GameBoard Board { get; private set; }
        public int CurrentScore { get; private set; }
        public int HighScore { get; set; }
        public bool IsPaused { get; set; }
        public bool IsNewGame { get; set; } = true;

        // Bộ đệm trạng thái lưu trữ bàn cờ và dock
        public int[][] SavedBoardMatrix { get; set; }
        public List<DockPieceModel> SavedDockPieces { get; set; }

        // === Kho chứa 5 loại Item ===
        public Dictionary<ItemType, Item> ItemsInventory { get; private set; }
        private readonly Random rand = new Random();
        public const int BOARD_SIZE = 8;

        public GameManager()
        {
            Board = new GameBoard();
            CurrentScore = 0;
            HighScore = 0;
            // === Khởi tạo và nạp file items.json ===
            InitItems();
            LoadItemsJson();
        }

        // ================================================================
        //  1. SINH KHỐI THEO MỨC ĐIỂM & ĐẢM BẢO TÍNH TUẦN TỰ (SEQUENTIAL SOLVABILITY)
        // ================================================================

        public BlockPiece CreateRandomPiece()
        {
            int shapeIndex = GetWeightedShapeIndex(CurrentScore);
            int[,] shape = BlockPiece.AllShapes[shapeIndex];
            int colorIndex = rand.Next(1, 8); // Màu ngẫu nhiên từ 1 đến 7
            return new BlockPiece(shape, colorIndex);
        }

        // Danh sách chỉ số (Index) trong BlockPiece.AllShapes chia chuẩn theo 3 mức độ khó:
        // - Nhóm 1 (Dễ - 1 đến 3 ô): 1x1, 1x2, 2x1, 1x3, 3x1, góc L nhỏ 2x2
        private static readonly int[] EasyShapeIndices = { 0, 1, 2, 3, 4, 11, 12, 13, 14 };
        // - Nhóm 2 (Vừa - 4 ô): 1x4, 4x1, vuông 2x2, chữ T, chữ Z/S
        private static readonly int[] MediumShapeIndices = { 5, 6, 9, 19, 20, 21, 22, 23, 24, 25, 26 };
        private static readonly int[] HardShapeIndices = { 7, 8, 10, 15, 16, 17, 18 };
        // - Nhóm 3 (Khó/Hiểm - 5 đến 9 ô): 1x5, 5x1, vuông đại 3x3, chữ L đại 3x3

        private int GetWeightedShapeIndex(int score)
        {
            double roll = rand.NextDouble();

            if (score < 1500)
            {
                // Dưới 1500đ: 60% Dễ - 30% Vừa - 10% Khó
                if (roll < 0.60) return GetRandomShapeFromCategory(1);
                if (roll < 0.90) return GetRandomShapeFromCategory(2);
                return GetRandomShapeFromCategory(3);
            }
            else if (score < 3000)
            {
                // 1500 - 3000đ: 35% Dễ - 45% Vừa - 20% Khó
                if (roll < 0.35) return GetRandomShapeFromCategory(1);
                if (roll < 0.80) return GetRandomShapeFromCategory(2);
                return GetRandomShapeFromCategory(3);
            }
            else if (score < 5000)
            {
                // 3000 - 5000đ: 15% Dễ - 45% Vừa - 40% Khó
                if (roll < 0.15) return GetRandomShapeFromCategory(1);
                if (roll < 0.60) return GetRandomShapeFromCategory(2);
                return GetRandomShapeFromCategory(3);
            }
            else
            {
                // Trên 5000đ: 5% Dễ - 35% Vừa - 60% Khó (Ép dùng Item hoặc thua nếu xếp bừa)
                if (roll < 0.05) return GetRandomShapeFromCategory(1);
                if (roll < 0.40) return GetRandomShapeFromCategory(2);
                return GetRandomShapeFromCategory(3);
            }
        }

        private int GetRandomShapeFromCategory(int category)
        {
            if (category == 1)
                return EasyShapeIndices[rand.Next(EasyShapeIndices.Length)];
            if (category == 2)
                return MediumShapeIndices[rand.Next(MediumShapeIndices.Length)];
            return HardShapeIndices[rand.Next(HardShapeIndices.Length)];
        }

        public bool CanPlacePiece(BlockPiece piece)
        {
            if (piece == null || piece.Shape == null) return false;

            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (Board.CanPlaceShape(piece.Shape, r, c))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Kiểm tra xem 3 khối có thể đặt theo thứ tự tuần tự (Khối 1 -> Khối 2 -> Khối 3) với hình dạng gốc hay không
        public bool CanPlaceSequentialOrder(List<BlockPiece> pieces, int[] order)
        {
            if (pieces == null || pieces.Count != 3 || order == null || order.Length != 3) return false;

            int[,] tempGrid = CloneGrid(Board.GetGrid());

            for (int i = 0; i < 3; i++)
            {
                int pieceIdx = order[i];
                var piece = pieces[pieceIdx];
                if (piece == null || piece.Shape == null) return false;

                bool placedThisPiece = false;
                for (int r = 0; r < BOARD_SIZE; r++)
                {
                    for (int c = 0; c < BOARD_SIZE; c++)
                    {
                        if (CanFitShapeOnGrid(piece.Shape, r, c, tempGrid))
                        {
                            PlaceShapeOnGrid(piece.Shape, r, c, tempGrid);
                            placedThisPiece = true;
                            break;
                        }
                    }
                    if (placedThisPiece) break;
                }

                if (!placedThisPiece) return false;
            }

            return true;
        }

        private bool CanFitShapeOnGrid(int[,] shape, int startRow, int startCol, int[,] grid)
        {
            int rows = shape.GetLength(0);
            int cols = shape.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (shape[r, c] != 0)
                    {
                        int tr = startRow + r;
                        int tc = startCol + c;
                        if (tr < 0 || tr >= BOARD_SIZE || tc < 0 || tc >= BOARD_SIZE) return false;
                        if (grid[tr, tc] != 0) return false;
                    }
                }
            }
            return true;
        }

        private void PlaceShapeOnGrid(int[,] shape, int startRow, int startCol, int[,] grid)
        {
            int rows = shape.GetLength(0);
            int cols = shape.GetLength(1);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (shape[r, c] != 0)
                    {
                        grid[startRow + r, startCol + c] = 1;
                    }
                }
            }
        }

        private int[,] CloneGrid(int[,] src)
        {
            int[,] dest = new int[BOARD_SIZE, BOARD_SIZE];
            for (int r = 0; r < BOARD_SIZE; r++)
                for (int c = 0; c < BOARD_SIZE; c++)
                    dest[r, c] = src[r, c];
            return dest;
        }

        // Sinh 3 khối mới: Đảm bảo có ít nhất một thứ tự tuần tự giải được bằng hình dạng gốc, sau đó xáo trộn vị trí ngẫu nhiên trên Dock
        public List<BlockPiece> GenerateValidPieceSet()
        {
            List<List<BlockPiece>> validCombinations = new List<List<BlockPiece>>();

            // Giảm dần số lần thử "cứu trợ" theo mức điểm
            int samplePoolSize = CurrentScore < 1500 ? 60 :
                                 CurrentScore < 3000 ? 30 :
                                 CurrentScore < 5000 ? 10 : 1;

            int[][] possibleOrders = new int[][]
            {
                new int[] { 0, 1, 2 },
                new int[] { 0, 2, 1 },
                new int[] { 1, 0, 2 },
                new int[] { 1, 2, 0 },
                new int[] { 2, 0, 1 },
                new int[] { 2, 1, 0 }
            };

            for (int i = 0; i < samplePoolSize; i++)
            {
                List<BlockPiece> candidateSet = new List<BlockPiece>
                {
                    CreateRandomPiece(),
                    CreateRandomPiece(),
                    CreateRandomPiece()
                };

                foreach (var order in possibleOrders)
                {
                    if (CanPlaceSequentialOrder(candidateSet, order))
                    {
                        validCombinations.Add(candidateSet);
                        break;
                    }
                }
            }

            if (validCombinations.Count > 0)
            {
                List<BlockPiece> selectedSet = validCombinations[rand.Next(validCombinations.Count)];

                // Xáo trộn vị trí ngẫu nhiên trên Dock
                for (int idx = selectedSet.Count - 1; idx > 0; idx--)
                {
                    int k = rand.Next(idx + 1);
                    var temp = selectedSet[idx];
                    selectedSet[idx] = selectedSet[k];
                    selectedSet[k] = temp;
                }
                return selectedSet;
            }

            // BỎ fallback [1x1, 1x2, 2x1]: Khi bàn cờ quá chật hoặc điểm cao, sinh thẳng theo trọng số độ khó!
            // Người chơi bắt buộc phải dùng các Item phá gạch đang có, nếu hết Item sẽ thua.
            return new List<BlockPiece>
            {
                CreateRandomPiece(),
                CreateRandomPiece(),
                CreateRandomPiece()
            };
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

        public bool CheckGameOverWithItems(List<BlockPiece> remainingPieces)
        {
            // Kiểm tra xem các khối gạch còn lại có đặt được lên bàn cờ hay không
            if (remainingPieces != null && remainingPieces.Count > 0)
            {
                foreach (var piece in remainingPieces)
                {
                    // Nếu còn ít nhất một khối có thể đặt lên bàn cờ thì chưa Game Over
                    if (CanPlacePiece(piece)) return false;
                }
            }
            else
            {
                // Nếu không có khối gạch nào trong danh sách thì không tính là thua
                return false;
            }

            // Nếu không thể đặt bất kỳ khối nào lên bàn cờ, tiếp tục kiểm tra các vật phẩm (Items) có thể cứu nguy
            int usableItems = 0;
            if (ItemsInventory != null)
            {
                foreach (var kvp in ItemsInventory)
                {
                    // Nếu cả 3 khay đều đang đầy gạch thì Item Đổi Gạch không thể dùng để cứu nguy được
                    if (kvp.Key == ItemType.RerollDock && remainingPieces.Count >= 3)
                        continue;

                    usableItems += kvp.Value.Count;
                }
            }

            // Game Over thực sự khi không đặt được gạch VÀ cũng không còn vật phẩm nào khả dụng
            return usableItems == 0;
        }

        // ================================================================
        //  2. LƯU & TẢI TRẠNG THÁI GAME QUA FILE JSON (A[i][j])
        // ================================================================

        private string GetSaveGameFilePath()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(folder, "gamestate.json");
        }

        // BỔ SUNG: Kiểm tra có ván chơi đang lưu hay không
        public bool HasSavedGame => File.Exists(GetSaveGameFilePath());

        // BỔ SUNG: Xóa file lưu khi Game Over hoặc Chơi Lại mới
        public void DeleteSavedGame()
        {
            try
            {
                string path = GetSaveGameFilePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
        public void SaveGameState(Panel pnl1, Panel pnl2, Panel pnl3, bool isGameOver)
        {
            try
            {
                // === BƯỚC 1: CẬP NHẬT TRẠNG THÁI VÀO CÁC BIẾN TOÀN CỤC ===
                if (isGameOver)
                {
                    // Trường hợp Game Over: Reset trạng thái
                    this.IsNewGame = true;
                    this.CurrentScore = 0; // Reset điểm hiện tại về 0

                    // Tạo ma trận bàn cờ toàn số 0 (Trống) cho biến toàn cục
                    SavedBoardMatrix = new int[BOARD_SIZE][];
                    for (int r = 0; r < BOARD_SIZE; r++)
                    {
                        SavedBoardMatrix[r] = new int[BOARD_SIZE];
                        for (int c = 0; c < BOARD_SIZE; c++)
                        {
                            SavedBoardMatrix[r][c] = 0;
                        }
                    }

                    // Xóa sạch 3 ô Dock cho biến toàn cục
                    SavedDockPieces = new List<DockPieceModel>();
                    for (int i = 0; i < 3; i++)
                    {
                        SavedDockPieces.Add(new DockPieceModel
                        {
                            IsVisible = false,
                            ColorIndex = 1,
                            ShapeMatrix = new int[0][]
                        });
                    }
                }
                else
                {
                    // Trường hợp Thoát giữa chừng / Về Menu: Giữ nguyên trạng thái chơi dở
                    this.IsNewGame = false;
                    // CurrentScore đã được cập nhật liên tục trong quá trình chơi

                    // Lưu ma trận bàn cờ hiện tại vào biến toàn cục SavedBoardMatrix
                    int[,] grid = GetGameBoardMatrix(this.Board);
                    SavedBoardMatrix = new int[BOARD_SIZE][];
                    for (int r = 0; r < BOARD_SIZE; r++)
                    {
                        SavedBoardMatrix[r] = new int[BOARD_SIZE];
                        for (int c = 0; c < BOARD_SIZE; c++)
                        {
                            SavedBoardMatrix[r][c] = grid[r, c];
                        }
                    }

                    // Lưu trạng thái 3 ô Dock hiện tại vào biến toàn cục SavedDockPieces
                    SavedDockPieces = new List<DockPieceModel>();
                    Panel[] panels = { pnl1, pnl2, pnl3 };
                    foreach (var panel in panels)
                    {
                        var dockModel = new DockPieceModel();
                        if (panel != null && panel.Visible && panel.Tag is BlockPiece piece)
                        {
                            dockModel.IsVisible = true;
                            dockModel.ColorIndex = piece.ColorIndex;
                            int[,] shape = piece.Shape;
                            int rows = shape.GetLength(0);
                            int cols = shape.GetLength(1);
                            dockModel.ShapeMatrix = new int[rows][];
                            for (int r = 0; r < rows; r++)
                            {
                                dockModel.ShapeMatrix[r] = new int[cols];
                                for (int c = 0; c < cols; c++)
                                {
                                    dockModel.ShapeMatrix[r][c] = shape[r, c];
                                }
                            }
                        }
                        else
                        {
                            dockModel.IsVisible = false;
                            dockModel.ColorIndex = 1;
                            dockModel.ShapeMatrix = new int[0][];
                        }
                        SavedDockPieces.Add(dockModel);
                    }
                }

                // === BƯỚC 2: DỰA TRÊN CÁC BIẾN TOÀN CỤC ĐỂ LƯU VÀO FILE STATE GAME ===
                var model = new GameSaveModel();
                model.HighScore = this.HighScore;
                model.CurrentScore = this.CurrentScore;
                model.IsNewGame = this.IsNewGame;
                model.BoardMatrix = SavedBoardMatrix;
                model.DockPieces = SavedDockPieces;

                // Ghi xuống file gamestate.json
                string path = GetSaveGameFilePath();
                string jsonString = SimpleJsonSerialize(model);
                File.WriteAllText(path, jsonString);
            }
            catch
            {
                // Bắt lỗi ngầm tránh crash ứng dụng
            }
        }

        public bool LoadGameState(Panel pnl1, Panel pnl2, Panel pnl3)
        {
            try
            {
                string path = GetSaveGameFilePath();
                if (!File.Exists(path)) return false;

                string jsonString = File.ReadAllText(path);
                var model = SimpleJsonDeserialize(jsonString);
                if (model == null) return false;

                // === BƯỚC 1: LƯU DỮ LIỆU ĐỌC ĐƯỢC VÀO CÁC BIẾN TOÀN CỤC ===
                this.CurrentScore = model.CurrentScore;
                this.HighScore = model.HighScore;
                this.IsNewGame = model.IsNewGame;

                // Nếu là game mới hoặc dữ liệu trống thì không load bàn cờ/dock
                if (this.IsNewGame) return false;

                // Lưu ma trận bàn cờ vào biến toàn cục SavedBoardMatrix
                if (model.BoardMatrix != null)
                {
                    int rows = model.BoardMatrix.Length;
                    SavedBoardMatrix = new int[rows][];
                    int[,] grid = new int[rows, rows > 0 ? model.BoardMatrix[0].Length : BOARD_SIZE];

                    for (int r = 0; r < rows; r++)
                    {
                        int cols = model.BoardMatrix[r].Length;
                        SavedBoardMatrix[r] = new int[cols];
                        for (int c = 0; c < cols; c++)
                        {
                            SavedBoardMatrix[r][c] = model.BoardMatrix[r][c];
                            if (r < BOARD_SIZE && c < BOARD_SIZE)
                            {
                                grid[r, c] = model.BoardMatrix[r][c];
                            }
                        }
                    }

                    // Đưa ma trận vào đối tượng Board hiện tại
                    if (this.Board != null && this.Board.GetType().GetMethod("SetGrid") != null)
                    {
                        this.Board.GetType().GetMethod("SetGrid").Invoke(this.Board, new object[] { grid });
                    }
                }

                // Lưu trạng thái 3 ô dock vào biến toàn cục SavedDockPieces và khôi phục giao diện
                SavedDockPieces = new List<DockPieceModel>();
                Panel[] panels = { pnl1, pnl2, pnl3 };

                for (int i = 0; i < panels.Length && i < model.DockPieces.Count; i++)
                {
                    var panel = panels[i];
                    var dockModel = model.DockPieces[i];

                    // Lưu bản sao vào biến toàn cục
                    SavedDockPieces.Add(dockModel);

                    if (panel != null)
                    {
                        if (dockModel.IsVisible && dockModel.ShapeMatrix != null && dockModel.ShapeMatrix.Length > 0)
                        {
                            int sRows = dockModel.ShapeMatrix.Length;
                            int sCols = sRows > 0 ? dockModel.ShapeMatrix[0].Length : 0;
                            int[,] shape2D = new int[sRows, sCols];

                            for (int r = 0; r < sRows; r++)
                            {
                                for (int c = 0; c < dockModel.ShapeMatrix[r].Length && c < sCols; c++)
                                {
                                    shape2D[r, c] = dockModel.ShapeMatrix[r][c];
                                }
                            }

                            BlockPiece piece = new BlockPiece(shape2D, dockModel.ColorIndex);
                            panel.Tag = piece;
                            panel.Visible = true;
                            panel.Invalidate();
                        }
                        else
                        {
                            panel.Tag = null;
                            panel.Visible = false;
                            panel.Invalidate();
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ================================================================
        //  3. ĐỊNH DẠNG JSON CHUẨN ĐẸP (FORMATTED JSON SERIALIZE)
        // ================================================================
        private int[,] GetGameBoardMatrix(GameBoard board)
        {
            if (board != null)
            {
                // Nếu lớp Board của bạn dùng hàm lấy mảng là GetGrid() hoặc thuộc tính Grid, 
                // hãy chắc chắn tên gọi khớp với code Board hiện tại của bạn.
                return board.GetGrid();
            }
            return new int[BOARD_SIZE, BOARD_SIZE];
        }
        private string SimpleJsonSerialize(GameSaveModel data)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"CurrentScore\": {data.CurrentScore},");
            sb.AppendLine($"  \"HighScore\": {data.HighScore},");
            sb.AppendLine($"  \"IsNewGame\": {data.IsNewGame.ToString().ToLower()},");

            sb.AppendLine("  \"BoardMatrix\": [");
            for (int r = 0; r < data.BoardMatrix.Length; r++)
            {
                string rowStr = "    [" + string.Join(",", data.BoardMatrix[r]) + "]";
                if (r < data.BoardMatrix.Length - 1) rowStr += ",";
                sb.AppendLine(rowStr);
            }
            sb.AppendLine("  ],");

            sb.AppendLine("  \"DockPieces\": [");
            for (int i = 0; i < data.DockPieces.Count; i++)
            {
                var d = data.DockPieces[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"IsVisible\": {d.IsVisible.ToString().ToLower()},");
                sb.AppendLine($"      \"ColorIndex\": {d.ColorIndex},");

                string shapeStr;
                if (d.ShapeMatrix != null && d.ShapeMatrix.Length > 0)
                {
                    var rows = d.ShapeMatrix.Select(row => "[" + string.Join(",", row) + "]");
                    shapeStr = "[" + string.Join(",", rows) + "]";
                }
                else
                {
                    shapeStr = "[]";
                }
                sb.AppendLine($"      \"ShapeMatrix\": {shapeStr}");

                if (i < data.DockPieces.Count - 1)
                    sb.AppendLine("    },");
                else
                    sb.AppendLine("    }");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public GameSaveModel SimpleJsonDeserialize(string json)
        {
            try
            {
                var model = new GameSaveModel();

                int scoreIdx = json.IndexOf("CurrentScore");
                if (scoreIdx != -1)
                {
                    int colon = json.IndexOf(':', scoreIdx);
                    int comma = json.IndexOf(',', colon);
                    if (comma == -1) comma = json.IndexOf('}', colon);
                    int.TryParse(json.Substring(colon + 1, comma - colon - 1).Trim(), out int sc);
                    model.CurrentScore = sc;
                }

                int hsIdx = json.IndexOf("HighScore");
                if (hsIdx != -1)
                {
                    int colon = json.IndexOf(':', hsIdx);
                    int comma = json.IndexOf(',', colon);
                    if (comma == -1) comma = json.IndexOf('}', colon);
                    int.TryParse(json.Substring(colon + 1, comma - colon - 1).Trim(), out int hs);
                    model.HighScore = hs;
                }

                int newGameIdx = json.IndexOf("IsNewGame");
                if (newGameIdx != -1)
                {
                    int colon = json.IndexOf(':', newGameIdx);
                    int comma = json.IndexOf(',', colon);
                    if (comma == -1) comma = json.IndexOf('}', colon);
                    bool.TryParse(json.Substring(colon + 1, comma - colon - 1).Trim(), out bool isNew);
                    model.IsNewGame = isNew;
                }
                else
                {
                    model.IsNewGame = true;
                }

                model.BoardMatrix = new int[BOARD_SIZE][];
                for (int i = 0; i < BOARD_SIZE; i++) model.BoardMatrix[i] = new int[BOARD_SIZE];

                int boardKey = json.IndexOf("BoardMatrix");
                if (boardKey != -1)
                {
                    int startBracket = json.IndexOf('[', boardKey);
                    int endBracket = FindMatchingCloseBracket(json, startBracket);
                    if (startBracket != -1 && endBracket != -1)
                    {
                        string boardContent = json.Substring(startBracket + 1, endBracket - startBracket - 1);
                        int r = 0;
                        int idx = 0;
                        while (r < BOARD_SIZE && idx < boardContent.Length)
                        {
                            int rowStart = boardContent.IndexOf('[', idx);
                            if (rowStart == -1) break;
                            int rowEnd = boardContent.IndexOf(']', rowStart);
                            if (rowEnd == -1) break;

                            string rowVals = boardContent.Substring(rowStart + 1, rowEnd - rowStart - 1);
                            string[] nums = rowVals.Split(',');
                            for (int c = 0; c < BOARD_SIZE && c < nums.Length; c++)
                            {
                                int.TryParse(nums[c].Trim(), out int val);
                                model.BoardMatrix[r][c] = val;
                            }
                            idx = rowEnd + 1;
                            r++;
                        }
                    }
                }

                model.DockPieces = new List<DockPieceModel>();
                int dockKey = json.IndexOf("DockPieces");
                if (dockKey != -1)
                {
                    int dockStart = json.IndexOf('[', dockKey);
                    int dockEnd = FindMatchingCloseBracket(json, dockStart);
                    if (dockStart != -1 && dockEnd != -1)
                    {
                        string dockContent = json.Substring(dockStart + 1, dockEnd - dockStart - 1);
                        int searchIdx = 0;
                        while (true)
                        {
                            int objStart = dockContent.IndexOf('{', searchIdx);
                            if (objStart == -1) break;
                            int objEnd = FindMatchingCloseBracket(dockContent, objStart);
                            if (objEnd == -1) break;

                            string objStr = dockContent.Substring(objStart + 1, objEnd - objStart - 1);
                            var dockModel = new DockPieceModel();

                            dockModel.IsVisible = objStr.Contains("\"IsVisible\": true");

                            int colorKey = objStr.IndexOf("ColorIndex");
                            if (colorKey != -1)
                            {
                                int cColon = objStr.IndexOf(':', colorKey);
                                int cComma = objStr.IndexOf(',', cColon);
                                if (cComma == -1) cComma = objStr.Length;
                                int.TryParse(objStr.Substring(cColon + 1, cComma - cColon - 1).Trim(), out int colIdx);
                                dockModel.ColorIndex = colIdx;
                            }

                            int shapeKey = objStr.IndexOf("ShapeMatrix");
                            if (shapeKey != -1)
                            {
                                int sStart = objStr.IndexOf('[', shapeKey);
                                int sEnd = FindMatchingCloseBracket(objStr, sStart);
                                if (sStart != -1 && sEnd != -1)
                                {
                                    string shapeMatrixStr = objStr.Substring(sStart + 1, sEnd - sStart - 1).Trim();
                                    List<int[]> rowsList = new List<int[]>();
                                    int rIdx = 0;
                                    while (true)
                                    {
                                        int rStart = shapeMatrixStr.IndexOf('[', rIdx);
                                        if (rStart == -1 || rStart > sEnd) break;
                                        int rEndActual = shapeMatrixStr.IndexOf(']', rStart);
                                        if (rEndActual == -1) break;

                                        string rVals = shapeMatrixStr.Substring(rStart + 1, rEndActual - rStart - 1);
                                        string[] rNums = rVals.Split(',');
                                        List<int> rowValues = new List<int>();
                                        foreach (var num in rNums)
                                        {
                                            if (int.TryParse(num.Trim(), out int nVal))
                                                rowValues.Add(nVal);
                                        }
                                        if (rowValues.Count > 0) rowsList.Add(rowValues.ToArray());
                                        rIdx = rEndActual + 1;
                                    }

                                    if (rowsList.Count > 0)
                                    {
                                        int totalRows = rowsList.Count;
                                        int[][] finalShape = new int[totalRows][];
                                        for (int ri = 0; ri < totalRows; ri++) finalShape[ri] = rowsList[ri];
                                        dockModel.ShapeMatrix = finalShape;
                                    }
                                    else
                                    {
                                        dockModel.ShapeMatrix = new int[0][];
                                    }
                                }
                            }

                            model.DockPieces.Add(dockModel);
                            searchIdx = objEnd + 1;
                        }
                    }
                }

                while (model.DockPieces.Count < 3)
                {
                    model.DockPieces.Add(new DockPieceModel { IsVisible = false, ColorIndex = 1, ShapeMatrix = new int[0][] });
                }

                return model;
            }
            catch
            {
                return null;
            }
        }

        private int FindMatchingCloseBracket(string text, int openIdx)
        {
            int depth = 0;
            for (int i = openIdx; i < text.Length; i++)
            {
                if (text[i] == '[') depth++;
                else if (text[i] == ']')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
        // ================================================================
        //  4. HỆ THỐNG QUẢN LÝ ITEM & FILE items.json
        // ================================================================

        // Đếm số lần rung chấn bàn cờ từ các Combo lớn (Chữ thập, 3 dòng, 4 dòng) để thưởng Trọng Lực
        public int BoardShakeCounter { get; set; } = 0;

        // Khởi tạo sẵn 5 món đồ trong kho với số lượng ban đầu là 0
        private void InitItems()
        {
            ItemsInventory = new Dictionary<ItemType, Item>
            {
                { ItemType.RerollDock, new Item(ItemType.RerollDock, "Đổi Gạch", "Thêm 1 gạch mới vào khay trống", Color.FromArgb(52, 152, 219)) },
                { ItemType.Bomb3x3, new Item(ItemType.Bomb3x3, "Bom 3x3", "Kéo đà ném nổ vùng 3x3", Color.FromArgb(231, 76, 60)) },
                { ItemType.CrossClear, new Item(ItemType.CrossClear, "Tia Chữ Thập", "Kéo đà nổ hàng và cột", Color.FromArgb(241, 196, 15)) },
                { ItemType.ColorLightning, new Item(ItemType.ColorLightning, "Sấm Sét", "Ngắm vào 1 ô để quét sạch các ô cùng màu", Color.FromArgb(155, 89, 182)) },
                { ItemType.GravityFall, new Item(ItemType.GravityFall, "Trọng Lực", "Kéo đà dồn các khối rơi xuống đáy", Color.FromArgb(46, 204, 113)) }
            };
        }

        // Thưởng Item theo quy tắc mới:
        // - 2 dòng song song: Đổi Gạch (không rung bàn cờ)
        // - 2 dòng chữ thập (1 ngang + 1 dọc): Tia Chữ Thập (+1 Rung Chấn)
        // - 3 dòng: Bom 3x3 (+1 Rung Chấn)
        // - 4 dòng: Sấm Sét (+1 Rung Chấn)
        // - Đủ 3 lần Rung Chấn: Thưởng thêm Trọng Lực
        public Item AwardComboItem(int rowsCleared, int colsCleared, out bool triggeredShake, out bool awardedGravity)
        {
            triggeredShake = false;
            awardedGravity = false;

            int totalCleared = rowsCleared + colsCleared;
            if (totalCleared < 2) return null;

            bool isCross = (rowsCleared >= 1 && colsCleared >= 1);
            ItemType rewardType;

            if (totalCleared == 2)
            {
                if (isCross)
                {
                    // Nổ 2 hàng chữ thập (1 hàng + 1 cột) -> Tia Chữ Thập + tính Rung Chấn
                    rewardType = ItemType.CrossClear;
                    triggeredShake = true;
                }
                else
                {
                    // Nổ 2 hàng đôi song song -> Đổi Gạch (không tính Rung Chấn vì dễ)
                    rewardType = ItemType.RerollDock;
                    triggeredShake = false;
                }
            }
            else if (totalCleared == 3)
            {
                // Nổ 3 dòng -> Bom 3x3 + tính Rung Chấn
                rewardType = ItemType.Bomb3x3;
                triggeredShake = true;
            }
            else
            {
                // Nổ 4 dòng trở lên -> Sấm Sét + tính Rung Chấn
                rewardType = ItemType.ColorLightning;
                triggeredShake = true;
            }

            Item rewarded = null;
            if (ItemsInventory.ContainsKey(rewardType))
            {
                ItemsInventory[rewardType].Add(1);
                rewarded = ItemsInventory[rewardType];
            }

            // Tích lũy Rung Chấn từ Combo Chữ Thập / 3 dòng / 4 dòng
            if (triggeredShake)
            {
                BoardShakeCounter++;
                if (BoardShakeCounter >= 3)
                {
                    BoardShakeCounter = 0;
                    if (ItemsInventory.ContainsKey(ItemType.GravityFall))
                    {
                        ItemsInventory[ItemType.GravityFall].Add(1);
                        awardedGravity = true;
                    }
                }
            }

            SaveItemsJson();
            return rewarded;
        }

        // Thuật toán tìm khối gạch tối ưu nhất cho bàn cờ hiện tại:
        // 1. Khối làm nổ được nhiều hàng/cột nhất có thể (Line Clear)
        // 2. Khối lấp vào các hàng/cột sắp đầy (7/8 ô)
        // 3. Khối nhỏ gọn (1x1, 1x2, 2x1) dễ đặt, giải tỏa diện tích bàn cờ
        public BlockPiece CreatePieceForEmptySlot()
        {
            int[,] currentGrid = Board.GetGrid();
            int bestScore = -1;
            int[,] bestShape = null;

            // Quét qua tất cả các hình dạng chuẩn trong game
            foreach (var shape in BlockPiece.AllShapes)
            {
                int rows = shape.GetLength(0);
                int cols = shape.GetLength(1);

                int cellCount = 0;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        if (shape[r, c] != 0) cellCount++;

                // Thử tất cả vị trí đặt hợp lệ trên bàn cờ 8x8
                for (int r = 0; r <= BOARD_SIZE - rows; r++)
                {
                    for (int c = 0; c <= BOARD_SIZE - cols; c++)
                    {
                        if (Board.CanPlaceShape(shape, r, c))
                        {
                            int moveScore = EvaluateMoveScore(currentGrid, shape, r, c, cellCount);
                            if (moveScore > bestScore)
                            {
                                bestScore = moveScore;
                                bestShape = shape;
                            }
                        }
                    }
                }
            }

            // Nếu tìm thấy khối tối ưu nhất
            if (bestShape != null)
            {
                int color = rand.Next(1, 8);
                return new BlockPiece(bestShape, color);
            }

            // Dự phòng bàn cờ cực kỳ chật hẹp: trả về khối đơn 1x1 cứu nguy
            return new BlockPiece(new int[,] { { 1 } }, rand.Next(1, 8));
        }

        // Đánh giá điểm chiến lược của một nước đi giả lập
        private int EvaluateMoveScore(int[,] currentGrid, int[,] shape, int startR, int startC, int cellCount)
        {
            int rows = shape.GetLength(0);
            int cols = shape.GetLength(1);

            // Tạo bản sao bàn cờ để đặt thử
            int[,] temp = (int[,])currentGrid.Clone();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (shape[r, c] != 0)
                    {
                        temp[startR + r, startC + c] = 1;
                    }
                }
            }

            // 1. Đếm số dòng/cột sẽ bị nổ
            int clearedLines = 0;
            bool[] fullRows = new bool[BOARD_SIZE];
            bool[] fullCols = new bool[BOARD_SIZE];

            for (int r = 0; r < BOARD_SIZE; r++)
            {
                bool full = true;
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (temp[r, c] == 0) { full = false; break; }
                }
                if (full) { fullRows[r] = true; clearedLines++; }
            }

            for (int c = 0; c < BOARD_SIZE; c++)
            {
                bool full = true;
                for (int r = 0; r < BOARD_SIZE; r++)
                {
                    if (temp[r, c] == 0) { full = false; break; }
                }
                if (full) { fullCols[c] = true; clearedLines++; }
            }

            int score = 0;

            if (clearedLines > 0)
            {
                // ƯU TIÊN SỐ 1 TUYỆT ĐỐI: Phá nổ dòng (càng nhiều dòng nổ điểm càng áp đảo)
                score += 10000 * clearedLines;
            }
            else
            {
                // ƯU TIÊN SỐ 2: Lấp vào những hàng/cột sắp đầy (7/8 ô)
                int nearFullCount = 0;
                for (int r = 0; r < BOARD_SIZE; r++)
                {
                    int filled = 0;
                    for (int c = 0; c < BOARD_SIZE; c++) if (temp[r, c] != 0) filled++;
                    if (filled == BOARD_SIZE - 1) nearFullCount++;
                }
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    int filled = 0;
                    for (int r = 0; r < BOARD_SIZE; r++) if (temp[r, c] != 0) filled++;
                    if (filled == BOARD_SIZE - 1) nearFullCount++;
                }
                score += 250 * nearFullCount;

                // ƯU TIÊN SỐ 3: Khối nhỏ gọn ít chiếm diện tích (1x1, 1x2, góc L nhỏ)
                score += (10 - cellCount) * 20;
            }

            // Cộng thêm điểm cho số ô trống còn lại sau khi nổ để bàn cờ thông thoáng nhất
            int emptyCells = 0;
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (fullRows[r] || fullCols[c] || temp[r, c] == 0)
                    {
                        emptyCells++;
                    }
                }
            }
            score += emptyCells * 5;

            return score;
        }

        // Xóa sạch toàn bộ Item về 0 (dùng khi Reset lịch sử chơi)
        public void ResetAllItems()
        {
            BoardShakeCounter = 0;
            if (ItemsInventory != null)
            {
                foreach (var item in ItemsInventory.Values)
                {
                    item.Count = 0;
                }
            }
            SaveItemsJson();
        }

        // Tạo khối nhỏ may mắn (1 đến 4 ô) cho Item 1 (Copy / Lucky Piece)
        public BlockPiece GenerateLuckyPiece()
        {
            // Các khối nhỏ dễ đặt: 1x1, 1x2, 2x1, 1x3, 3x1, 2x2, chữ L nhỏ
            List<int[,]> smallShapes = new List<int[,]>
            {
                new int[,] { { 1 } },                     // 1 ô
                new int[,] { { 1, 1 } },                  // 2 ô ngang
                new int[,] { { 1 }, { 1 } },              // 2 ô dọc
                new int[,] { { 1, 1, 1 } },               // 3 ô ngang
                new int[,] { { 1 }, { 1 }, { 1 } },       // 3 ô dọc
                new int[,] { { 1, 1 }, { 1, 1 } },        // 4 ô vuông
                new int[,] { { 1, 0 }, { 1, 1 } },        // 3 ô góc L
                new int[,] { { 1, 1, 1, 1 } }             // 4 ô thẳng
            };

            int index = rand.Next(smallShapes.Count);
            int color = rand.Next(1, 8);
            return new BlockPiece(smallShapes[index], color);
        }

        // Đếm tổng số lượng tất cả Item còn trong kho
        public int GetTotalItemCount()
        {
            if (ItemsInventory == null) return 0;
            return ItemsInventory.Values.Sum(i => i.Count);
        }



        // Lưu kho đồ ra file items.json trong thư mục bin/Debug
        public void SaveItemsJson()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "items.json");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                int i = 0;
                foreach (var kvp in ItemsInventory)
                {
                    string comma = (++i < ItemsInventory.Count) ? "," : "";
                    sb.AppendLine($"  \"{kvp.Key}\": {kvp.Value.Count}{comma}");
                }
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }

        // Đọc kho đồ từ file items.json (để người chơi/tester có thể sửa file test)
        public void LoadItemsJson()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "items.json");
                if (!File.Exists(path))
                {
                    SaveItemsJson(); // Chưa có file thì tạo file mặc định
                    return;
                }

                string[] lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    foreach (var type in ItemsInventory.Keys)
                    {
                        if (line.Contains($"\"{type}\""))
                        {
                            string[] parts = line.Split(':');
                            if (parts.Length == 2)
                            {
                                string numStr = parts[1].Trim().Trim(',', '}');
                                if (int.TryParse(numStr, out int count))
                                {
                                    ItemsInventory[type].Count = count;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}