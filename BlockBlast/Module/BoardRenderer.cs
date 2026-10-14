using BlockBlast.Module;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BlockBlast
{
    public static class BoardRenderer
    {
        public const int BOARD_SIZE = 8; // Thay thế hoặc tham chiếu cố định
        public const int GAP = 4;
        public const int MARGIN = 8;

        // ===== Bảng màu Dark Slate =====
        public static readonly Color BoardFrameColor = Color.FromArgb(18, 21, 32);
        public static readonly Color BoardFrameBorderColor = Color.FromArgb(40, 45, 66);
        public static readonly Color EmptyCellBorderColor = Color.FromArgb(36, 41, 60);

        private static FontFamily uiFontFamily;

        // Hàm hỗ trợ tính toán CELL_SIZE phù hợp với kích thước Panel hiện tại
        public static int CalculateCellSize(Size panelSize)
        {
            // Tổng khoảng trống cố định cần trừ đi (2 lề + 7 khoảng cách giữa 8 ô)
            int totalSpacing = (MARGIN * 2) + (GAP * (BOARD_SIZE - 1));

            // Lấy chiều rộng và chiều cao khả dụng để vẽ 8 ô
            int availableWidth = panelSize.Width - totalSpacing;
            int availableHeight = panelSize.Height - totalSpacing;

            // Kích thước 1 ô sẽ lấy theo chiều nhỏ hơn để đảm bảo ô luôn là hình vuông
            int cellSize = Math.Min(availableWidth, availableHeight) / BOARD_SIZE;

            // Đảm bảo kích thước ô tối thiểu không bị nhỏ hơn 10px
            return Math.Max(cellSize, 10);
        }

        // Kích thước (px) toàn bộ vùng bàn cờ, bao gồm lề
        public static int GetBoardPixelSize(int cellSize)
        {
            return MARGIN * 2 + BOARD_SIZE * cellSize + (BOARD_SIZE - 1) * GAP;
        }

        // Hình chữ nhật của 1 ô trên bàn cờ (toạ độ pnlBoard)
        public static Rectangle GetCellRect(int row, int col, int cellSize)
        {
            return new Rectangle(MARGIN + col * (cellSize + GAP), MARGIN + row * (cellSize + GAP), cellSize, cellSize);
        }

        // Kích thước thực tế (px) của một khối gạch khi vẽ theo cellSize của bàn cờ
        public static Size GetPieceSize(int[,] shape, int cellSize)
        {
            int rows = shape.GetLength(0);
            int cols = shape.GetLength(1);
            return new Size(cols * cellSize + (cols - 1) * GAP, rows * cellSize + (rows - 1) * GAP);
        }

        public static bool TryGetCellAt(Point boardPoint, Size panelSize, out int row, out int col)
        {
            int cellSize = CalculateCellSize(panelSize);
            int totalBoardDim = MARGIN * 2 + BOARD_SIZE * cellSize + (BOARD_SIZE - 1) * GAP;

            row = -1; col = -1;
            if (boardPoint.X < MARGIN || boardPoint.Y < MARGIN) return false;

            col = (boardPoint.X - MARGIN) / (cellSize + GAP);
            row = (boardPoint.Y - MARGIN) / (cellSize + GAP);

            if (row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE)
            {
                // Kiểm tra con trỏ có nằm đúng trong phạm vi ô (tránh nằm trong khoảng GAP)
                int cellX = MARGIN + col * (cellSize + GAP);
                int cellY = MARGIN + row * (cellSize + GAP);
                if (boardPoint.X >= cellX && boardPoint.X <= cellX + cellSize &&
                    boardPoint.Y >= cellY && boardPoint.Y <= cellY + cellSize)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool GetCellFromPoint(Point pt, Size panelSize, out int row, out int col)
        {
            return TryGetCellAt(pt, panelSize, out row, out col);
        }

        /// <summary>
        /// Cơ chế "hút nam châm": làm tròn toạ độ góc trên-trái của khối gạch về ô gần nhất.
        /// Không có vùng chết trong khoảng GAP nên khối gạch không bị khựng/trượt khi rê chuột.
        /// </summary>
        public static bool GetMagneticCellAt(Point boardPoint, Size panelSize, out int row, out int col)
        {
            int cellSize = CalculateCellSize(panelSize);
            int step = cellSize + GAP;
            row = -1; col = -1;

            // Tính vị trí tương đối so với lề
            double relX = boardPoint.X - MARGIN;
            double relY = boardPoint.Y - MARGIN;

            col = (int)Math.Round(relX / step, MidpointRounding.AwayFromZero);
            row = (int)Math.Round(relY / step, MidpointRounding.AwayFromZero);

            return (row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE);
        }

        // ================================================================
        //  PRIMITIVES
        // ================================================================

        public static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
            if (d < 2)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRoundedRect(Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                int r = Math.Min(radius, Math.Min(width, height) / 2); // Tránh lỗi radius lớn hơn cell
                if (r < 1) r = 1;

                path.AddArc(x, y, r, r, 180, 90);
                path.AddArc(x + width - r, y, r, r, 270, 90);
                path.AddArc(x + width - r, y + height - r, r, r, 0, 90);
                path.AddArc(x, y + height - r, r, r, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        public static Color Lighten(Color c, float amount)
        {
            return Color.FromArgb(c.A,
                (int)(c.R + (255 - c.R) * amount),
                (int)(c.G + (255 - c.G) * amount),
                (int)(c.B + (255 - c.B) * amount));
        }

        public static Color Darken(Color c, float amount)
        {
            return Color.FromArgb(c.A,
                (int)(c.R * (1 - amount)),
                (int)(c.G * (1 - amount)),
                (int)(c.B * (1 - amount)));
        }

        private static Color WithAlpha(Color c, int alpha)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, alpha)), c.R, c.G, c.B);
        }

        private static FontFamily UiFontFamily
        {
            get
            {
                if (uiFontFamily == null)
                {
                    try { uiFontFamily = new FontFamily("Segoe UI"); }
                    catch (ArgumentException) { uiFontFamily = FontFamily.GenericSansSerif; }
                }
                return uiFontFamily;
            }
        }

        // Ô trống: nền Dark Slate + viền mảnh
        public static void DrawEmptyCell(Graphics g, Rectangle rect)
        {
            int radius = Math.Max(3, rect.Width / 6);
            using (GraphicsPath path = CreateRoundedPath(rect, radius))
            using (Brush brush = new SolidBrush(BlockPiece.GetColor(0)))
            using (Pen border = new Pen(EmptyCellBorderColor, 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(border, path);
            }
        }

        /// <summary>
        /// Vẽ 1 viên gạch phong cách 3D Gem/Bevel:
        /// nền gradient màu gốc + viền sáng trên-trái + viền bóng dưới-phải + vệt bóng kính phía trên.
        /// </summary>
        public static void DrawGemCell(Graphics g, Color baseColor, Rectangle rect, int alpha = 255)
        {
            if (rect.Width < 4 || rect.Height < 4 || alpha <= 0) return;

            float a = Math.Min(255, alpha) / 255f;
            int radius = Math.Max(3, rect.Width / 6);

            using (GraphicsPath body = CreateRoundedPath(rect, radius))
            {
                // 1. Nền khối gạch: màu gốc, gradient dọc rất nhẹ để có chiều sâu
                Rectangle gradRect = new Rectangle(rect.X, rect.Y - 1, rect.Width, rect.Height + 2);
                using (LinearGradientBrush brush = new LinearGradientBrush(gradRect,
                           WithAlpha(Lighten(baseColor, 0.10f), alpha),
                           WithAlpha(Darken(baseColor, 0.08f), alpha),
                           LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, body);
                }

                // 2. Viền nổi (bevel) bên trong khối
                int bevel = Math.Max(2, rect.Width / 9);
                int inset = (bevel + 1) / 2;
                Rectangle inner = Rectangle.Inflate(rect, -inset, -inset);

                using (GraphicsPath edge = CreateRoundedPath(inner, Math.Max(2, radius - inset)))
                {
                    // 2a. Inner Highlight - nửa trên-trái
                    GraphicsState state = g.Save();
                    using (GraphicsPath tri = new GraphicsPath())
                    {
                        tri.AddPolygon(new[]
                        {
                            new Point(rect.Left, rect.Top),
                            new Point(rect.Right, rect.Top),
                            new Point(rect.Left, rect.Bottom)
                        });
                        g.SetClip(tri, CombineMode.Intersect);
                        using (Pen pen = new Pen(Color.FromArgb((int)(70 * a), 255, 255, 255), bevel))
                        {
                            g.DrawPath(pen, edge);
                        }
                    }
                    g.Restore(state);

                    // 2b. Inner Shadow - nửa dưới-phải
                    state = g.Save();
                    using (GraphicsPath tri = new GraphicsPath())
                    {
                        tri.AddPolygon(new[]
                        {
                            new Point(rect.Right, rect.Top),
                            new Point(rect.Right, rect.Bottom),
                            new Point(rect.Left, rect.Bottom)
                        });
                        g.SetClip(tri, CombineMode.Intersect);
                        using (Pen pen = new Pen(Color.FromArgb((int)(60 * a), 0, 0, 0), bevel))
                        {
                            g.DrawPath(pen, edge);
                        }
                    }
                    g.Restore(state);
                }

                // 3. Vệt bóng kính (gloss) ở 40% phía trên
                int glossH = Math.Max(2, inner.Height * 2 / 5);
                Rectangle gloss = new Rectangle(inner.X + bevel, inner.Y + bevel / 2, Math.Max(2, inner.Width - bevel * 2), glossH);
                using (GraphicsPath glossPath = CreateRoundedPath(gloss, Math.Max(2, radius / 2)))
                using (LinearGradientBrush glossBrush = new LinearGradientBrush(
                           new Rectangle(gloss.X, gloss.Y - 1, gloss.Width, gloss.Height + 2),
                           Color.FromArgb((int)(55 * a), 255, 255, 255),
                           Color.FromArgb(0, 255, 255, 255),
                           LinearGradientMode.Vertical))
                {
                    g.FillPath(glossBrush, glossPath);
                }
            }
        }

        // Viền phát sáng (glow) quanh một hàng/cột sắp nổ
        private static void DrawLineGlow(Graphics g, Rectangle lineRect, Color baseColor)
        {
            Color glow = Lighten(baseColor, 0.45f);
            int radius = Math.Max(4, lineRect.Height / 5);

            // Lớp phủ sáng nhẹ bên trong
            using (GraphicsPath fillPath = CreateRoundedPath(lineRect, radius))
            using (Brush overlay = new SolidBrush(Color.FromArgb(28, 255, 255, 255)))
            {
                g.FillPath(overlay, fillPath);
            }

            // 3 lớp viền: ngoài mờ -> trong đậm, tạo cảm giác "phát sáng"
            int[] inflate = { 5, 3, 1 };
            float[] widths = { 5f, 3.5f, 2f };
            int[] alphas = { 40, 90, 235 };
            for (int i = 0; i < inflate.Length; i++)
            {
                Rectangle r = Rectangle.Inflate(lineRect, inflate[i], inflate[i]);
                using (GraphicsPath path = CreateRoundedPath(r, radius + inflate[i]))
                using (Pen pen = new Pen(WithAlpha(glow, alphas[i]), widths[i]))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, path);
                }
            }
        }

        // ================================================================
        //  BOARD
        // ================================================================

        /// <param name="flashingCells">Các ô đang nổ (Point.X = cột, Point.Y = hàng) - vẽ màu trắng sáng.</param>
        /// <param name="flashScale">1 = chớp trắng đủ kích thước; giảm dần về 0 = thu nhỏ và tan biến.</param>
        public static void RenderBoard(Graphics g, Size panelSize, GameBoard board, int[,] draggingShape, int draggingColorIndex,
                                       int hoverRow, int hoverCol, HashSet<Point> flashingCells = null, float flashScale = 1f)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Tính CELL_SIZE động
            int cellSize = CalculateCellSize(panelSize);
            int boardDim = GetBoardPixelSize(cellSize);
            int linePixelLength = BOARD_SIZE * cellSize + (BOARD_SIZE - 1) * GAP;

            // 0. Khung nền bàn cờ
            Rectangle frame = new Rectangle(1, 1, boardDim - 2, boardDim - 2);
            using (GraphicsPath framePath = CreateRoundedPath(frame, 12))
            using (Brush frameBrush = new SolidBrush(BoardFrameColor))
            using (Pen framePen = new Pen(BoardFrameBorderColor, 1.5f))
            {
                g.FillPath(frameBrush, framePath);
                g.DrawPath(framePen, framePath);
            }

            // Xác định các hàng/cột sẽ nổ nếu thả khối tại vị trí hover (Line Preview)
            bool hasPreview = draggingShape != null && hoverRow != -1 && hoverCol != -1;
            List<int> previewRows = new List<int>();
            List<int> previewCols = new List<int>();
            if (hasPreview)
            {
                board.GetLinesCompletedByPlacement(draggingShape, hoverRow, hoverCol, previewRows, previewCols);
            }
            Color dragColor = BlockPiece.GetColor(draggingColorIndex);

            // 1. Vẽ 64 ô của bàn chơi
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (flashingCells != null && flashingCells.Contains(new Point(c, r))) continue; // vẽ ở bước 4

                    Rectangle rect = GetCellRect(r, c, cellSize);
                    int value = board.GetCell(r, c);
                    if (value == 0)
                    {
                        DrawEmptyCell(g, rect);
                    }
                    else
                    {
                        // Ô thuộc hàng/cột sắp nổ được "nhuộm" theo màu khối đang kéo
                        bool inBlastLine = hasPreview && (previewRows.Contains(r) || previewCols.Contains(c));
                        DrawGemCell(g, inBlastLine ? dragColor : BlockPiece.GetColor(value), rect);
                    }
                }
            }

            // 2. Vẽ hiệu ứng xem trước (Ghost Preview) - bán trong suốt Alpha 130
            if (hasPreview)
            {
                int rows = draggingShape.GetLength(0);
                int cols = draggingShape.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (draggingShape[r, c] == 0) continue;

                        int targetR = hoverRow + r;
                        int targetC = hoverCol + c;
                        if (targetR >= 0 && targetR < BOARD_SIZE && targetC >= 0 && targetC < BOARD_SIZE)
                        {
                            DrawGemCell(g, dragColor, GetCellRect(targetR, targetC, cellSize), 130);
                        }
                    }
                }

                // 3. Line Preview Glow cho các hàng/cột sắp nổ
                foreach (int r in previewRows)
                {
                    DrawLineGlow(g, new Rectangle(MARGIN, MARGIN + r * (cellSize + GAP), linePixelLength, cellSize), dragColor);
                }
                foreach (int c in previewCols)
                {
                    DrawLineGlow(g, new Rectangle(MARGIN + c * (cellSize + GAP), MARGIN, cellSize, linePixelLength), dragColor);
                }
            }

            // 4. Các ô đang nổ: chớp trắng -> thu nhỏ & tan biến
            if (flashingCells != null && flashingCells.Count > 0)
            {
                float scale = Math.Max(0f, Math.Min(1f, flashScale));
                foreach (Point p in flashingCells)
                {
                    Rectangle rect = GetCellRect(p.Y, p.X, cellSize);
                    DrawEmptyCell(g, rect);

                    int size = (int)(cellSize * scale);
                    if (size < 2) continue;

                    Rectangle shrink = new Rectangle(rect.X + (cellSize - size) / 2, rect.Y + (cellSize - size) / 2, size, size);
                    int alpha = (int)(255 * Math.Min(1f, 0.25f + scale));

                    using (GraphicsPath path = CreateRoundedPath(shrink, Math.Max(2, size / 6)))
                    using (Brush white = new SolidBrush(Color.FromArgb(alpha, Color.White)))
                    using (Pen halo = new Pen(Color.FromArgb(alpha / 3, 255, 255, 255), 4f))
                    {
                        g.DrawPath(halo, path);
                        g.FillPath(white, path);
                    }
                }
            }
        }

        // ================================================================
        //  DRAGGING PIECE / DOCK / OVERLAYS
        // ================================================================

        /// <summary>Vẽ khối gạch kích thước thật (độ đậm 100%) tại origin, kèm bóng đổ khi được nhấc lên.</summary>
        public static void RenderFloatingPiece(Graphics g, int[,] shape, int colorIndex, int cellSize, Point origin, bool drawShadow)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int rows = shape.GetLength(0);
            int cols = shape.GetLength(1);
            Color color = BlockPiece.GetColor(colorIndex);

            if (drawShadow)
            {
                using (Brush shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                {
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                        {
                            if (shape[r, c] == 0) continue;
                            Rectangle rect = new Rectangle(origin.X + c * (cellSize + GAP) + 3, origin.Y + r * (cellSize + GAP) + 7, cellSize, cellSize);
                            using (GraphicsPath p = CreateRoundedPath(rect, Math.Max(3, cellSize / 6)))
                            {
                                g.FillPath(shadow, p);
                            }
                        }
                }
            }

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (shape[r, c] == 0) continue;
                    DrawGemCell(g, color, new Rectangle(origin.X + c * (cellSize + GAP), origin.Y + r * (cellSize + GAP), cellSize, cellSize));
                }
        }

        /// <summary>Tạo bitmap ARGB (nền trong suốt) của khối gạch đang bay, dùng cho cửa sổ lớp phủ kéo-thả.</summary>
        public static Bitmap CreateFloatingPieceBitmap(int[,] shape, int colorIndex, int cellSize, int padding)
        {
            Size pieceSize = GetPieceSize(shape, cellSize);
            Bitmap bmp = new Bitmap(pieceSize.Width + padding * 2, pieceSize.Height + padding * 2, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                RenderFloatingPiece(g, shape, colorIndex, cellSize, new Point(padding, padding), true);
            }
            return bmp;
        }

        public static void RenderEmptyDockSlot(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle slotRect = new Rectangle(bounds.X + 8, bounds.Y + 8, bounds.Width - 16, bounds.Height - 16);
            if (slotRect.Width <= 0 || slotRect.Height <= 0) return;

            using (GraphicsPath slotPath = CreateRoundedPath(slotRect, 14))
            using (Brush slotBg = new SolidBrush(Color.FromArgb(26, 29, 46)))
            using (Pen slotBorder = new Pen(Color.FromArgb(45, 50, 77), 1.5f))
            {
                slotBorder.DashStyle = DashStyle.Dash;
                slotBorder.DashPattern = new float[] { 4f, 4f };
                g.FillPath(slotBg, slotPath);
                g.DrawPath(slotBorder, slotPath);
            }
        }

        public static void RenderDockPiece(Graphics g, Panel panel)
        {
            if (panel == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (panel.Tag == null)
            {
                RenderEmptyDockSlot(g, panel.ClientRectangle);
                return;
            }

            BlockPiece piece = (BlockPiece)panel.Tag;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int[,] shape = piece.Shape;
            int rows = shape.GetLength(0);
            int cols = shape.GetLength(1);

            int miniCell = Math.Min((panel.Width - 20) / 5, (panel.Height - 20) / 5);
            int miniGap = 3;

            int totalW = cols * (miniCell + miniGap) - miniGap;
            int totalH = rows * (miniCell + miniGap) - miniGap;
            int startX = (panel.Width - totalW) / 2;
            int startY = (panel.Height - totalH) / 2;

            Color color = BlockPiece.GetColor(piece.ColorIndex);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (shape[r, c] != 0)
                    {
                        int x = startX + c * (miniCell + miniGap);
                        int y = startY + r * (miniCell + miniGap);
                        DrawGemCell(g, color, new Rectangle(x, y, miniCell, miniCell));
                    }
                }
            }
        }

        /// <summary>Chữ nổi tự động co giãn cỡ chữ và kẹp trong phạm vi bàn cờ để không bị cắt mất chữ.</summary>
        public static void RenderFloatingText(Graphics g, string text, PointF center, float alpha)
        {
            if (string.IsNullOrEmpty(text) || alpha <= 0f) return;

            int a = (int)(255 * Math.Min(1f, alpha));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF clip = g.VisibleClipBounds;
            float maxAllowedWidth = Math.Max(180f, clip.Width - 24f);

            using (GraphicsPath path = new GraphicsPath())
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                // 1. Tự động giảm fontSize từ 30f xuống tối thiểu 13f nếu chuỗi quá dài so với chiều ngang bàn cờ
                float fontSize = 30f;
                RectangleF bounds = RectangleF.Empty;

                while (fontSize >= 13f)
                {
                    path.Reset();
                    path.AddString(text, UiFontFamily, (int)FontStyle.Bold, fontSize, center, sf);
                    bounds = path.GetBounds();
                    if (bounds.Width <= maxAllowedWidth || fontSize <= 13f)
                        break;
                    fontSize -= 2f;
                }

                if (bounds.Width < 1 || bounds.Height < 1) return;

                // 2. Kẹp (Clamp) vị trí sao cho khung chữ không bị tràn ra ngoài 4 cạnh của pnlBoard
                float dx = 0f, dy = 0f;
                if (bounds.Left < 12f) dx = 12f - bounds.Left;
                else if (bounds.Right > clip.Width - 12f) dx = (clip.Width - 12f) - bounds.Right;

                if (bounds.Top < 12f) dy = 12f - bounds.Top;
                else if (bounds.Bottom > clip.Height - 12f) dy = (clip.Height - 12f) - bounds.Bottom;

                if (Math.Abs(dx) > 0.5f || Math.Abs(dy) > 0.5f)
                {
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(dx, dy);
                        path.Transform(m);
                        bounds = path.GetBounds();
                    }
                }

                float outlineWidth = Math.Max(3.5f, fontSize / 5.5f);
                using (Pen outline = new Pen(Color.FromArgb(a * 220 / 255, 12, 14, 24), outlineWidth) { LineJoin = LineJoin.Round })
                {
                    g.DrawPath(outline, path);
                }
                using (LinearGradientBrush fill = new LinearGradientBrush(
                           RectangleF.Inflate(bounds, 0, 2),
                           Color.FromArgb(a, 255, 240, 150),
                           Color.FromArgb(a, 255, 160, 40),
                           LinearGradientMode.Vertical))
                {
                    g.FillPath(fill, path);
                }
            }
        }

        // Lớp phủ khi tạm dừng
        public static void RenderPausedOverlay(Graphics g, Size panelSize)
        {
            int boardDim = GetBoardPixelSize(CalculateCellSize(panelSize));
            Rectangle area = new Rectangle(1, 1, boardDim - 2, boardDim - 2);
            using (GraphicsPath path = CreateRoundedPath(area, 12))
            using (Brush dim = new SolidBrush(Color.FromArgb(170, 10, 12, 20)))
            {
                g.FillPath(dim, path);
            }
            RenderFloatingText(g, "TẠM DỪNG", new PointF(boardDim / 2f, boardDim / 2f), 1f);
        }

        public static void RenderGameOverOverlay(Graphics g, Size boardSize, int score, int highScore, bool isNewRecord)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int w = boardSize.Width;
            int h = boardSize.Height;
            Rectangle rect = new Rectangle(0, 0, w, h);

            // 1. Lớp phủ làm tối cao cấp (Semi-transparent Dim Overlay)
            int radius = Math.Max(8, w / 25);
            using (GraphicsPath path = CreateRoundedPath(rect, radius))
            using (Brush overlayBrush = new SolidBrush(Color.FromArgb(235, 14, 17, 26)))
            using (Pen borderPen = new Pen(Color.FromArgb(60, 255, 159, 67), 2f))
            {
                g.FillPath(overlayBrush, path);
                g.DrawPath(borderPen, path);
            }

            // 2. Tiêu đề "GAME OVER" (Cam Neon rực rỡ)
            using (Font titleFont = new Font("Segoe UI", 26f, FontStyle.Bold))
            using (Brush titleBrush = new SolidBrush(Color.FromArgb(255, 159, 67)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("GAME OVER", titleFont, titleBrush, new RectangleF(0, h * 0.12f, w, 50), sf);
            }

            // 3. Điểm số "ĐIỂM: 450" (Trắng tinh, to rõ)
            using (Font scoreFont = new Font("Segoe UI", 18f, FontStyle.Bold))
            using (Brush scoreBrush = new SolidBrush(Color.White))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString($"ĐIỂM: {score}", scoreFont, scoreBrush, new RectangleF(0, h * 0.30f, w, 40), sf);
            }

            // 4. Kỷ lục "KỶ LỤC MỚI!" hoặc "KỶ LỤC: 1050"
            string recordText = isNewRecord ? $"KỶ LỤC MỚI: {score}!" : $"KỶ LỤC: {highScore}";
            Color recordColor = isNewRecord ? Color.FromArgb(255, 215, 0) : Color.FromArgb(160, 165, 181);
            using (Font recordFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))
            using (Brush recordBrush = new SolidBrush(recordColor))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(recordText, recordFont, recordBrush, new RectangleF(0, h * 0.44f, w, 36), sf);
            }
        }
