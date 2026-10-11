using BlockBlast.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BlockBlast
{
    // Cấu trúc mô hình dữ liệu JSON cho trạng thái game
    public class GameSaveModel
    {
        public int CurrentScore { get; set; }
        public int[][] BoardMatrix { get; set; } // A[i][j] = 1 (có block), 0 (trống)
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

        private int GetWeightedShapeIndex(int score)
        {
            double roll = rand.NextDouble();

            if (score < 1000)
            {
                return GetRandomShapeFromCategory(1);
            }
            else if (score >= 1000 && score < 2000)
            {
                return roll < 0.8 ? GetRandomShapeFromCategory(1) : GetRandomShapeFromCategory(2);
            }
            else if (score >= 2000 && score < 3000)
            {
                if (roll < 0.6) return GetRandomShapeFromCategory(1);
                else if (roll < 0.8) return GetRandomShapeFromCategory(2);
                else return GetRandomShapeFromCategory(3);
            }
            else
            {
                if (roll < 0.4) return GetRandomShapeFromCategory(1);
                else if (roll < 0.7) return GetRandomShapeFromCategory(2);
                else return GetRandomShapeFromCategory(3);
            }
        }

        private int GetRandomShapeFromCategory(int category)
        {
            int totalShapes = BlockPiece.AllShapes.Count;

            if (category == 1) // Khối đẹp: 1x1, thanh thẳng, vuông (Index 0 -> 10)
            {
                return rand.Next(0, Math.Min(11, totalShapes));
            }
            else if (category == 2) // Khối chữ L, chữ T (Index 11 -> 22)
            {
                int minIdx = Math.Min(11, totalShapes);
                int maxIdx = Math.Min(23, totalShapes);
                if (minIdx >= maxIdx) minIdx = 0;
                return rand.Next(minIdx, maxIdx);
            }
            else // Khối chữ Z, chữ S và các khối còn lại (Index 23 -> hết)
            {
                int minIdx = Math.Min(23, totalShapes);
                if (minIdx >= totalShapes) minIdx = 0;
                return rand.Next(minIdx, totalShapes);
            }
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
            int samplePoolSize = 60;

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
                List<BlockPiece> candidateSet = new List<BlockPiece>();
                for (int j = 0; j < 3; j++)
                {
                    candidateSet.Add(CreateRandomPiece());
                }

                bool hasValidOrder = false;
                foreach (var order in possibleOrders)
                {
                    if (CanPlaceSequentialOrder(candidateSet, order))
                    {
                        hasValidOrder = true;
                        break;
                    }
                }

                if (hasValidOrder)
                {
                    validCombinations.Add(candidateSet);
                }
            }

            if (validCombinations.Count > 0)
            {
                int randomIndex = rand.Next(validCombinations.Count);
                List<BlockPiece> selectedSet = validCombinations[randomIndex];

                // Xáo trộn ngẫu nhiên vị trí (Shuffle) để che giấu thứ tự đặt của hệ thống
                for (int idx = selectedSet.Count - 1; idx > 0; idx--)
                {
                    int k = rand.Next(idx + 1);
                    var temp = selectedSet[idx];
                    selectedSet[idx] = selectedSet[k];
                    selectedSet[k] = temp;
                }

                return selectedSet;
            }

            // Fallback khi bàn cờ quá chật
            List<BlockPiece> fallbackSet = new List<BlockPiece>
            {
                new BlockPiece(new int[,] { { 1 } }, rand.Next(1, 8)),
                new BlockPiece(new int[,] { { 1, 1 } }, rand.Next(1, 8)),
                new BlockPiece(new int[,] { { 1 }, { 1 } }, rand.Next(1, 8))
            };

            return fallbackSet;
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
                if (CanPlacePiece(piece)) return false;
            }
            return true;
        }

        // ================================================================
        //  2. LƯU & TẢI TRẠNG THÁI GAME QUA FILE JSON (A[i][j])
        // ================================================================

        private string GetSaveGameFilePath()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(folder, "gamestate.json");
        }

        public void SaveGameState(Panel pnl1, Panel pnl2, Panel pnl3)
        {
            try
            {
                int[,] boardGrid = Board.GetGrid();
                int[][] boardMatrix = new int[BOARD_SIZE][];
                for (int r = 0; r < BOARD_SIZE; r++)
                {
                    boardMatrix[r] = new int[BOARD_SIZE];
                    for (int c = 0; c < BOARD_SIZE; c++)
                    {
                        boardMatrix[r][c] = boardGrid[r, c]; // A[i][j]
                    }
                }

                var dockList = new List<DockPieceModel>();
                Panel[] panels = { pnl1, pnl2, pnl3 };
                foreach (var p in panels)
                {
                    var model = new DockPieceModel();
                    if (p != null && p.Tag is BlockPiece piece && piece.Shape != null)
                    {
                        model.IsVisible = true;
                        model.ColorIndex = piece.ColorIndex;
                        int rows = piece.Shape.GetLength(0);
                        int cols = piece.Shape.GetLength(1);
                        model.ShapeMatrix = new int[rows][];
                        for (int r = 0; r < rows; r++)
                        {
                            model.ShapeMatrix[r] = new int[cols];
                            for (int c = 0; c < cols; c++)
                            {
                                model.ShapeMatrix[r][c] = piece.Shape[r, c];
                            }
                        }
                    }
                    else
                    {
                        model.IsVisible = false;
                        model.ColorIndex = 0;
                        model.ShapeMatrix = new int[0][];
                    }
                    dockList.Add(model);
                }

                var saveData = new GameSaveModel
                {
                    CurrentScore = CurrentScore,
                    BoardMatrix = boardMatrix,
                    DockPieces = dockList
                };

                string jsonString = SimpleJsonSerialize(saveData);
                File.WriteAllText(GetSaveGameFilePath(), jsonString);
            }
            catch { }
        }

        public bool LoadGameState(Panel pnl1, Panel pnl2, Panel pnl3)
        {
            try
            {
                string path = GetSaveGameFilePath();
                if (!File.Exists(path)) return false;

                string jsonString = File.ReadAllText(path);
                var saveData = SimpleJsonDeserialize(jsonString);
                if (saveData == null) return false;

                this.CurrentScore = saveData.CurrentScore;

                int[,] newGrid = new int[BOARD_SIZE, BOARD_SIZE];
                for (int r = 0; r < BOARD_SIZE && r < saveData.BoardMatrix.Length; r++)
                {
                    for (int c = 0; c < BOARD_SIZE && c < saveData.BoardMatrix[r].Length; c++)
                    {
                        newGrid[r, c] = saveData.BoardMatrix[r][c];
                    }
                }
                this.Board.SetGrid(newGrid);

                Panel[] panels = { pnl1, pnl2, pnl3 };
                for (int i = 0; i < panels.Length && i < saveData.DockPieces.Count; i++)
                {
                    var model = saveData.DockPieces[i];
                    if (model.IsVisible && model.ShapeMatrix != null && model.ShapeMatrix.Length > 0)
                    {
                        int rows = model.ShapeMatrix.Length;
                        int cols = model.ShapeMatrix[0].Length;
                        int[,] shape = new int[rows, cols];
                        for (int r = 0; r < rows; r++)
                        {
                            for (int c = 0; c < cols; c++)
                            {
                                shape[r, c] = model.ShapeMatrix[r][c];
                            }
                        }
                        panels[i].Tag = new BlockPiece(shape, model.ColorIndex);
                        panels[i].Visible = true;
                    }
                    else
                    {
                        panels[i].Tag = null;
                        panels[i].Visible = false;
                    }
                    panels[i].Invalidate();
                }

                File.Delete(path);
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

        private string SimpleJsonSerialize(GameSaveModel data)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"CurrentScore\": {data.CurrentScore},");

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

        private GameSaveModel SimpleJsonDeserialize(string json)
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
                    string scoreStr = json.Substring(colon + 1, comma - colon - 1).Trim();
                    int.TryParse(scoreStr, out int sc);
                    model.CurrentScore = sc;
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
                                if (int.TryParse(nums[c].Trim(), out int val))
                                {
                                    model.BoardMatrix[r][c] = val; // A[i][j]
                                }
                            }
                            idx = rowEnd + 1;
                            r++;
                        }
                    }
                }

                model.DockPieces = new List<DockPieceModel>();
                for (int i = 0; i < 3; i++)
                {
                    model.DockPieces.Add(new DockPieceModel { IsVisible = false, ColorIndex = 1, ShapeMatrix = new int[1][] { new int[] { 1 } } });
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
    }
}