public static void ApplyModernTheme(Button[] buttons, Panel[] docks)
        {
            foreach (Button btn in buttons)
            {
                if (btn != null)
                {
                    btn.Cursor = Cursors.Hand;
                    ApplyRoundedRegion(btn, 12);
                }
            }

            foreach (Panel dock in docks)
            {
                if (dock != null)
                {
                    dock.Cursor = Cursors.Hand;
                    ApplyRoundedRegion(dock, 16);
                }
            }
        }

        public static void ApplyRoundedRegion(Control control, int radius)
        {
            using (GraphicsPath path = CreateRoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius))
            {
                control.Region = new Region(path);
            }
        }

        // Sự kiện vẽ khung điểm bo góc tối sang trọng (#1E2235)
        public static void DrawScoreGroupBackground(object sender, PaintEventArgs e)
        {
            if (!(sender is Control pnl)) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
            using (GraphicsPath path = CreateRoundedPath(rect, 14))
            using (Brush bg = new SolidBrush(Color.FromArgb(30, 34, 53)))
            using (Pen border = new Pen(Color.FromArgb(48, 54, 80)))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }

            using (Pen separator = new Pen(Color.FromArgb(48, 54, 80)))
            {
                g.DrawLine(separator, 190, 16, 190, rect.Height - 16);
            }
        }

        // Cập nhật giá trị lên các Label điểm số
        public static void UpdateScoreLabels(Label lblScore, Label lblBestScore, GameManager gameManager)
        {
            if (lblScore != null) lblScore.Text = gameManager.CurrentScore.ToString();
            if (lblBestScore != null) lblBestScore.Text = gameManager.HighScore.ToString();
        }
        public static PointF GetCellsCenter(Size boardSize, IEnumerable<Point> cells)
        {
            int cellSize = CalculateCellSize(boardSize);
            float sumX = 0, sumY = 0;
            int count = 0;

            foreach (Point p in cells)
            {
                Rectangle rect = GetCellRect(p.Y, p.X, cellSize);
                sumX += rect.X + rect.Width / 2f;
                sumY += rect.Y + rect.Height / 2f;
                count++;
            }

            int boardDim = GetBoardPixelSize(cellSize);
            if (count == 0) return new PointF(boardDim / 2f, boardDim / 2f);

            // Giữ chữ nằm trọn trong bàn cờ
            float x = Math.Max(120f, Math.Min(boardDim - 120f, sumX / count));
            float y = Math.Max(110f, Math.Min(boardDim - 50f, sumY / count));
            return new PointF(x, y);
        }

        // Tạo nội dung thông báo combo dựa trên số dòng ăn được
        public static string BuildComboText(int linesCleared, int scoreGained)
        {
            if (linesCleared >= 4) return "MEGA BLAST!\n+" + scoreGained;
            if (scoreGained >= 200) return "COMBO x" + linesCleared + "!\n+" + scoreGained;
            return "+" + scoreGained;
        }
        // Bật bộ đệm kép (Double-Buffering) cho toàn bộ Control và Panel con để chống giật chớp màn hình
        public static void EnableDoubleBuffering(Control control)
        {
            if (control == null) return;

            var prop = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(control, true, null);

            foreach (Control child in control.Controls)
            {
                EnableDoubleBuffering(child);
            }
        }
        // ================================================================
        //  CÁC HÀM RENDER ĐỒ HỌA GDI+ (CHUYỂN TỪ FORMGAMEPLAY SANG RENDERER)
        // ================================================================

        // 1. Vẽ icon bánh răng cưa cho nút Pause
        public static void DrawGearIcon(Graphics g, float width, float height, Color backColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = width / 2f;
            float cy = height / 2f;
            Color gearColor = Color.FromArgb(40, 48, 70);

            using (SolidBrush brush = new SolidBrush(gearColor))
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 45f;
                    var state = g.Save();
                    g.TranslateTransform(cx, cy);
                    g.RotateTransform(angle);
                    g.FillRectangle(brush, -3.5f, -13f, 7f, 26f);
                    g.Restore(state);
                }
                g.FillEllipse(brush, cx - 10f, cy - 10f, 20f, 20f);
                using (SolidBrush holeBrush = new SolidBrush(backColor))
                {
                    g.FillEllipse(holeBrush, cx - 4f, cy - 4f, 8f, 8f);
                }
            }
        }

        // 2. Vẽ tiêu đề lớn 3D và huy hiệu Kỷ lục trên Main Menu
        public static void DrawMainMenuBackground(Graphics g, int w, int h, int highScore)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Viền khung phát sáng
            using (var path = CreateRoundedPath(new Rectangle(2, 2, w - 5, h - 5), 18))
            using (Pen borderPen = new Pen(Color.FromArgb(110, 255, 159, 67), 2f))
            {
                g.DrawPath(borderPen, path);
            }

            // Tiêu đề "BLOCK BLAST" 3D
            using (Font titleFont = new Font("Segoe UI", 25F, FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("BLOCK BLAST", titleFont, new SolidBrush(Color.FromArgb(160, 0, 0, 0)), new PointF(w / 2f + 2, 22), sf);
                using (var brush = new LinearGradientBrush(
                    new Rectangle(0, 18, w, 45),
                    Color.FromArgb(255, 230, 100),
                    Color.FromArgb(255, 120, 40),
                    LinearGradientMode.Vertical))
                {
                    g.DrawString("BLOCK BLAST", titleFont, brush, new PointF(w / 2f, 20), sf);
                }
            }

            // Huy hiệu Kỷ lục (Mở rộng chiều rộng lên 340px để số điểm không bao giờ bị ngắt dòng hay che mất)
            Rectangle badgeRect = new Rectangle((w - 340) / 2, 75, 340, 36);
            using (var badgePath = CreateRoundedPath(badgeRect, 16))
            using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(34, 40, 64)))
            using (Pen badgeBorder = new Pen(Color.FromArgb(120, 255, 204, 77), 1.5f))
            using (Font badgeFont = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(255, 215, 0)))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                g.FillPath(badgeBg, badgePath);
                g.DrawPath(badgeBorder, badgePath);
                g.DrawString($"★ KỶ LỤC CAO NHẤT: {highScore:N0} ★", badgeFont, textBrush, badgeRect, sf);
            }
        }

        // 3. Vẽ ô chứa Item ở cột phải (Nền, viền Neon và Icon)
        public static void DrawItemSlot(Graphics g, int w, int h, Item item, bool isSelected)
        {
            if (item == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(1, 1, w - 3, h - 3);
            bool hasItem = item.Count > 0;

            using (var path = CreateRoundedPath(rect, 10))
            {
                if (isSelected)
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(42, 52, 78)))
                        g.FillPath(bg, path);
                    using (Pen border = new Pen(Color.FromArgb(255, 204, 77), 2.2f))
                        g.DrawPath(border, path);
                }
                else if (hasItem)
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(28, 34, 52)))
                        g.FillPath(bg, path);
                    using (Pen border = new Pen(Color.FromArgb(210, item.ThemeColor), 1.8f))
                        g.DrawPath(border, path);
                }
                else
                {
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(20, 23, 34)))
                        g.FillPath(bg, path);
                    using (Pen border = new Pen(Color.FromArgb(50, 55, 72), 1f))
                        g.DrawPath(border, path);
                }
            }

            float iconCx = w / 2f;
            float iconCy = h * 0.50f;
            float iconRadius = Math.Min(w, h) * 0.19f;

            if (hasItem)
                DrawItemProjectileIcon(g, item.Type, iconCx, iconCy, iconRadius);
            else
                DrawDimmedItemIcon(g, item.Type, iconCx, iconCy, iconRadius);
        }

        // 4. Vẽ icon vật phẩm sắc nét rực rỡ
        public static void DrawItemProjectileIcon(Graphics g, ItemType type, float cx, float cy, float radius)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (type == ItemType.Bomb3x3)
            {
                using (SolidBrush bombBrush = new SolidBrush(Color.FromArgb(36, 40, 52)))
                    g.FillEllipse(bombBrush, cx - radius, cy - radius + 2, radius * 2, radius * 2);
                using (Pen border = new Pen(Color.FromArgb(20, 22, 28), 1.5f))
                    g.DrawEllipse(border, cx - radius, cy - radius + 2, radius * 2, radius * 2);
                using (SolidBrush highlight = new SolidBrush(Color.FromArgb(140, 255, 255, 255)))
                    g.FillEllipse(highlight, cx - radius * 0.5f, cy - radius * 0.5f, radius * 0.45f, radius * 0.45f);
                using (Pen fusePen = new Pen(Color.FromArgb(210, 160, 90), 2f))
                    g.DrawArc(fusePen, cx - 4, cy - radius - 5, 8, 8, 200, 140);
                using (SolidBrush spark = new SolidBrush(Color.FromArgb(255, 120, 30)))
                    g.FillEllipse(spark, cx + 1, cy - radius - 7, 5, 5);
            }
            else if (type == ItemType.CrossClear)
            {
                float armLen = radius * 0.85f;
                float armThick = radius * 0.45f;
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 220, 60)))
                {
                    g.FillRectangle(brush, cx - armLen, cy - armThick / 2, armLen * 2, armThick);
                    g.FillRectangle(brush, cx - armThick / 2, cy - armLen, armThick, armLen * 2);
                }
                using (Pen border = new Pen(Color.White, 1.2f))
                {
                    g.DrawRectangle(border, cx - armLen, cy - armThick / 2, armLen * 2, armThick);
                    g.DrawRectangle(border, cx - armThick / 2, cy - armLen, armThick, armLen * 2);
                }
            }
            else if (type == ItemType.ColorLightning)
            {
                PointF[] bolt = new PointF[]
                {
                    new PointF(cx + 1, cy - radius),
                    new PointF(cx - radius * 0.75f, cy + 1),
                    new PointF(cx - 1, cy + 1),
                    new PointF(cx - 3, cy + radius),
                    new PointF(cx + radius * 0.75f, cy - 1),
                    new PointF(cx + 1, cy - 1)
                };
                using (SolidBrush boltBrush = new SolidBrush(Color.FromArgb(255, 230, 80)))
                    g.FillPolygon(boltBrush, bolt);
                using (Pen boltPen = new Pen(Color.White, 1.2f))
                    g.DrawPolygon(boltPen, bolt);
            }
            else if (type == ItemType.GravityFall)
            {
                PointF[] arrow1 = new PointF[]
                {
                    new PointF(cx - radius * 0.7f, cy - radius * 0.4f),
                    new PointF(cx + radius * 0.7f, cy - radius * 0.4f),
                    new PointF(cx, cy + 2)
                };
                PointF[] arrow2 = new PointF[]
                {
                    new PointF(cx - radius * 0.7f, cy + 1),
                    new PointF(cx + radius * 0.7f, cy + 1),
                    new PointF(cx, cy + radius * 0.9f)
                };
                using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
                {
                    g.FillPolygon(arrowBrush, arrow1);
                    g.FillPolygon(arrowBrush, arrow2);
                }
                using (Pen arrowPen = new Pen(Color.White, 1.0f))
                {
                    g.DrawPolygon(arrowPen, arrow1);
                    g.DrawPolygon(arrowPen, arrow2);
                }
            }
            else
            {
                using (Pen arcPen = new Pen(Color.FromArgb(52, 152, 219), 3f))
                    g.DrawArc(arcPen, cx - radius * 0.6f, cy - radius * 0.6f, radius * 1.2f, radius * 1.2f, 30, 280);
            }
        }

        // 5. Vẽ icon chìm mờ khi vật phẩm có số lượng = 0
        public static void DrawDimmedItemIcon(Graphics g, ItemType type, float cx, float cy, float radius)
        {
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(50, 56, 75)))
            using (Pen dimPen = new Pen(Color.FromArgb(70, 78, 100), 1.2f))
            {
                if (type == ItemType.Bomb3x3)
                {
                    g.FillEllipse(dimBrush, cx - radius, cy - radius + 2, radius * 2, radius * 2);
                    g.DrawEllipse(dimPen, cx - radius, cy - radius + 2, radius * 2, radius * 2);
                }
                else if (type == ItemType.CrossClear)
                {
                    float armLen = radius * 0.85f;
                    float armThick = radius * 0.45f;
                    g.FillRectangle(dimBrush, cx - armLen, cy - armThick / 2, armLen * 2, armThick);
                    g.FillRectangle(dimBrush, cx - armThick / 2, cy - armLen, armThick, armLen * 2);
                    g.DrawRectangle(dimPen, cx - armLen, cy - armThick / 2, armLen * 2, armThick);
                    g.DrawRectangle(dimPen, cx - armThick / 2, cy - armLen, armThick, armLen * 2);
                }
                else if (type == ItemType.ColorLightning)
                {
                    PointF[] bolt = new PointF[]
                    {
                        new PointF(cx + 1, cy - radius),
                        new PointF(cx - radius * 0.75f, cy + 1),
                        new PointF(cx - 1, cy + 1),
                        new PointF(cx - 3, cy + radius),
                        new PointF(cx + radius * 0.75f, cy - 1),
                        new PointF(cx + 1, cy - 1)
                    };
                    g.FillPolygon(dimBrush, bolt);
                    g.DrawPolygon(dimPen, bolt);
                }
                else if (type == ItemType.GravityFall)
                {
                    PointF[] arrow1 = new PointF[]
                    {
                        new PointF(cx - radius * 0.7f, cy - radius * 0.4f),
                        new PointF(cx + radius * 0.7f, cy - radius * 0.4f),
                        new PointF(cx, cy + 2)
                    };
                    PointF[] arrow2 = new PointF[]
                    {
                        new PointF(cx - radius * 0.7f, cy + 1),
                        new PointF(cx + radius * 0.7f, cy + 1),
                        new PointF(cx, cy + radius * 0.9f)
                    };
                    g.FillPolygon(dimBrush, arrow1);
                    g.FillPolygon(dimBrush, arrow2);
                    g.DrawPolygon(dimPen, arrow1);
                    g.DrawPolygon(dimPen, arrow2);
                }
                else
                {
                    g.DrawArc(dimPen, cx - radius * 0.7f, cy - radius * 0.7f, radius * 1.4f, radius * 1.4f, 40, 270);
                }
            }
        }
    }
}
