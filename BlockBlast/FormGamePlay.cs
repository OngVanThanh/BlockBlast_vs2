using BlockBlast.Module;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlockBlast
{
    public partial class fGamePlay : Form
    {
        private const int LIFT_OFFSET_Y = 35;
        private const int GHOST_PADDING = 12;

        private readonly GameManager gameManager = new GameManager();
        private readonly TutorialManager tutorialManager = new TutorialManager();
        private readonly Random fxRandom = new Random();

        private int[,] draggingShape = null;
        private int draggingColorIndex = 0;
        private Panel activeDockPanel = null;
        private int hoverRow = -1;
        private int hoverCol = -1;
        private int dragOffsetX = 0;
        private int dragOffsetY = 0;

        private bool isProcessingMove = false;
        private readonly HashSet<Point> flashingCells = new HashSet<Point>();
        private float flashScale = 1f;
        private string floatingComboText = "";
        private PointF floatingTextPos = PointF.Empty;
        private float floatingTextAlpha = 0f;
        private readonly Timer floatingTextTimer;

        private DragGhostWindow dragGhost;
        private Label lblTutorialGuide;
        private int sessionGameInitialHighScore = 0;
        private bool isGameOver = false;
        private bool isGameInProgress = false; // Cờ theo dõi trạng thái đang chơi dở hay không

        public fGamePlay()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            floatingTextTimer = new Timer { Interval = 16 };
            floatingTextTimer.Tick += FloatingTextTimer_Tick;
        }

        // ================================================================
        //  1. KHỞI TẠO VÀ SỰ KIỆN FORM (LIFECYCLE)
        // ================================================================

        private void fGamePlay_Load(object sender, EventArgs e)
        {
            gameManager.HighScore = ScoreManager.LoadSavedHighScore();
            sessionGameInitialHighScore = gameManager.HighScore;

            SoundManager.IsMuted = ScoreManager.LoadAudioSettings();
            if (btnSound != null)
            {
                btnSound.Text = SoundManager.IsMuted ? "LOA: TẮT" : "LOA: BẬT";
            }

            if (btnReplay != null && btnReplay.Parent != pnlBoard) pnlBoard.Controls.Add(btnReplay);
            if (btnReplay != null) btnReplay.Visible = false;
            if (pnlGameOver != null) pnlGameOver.Visible = false;
            if (pnlPause != null) pnlPause.Visible = false;
            if (pnlAbout != null) pnlAbout.Visible = false;

            InitTutorialLabel();
            LayoutGameControls();

            this.Resize += (s, ev) => { if (this.WindowState != FormWindowState.Minimized) LayoutGameControls(); };

            RegisterEventHandlers();
            ApplyModernTheme();
            UpdateScoreLabels();

            // Hiển thị Menu chính ngay khi mở ứng dụng
            ShowMainMenu();
        }

        private void InitTutorialLabel()
        {
            lblTutorialGuide = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 196, 77),
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblTutorialGuide);
        }

        private void RegisterEventHandlers()
        {
            pnlBoard.MouseMove += fGamePlay_MouseMove;
            pnlBoard.MouseUp += fGamePlay_MouseUp;
            pnlDock.MouseMove += fGamePlay_MouseMove;
            pnlDock.MouseUp += fGamePlay_MouseUp;
            pnlHeader.MouseMove += fGamePlay_MouseMove;
            pnlHeader.MouseUp += fGamePlay_MouseUp;

            pnlDockPiece1.MouseUp += fGamePlay_MouseUp;
            pnlDockPiece2.MouseUp += fGamePlay_MouseUp;
            pnlDockPiece3.MouseUp += fGamePlay_MouseUp;

            // Nút âm thanh trong menu tạm dừng
            if (btnSound != null)
            {
                btnSound.Click += (s, ev) =>
                {
                    SoundManager.IsMuted = !SoundManager.IsMuted;
                    ScoreManager.SaveAudioSettings(SoundManager.IsMuted);
                    btnSound.Text = SoundManager.IsMuted ? "LOA: TẮT" : "LOA: BẬT";
                };
            }

            // Nút mở Menu / Tạm dừng trên header trong game
            if (btnPause != null)
            {
                btnPause.Click += btnOpenMenu_Click;
            }

            // Các nút chức năng nằm trong cụm pnlPause (Menu tạm dừng)
            if (btnResume != null) btnResume.Click += btnResume_Click;
            if (btnRestart != null) btnRestart.Click += btnRestart_Click;
            if (btnExit != null) btnExit.Click += btnExit_Click;
            if (btnAbout != null) btnAbout.Click += btnAboutInPause_Click; // Nút About trong menu tạm dừng

            // Nút mở Menu chính (nếu có nút Back về Menu chính từ Pause)
            //if (btnBackToMenu != null) btnBackToMenu.Click += (s, e) => ShowMainMenu();

            // Các nút chức năng trên Menu chính (`pnlMainMenu`)
            if (btnMenuPlay != null) btnMenuPlay.Click += btnMenuPlay_Click;
            //if (btnMenuLeaderboard != null) btnMenuLeaderboard.Click += btnMenuLeaderboard_Click;
            if (btnMenuAbout != null) btnMenuAbout.Click += btnMenuAbout_Click;
            if (btnMenuExit != null) btnMenuExit.Click += btnMenuExit_Click;

            // Nút Đóng panel About (`btnCloseAbout`)
            if (btnCloseAbout != null) btnCloseAbout.Click += btnCloseAbout_Click;

            // Nút replay ngoài bàn cờ
            if (btnReplay != null) btnReplay.Click += btnRestart_Click;

            this.Deactivate += (s, ev) => CancelDrag();
            this.FormClosed += fGamePlay_FormClosed;
        }

        private void LoadGameSessionOrTutorial()
        {
            string saveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockBlast", "gamestate.dat");
            if (File.Exists(saveFilePath) && gameManager.LoadGameState(pnlDockPiece1, pnlDockPiece2, pnlDockPiece3))
            {
                tutorialManager.IsTutorialActive = false;
                if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;
                UpdateScoreLabels();
                pnlBoard.Invalidate();
                return;
            }

            if (File.Exists("tutorial_done.txt"))
            {
                tutorialManager.IsTutorialActive = false;
                if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;
                GenerateNewPieces();
            }
            else
            {
                tutorialManager.IsTutorialActive = true;
                LoadPuzzleLevel();
            }
        }

        // ================================================================
        //  2. QUẢN LÝ GIAO DIỆN & MENU (PANEL NAVIGATION)
        // ================================================================

        private void ShowMainMenu()
        {
            CancelDrag();
            // Ẩn giao diện bàn chơi và các panel phụ khác
            if (pnlBoard != null) pnlBoard.Visible = false;
            if (pnlDock != null) pnlDock.Visible = false;
            if (pnlHeader != null) pnlHeader.Visible = false;
            if (pnlPause != null) pnlPause.Visible = false;
            if (pnlGameOver != null) pnlGameOver.Visible = false;
            if (pnlAbout != null) pnlAbout.Visible = false;

            // Hiển thị Menu chính và căn giữa Form
            if (pnlMainMenu != null)
            {
                pnlMainMenu.Parent = this;
                pnlMainMenu.Location = new Point(
                    (this.ClientSize.Width - pnlMainMenu.Width) / 2,
                    (this.ClientSize.Height - pnlMainMenu.Height) / 2
                );
                pnlMainMenu.Visible = true;
                pnlMainMenu.BringToFront();
            }
        }

        // 1. Nút "Bắt đầu chơi" trên Menu chính
        private void btnMenuPlay_Click(object sender, EventArgs e)
        {
            if (pnlMainMenu != null) pnlMainMenu.Visible = false;

            if (pnlBoard != null) pnlBoard.Visible = true;
            if (pnlDock != null) pnlDock.Visible = true;
            if (pnlHeader != null) pnlHeader.Visible = true;

            // Kiểm tra: Nếu game trước đó vẫn còn dang dở và chưa thua thì tiếp tục, ngược lại tạo mới
            if (isGameInProgress && !isGameOver)
            {
                gameManager.IsPaused = false;
                pnlBoard.Invalidate();
            }
            else
            {
                LoadGameSessionOrTutorial();
                isGameInProgress = true;
                isGameOver = false;
            }
        }

        // 2. Nút "Bảng xếp hạng" trên Menu chính
        private void btnMenuLeaderboard_Click(object sender, EventArgs e)
        {
            try
            {
                var topScores = ScoreManager.LoadLeaderboardScores();
                string leaderboardText = "--- BẢNG XẾP HẠNG TOP CAO THỦ ---\n\n";
                int rank = 1;
                foreach (var entry in topScores)
                {
                    leaderboardText += $"{rank}. {entry.PlayerName}: {entry.Score} điểm ({entry.Date:dd/MM/yyyy})\n";
                    rank++;
                    if (rank > 10) break;
                }
                if (topScores.Count == 0) leaderboardText += "Chưa có dữ liệu thành tích nào!";

                MessageBox.Show(leaderboardText, "Bảng Xếp Hạng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { }
        }

        // 3. Nút "Thông tin nhóm" trên Menu chính
        private void btnMenuAbout_Click(object sender, EventArgs e)
        {
            if (pnlMainMenu != null) pnlMainMenu.Visible = false;
            if (pnlAbout != null)
            {
                pnlAbout.Parent = this;
                pnlAbout.Location = new Point(
                    (this.ClientSize.Width - pnlAbout.Width) / 2,
                    (this.ClientSize.Height - pnlAbout.Height) / 2
                );
                pnlAbout.Visible = true;
                pnlAbout.BringToFront();
            }
        }

        // 4. Nút "Thoát" trên Menu chính
        private void btnMenuExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn thoát game không?", "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        // 5. Nút "About" khi đang trong menu tạm dừng (`pnlPause`)
        private void btnAboutInPause_Click(object sender, EventArgs e)
        {
            if (pnlPause != null) pnlPause.Visible = false;
            if (pnlAbout != null)
            {
                pnlAbout.Parent = this;
                pnlAbout.Location = new Point(
                    (this.ClientSize.Width - pnlAbout.Width) / 2,
                    (this.ClientSize.Height - pnlAbout.Height) / 2
                );
                pnlAbout.Visible = true;
                pnlAbout.BringToFront();
            }
        }

        // 6. Nút đóng Panel About (`btnCloseAbout`)
        private void btnCloseAbout_Click(object sender, EventArgs e)
        {
            if (pnlAbout != null) pnlAbout.Visible = false;

            // Nếu đang trong game thì quay lại menu tạm dừng (`pnlPause`), ngược lại về Menu chính
            if (isGameInProgress && !isGameOver && gameManager.IsPaused)
            {
                if (pnlPause != null)
                {
                    pnlPause.Visible = true;
                    pnlPause.BringToFront();
                }
            }
            else
            {
                ShowMainMenu();
            }
        }

        // ================================================================
        //  3. BỐ CỤC GIAO DIỆN (LAYOUT)
        // ================================================================

        private void LayoutGameControls()
        {
            if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0) return;

            int cellSize = BoardRenderer.CalculateCellSize(new Size(500, 500));
            int boardPixelDim = BoardRenderer.GetBoardPixelSize(cellSize);
            pnlBoard.Size = new Size(boardPixelDim, boardPixelDim);

            int boardX = (this.ClientSize.Width - pnlBoard.Width) / 2;
            int availableHeight = this.ClientSize.Height - pnlHeader.Height - pnlDock.Height;
            int boardY = pnlHeader.Height + (availableHeight - pnlBoard.Height) / 2;
            pnlBoard.Location = new Point(boardX, boardY);

            if (lblTutorialGuide != null)
            {
                lblTutorialGuide.Size = new Size(pnlBoard.Width, 40);
                lblTutorialGuide.Location = new Point(boardX, Math.Max(pnlHeader.Height + 5, boardY - 45));
            }

            const int dockSlotSize = 140;
            const int dockSpacing = 30;
            const int totalDockWidth = dockSlotSize * 3 + dockSpacing * 2;
            int startX = (pnlDock.ClientSize.Width - totalDockWidth) / 2;
            int dockY = (pnlDock.ClientSize.Height - dockSlotSize) / 2;

            pnlDockPiece1.Size = new Size(dockSlotSize, dockSlotSize);
            pnlDockPiece2.Size = new Size(dockSlotSize, dockSlotSize);
            pnlDockPiece3.Size = new Size(dockSlotSize, dockSlotSize);

            pnlDockPiece1.Location = new Point(startX, dockY);
            pnlDockPiece2.Location = new Point(startX + dockSlotSize + dockSpacing, dockY);
            pnlDockPiece3.Location = new Point(startX + (dockSlotSize + dockSpacing) * 2, dockY);

            if (btnReplay != null)
            {
                int btnW = 180;
                int btnH = 50;
                btnReplay.Size = new Size(btnW, btnH);
                btnReplay.Location = new Point((pnlBoard.Width - btnW) / 2, (int)(pnlBoard.Height * 0.60f));
            }

            if (pnlGameOver != null) pnlGameOver.Visible = false;
        }

        private void ApplyModernTheme()
        {
            BoardRenderer.ApplyModernTheme(
                new Button[] { btnSound, btnResume, btnRestart, btnExit, btnReplay, btnPause, btnAbout, btnCloseAbout },
                new Panel[] { pnlDockPiece1, pnlDockPiece2, pnlDockPiece3 }
            );

            if (pnlScoreGroup != null)
            {
                pnlScoreGroup.Paint += BoardRenderer.DrawScoreGroupBackground;
            }
        }

        private void UpdateScoreLabels()
        {
            if (gameManager != null)
            {
                if (gameManager.CurrentScore > gameManager.HighScore)
                {
                    gameManager.HighScore = gameManager.CurrentScore;
                    ScoreManager.SaveHighScoreSilently(gameManager.HighScore);
                }
                BoardRenderer.UpdateScoreLabels(lblScore, lblBestScore, gameManager);
            }
        }



        // ================================================================
        //  4. SỰ KIỆN VẼ GIAO DIỆN (PAINT EVENTS)
        // ================================================================

        private void pnlBoard_Paint(object sender, PaintEventArgs e)
        {
            BoardRenderer.RenderBoard(e.Graphics, pnlBoard.Size, gameManager.Board, draggingShape, draggingColorIndex, hoverRow, hoverCol, flashingCells, flashScale);

            if (floatingTextAlpha > 0f) BoardRenderer.RenderFloatingText(e.Graphics, floatingComboText, floatingTextPos, floatingTextAlpha);
            if (isGameOver)
            {
                int current = gameManager.CurrentScore;
                bool isNewRecord = current > sessionGameInitialHighScore && current > 0;
                BoardRenderer.RenderGameOverOverlay(e.Graphics, pnlBoard.Size, current, gameManager.HighScore, isNewRecord);
            }
        }

        private void pnlDockPiece_Paint(object sender, PaintEventArgs e)
        {
            Panel pnl = sender as Panel;
            if (pnl == null || !pnl.Visible) return;

            if (pnl == activeDockPanel && draggingShape != null)
            {
                BoardRenderer.RenderEmptyDockSlot(e.Graphics, pnl.ClientRectangle);
                return;
            }
            BoardRenderer.RenderDockPiece(e.Graphics, pnl);
        }

        // ================================================================
        //  5. ĐIỀU KHIỂN KÉO THẢ (DRAG & DROP CONTROLS)
        // ================================================================

        private void pnlDockPiece_MouseDown(object sender, MouseEventArgs e)
        {
            if (isGameOver || isProcessingMove || gameManager.IsPaused) return;
            if (e.Button != MouseButtons.Left || draggingShape != null) return;

            Panel dock = sender as Panel;
            if (dock == null || dock.Tag == null) return;

            BlockPiece piece = (BlockPiece)dock.Tag;
            activeDockPanel = dock;
            draggingShape = piece.Shape;
            draggingColorIndex = piece.ColorIndex;
            SoundManager.PlayPickup();

            int cellSize = BoardRenderer.CalculateCellSize(pnlBoard.Size);
            Size pieceSize = BoardRenderer.GetPieceSize(draggingShape, cellSize);
            dragOffsetX = pieceSize.Width / 2;
            dragOffsetY = pieceSize.Height / 2;

            dock.Capture = true;
            dock.Invalidate();

            ShowDragGhost(cellSize);
            UpdateDragPosition();
        }

        private void fGamePlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (isGameOver || isProcessingMove || gameManager.IsPaused || draggingShape == null) return;
            UpdateDragPosition();
        }

        private void UpdateDragPosition()
        {
            Point boardPoint = pnlBoard.PointToClient(Cursor.Position);
            Point topLeft = new Point(boardPoint.X - dragOffsetX, boardPoint.Y - dragOffsetY - LIFT_OFFSET_Y);
            MoveDragGhost(pnlBoard.PointToScreen(topLeft));

            int newRow = -1, newCol = -1;
            if (BoardRenderer.GetMagneticCellAt(topLeft, pnlBoard.Size, out int r, out int c) &&
                gameManager.Board.CanPlaceShape(draggingShape, r, c))
            {
                newRow = r;
                newCol = c;
            }

            if (newRow != hoverRow || newCol != hoverCol)
            {
                hoverRow = newRow;
                hoverCol = newCol;
                pnlBoard.Invalidate();
            }
        }

        private async void fGamePlay_MouseUp(object sender, MouseEventArgs e)
        {
            if (draggingShape == null) return;
            if (isGameOver || isProcessingMove || gameManager.IsPaused || e.Button != MouseButtons.Left)
            {
                CancelDrag();
                return;
            }

            int[,] shapeToPlace = draggingShape;
            int colorToPlace = draggingColorIndex;
            Panel currentDock = activeDockPanel;
            int targetRow = hoverRow;
            int targetCol = hoverCol;

            EndDrag();

            if (targetRow == -1 || targetCol == -1 || !gameManager.Board.CanPlaceShape(shapeToPlace, targetRow, targetCol))
            {
                if (currentDock != null) currentDock.Invalidate();
                pnlBoard.Invalidate();
                return;
            }

            isProcessingMove = true;
            try
            {
                gameManager.Board.PlaceShape(shapeToPlace, colorToPlace, targetRow, targetCol);
                SoundManager.PlayDrop();

                if (currentDock != null) { currentDock.Tag = null; currentDock.Invalidate(); }
                pnlBoard.Refresh();

                if (!await AnimateLineClearAsync()) return;

                HandleGameProgressionAfterMove();
            }
            finally
            {
                isProcessingMove = false;
            }
        }

        private void HandleGameProgressionAfterMove()
        {
            if (tutorialManager.IsTutorialActive)
            {
                if (tutorialManager.NextLevel())
                {
                    LoadPuzzleLevel();
                }
                else
                {
                    if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;
                    try { File.WriteAllText("tutorial_done.txt", "completed"); } catch { }

                    tutorialManager.IsTutorialActive = false;
                    gameManager.ResetGame();
                    GenerateNewPieces();
                    pnlBoard.Invalidate();
                }
            }
            else
            {
                if (pnlDockPiece1.Tag == null && pnlDockPiece2.Tag == null && pnlDockPiece3.Tag == null)
                {
                    GenerateNewPieces();
                }
                CheckGameOver();
            }
        }

        private void EndDrag()
        {
            HideDragGhost();
            if (activeDockPanel != null && activeDockPanel.Capture) activeDockPanel.Capture = false;
            draggingShape = null;
            activeDockPanel = null;
            hoverRow = -1;
            hoverCol = -1;
        }

        private void CancelDrag()
        {
            if (draggingShape == null) return;
            Panel dock = activeDockPanel;
            EndDrag();
            if (dock != null) dock.Invalidate();
            pnlBoard.Invalidate();
        }

        private void ShowDragGhost(int cellSize)
        {
            if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostWindow { Owner = this };
            using (Bitmap bmp = BoardRenderer.CreateFloatingPieceBitmap(draggingShape, draggingColorIndex, cellSize, GHOST_PADDING))
            {
                Point boardPoint = pnlBoard.PointToClient(Cursor.Position);
                Point topLeft = pnlBoard.PointToScreen(new Point(boardPoint.X - dragOffsetX, boardPoint.Y - dragOffsetY - LIFT_OFFSET_Y));
                dragGhost.SetBitmap(bmp, new Point(topLeft.X - GHOST_PADDING, topLeft.Y - GHOST_PADDING));
            }
            if (!dragGhost.Visible) dragGhost.Show();
        }

        private void MoveDragGhost(Point pieceTopLeftOnScreen)
        {
            if (dragGhost != null && !dragGhost.IsDisposed && dragGhost.Visible)
                dragGhost.MoveTo(new Point(pieceTopLeftOnScreen.X - GHOST_PADDING, pieceTopLeftOnScreen.Y - GHOST_PADDING));
        }

        private void HideDragGhost()
        {
            if (dragGhost != null && !dragGhost.IsDisposed && dragGhost.Visible) dragGhost.Hide();
        }

        // ================================================================
        //  6. HOẠT HỌA VÀ HIỆU ỨNG (ANIMATIONS & EFFECTS)
        // ================================================================

        private async Task<bool> AnimateLineClearAsync()
        {
            List<int> fullRows = new List<int>();
            List<int> fullCols = new List<int>();
            gameManager.Board.FindFullLines(fullRows, fullCols);

            int linesCleared = fullRows.Count + fullCols.Count;
            if (linesCleared == 0) return true;

            int scoreGained = GameBoard.CalculateLineScore(linesCleared);

            foreach (int r in fullRows) for (int c = 0; c < GameBoard.BOARD_SIZE; c++) flashingCells.Add(new Point(c, r));
            foreach (int c in fullCols) for (int r = 0; r < GameBoard.BOARD_SIZE; r++) flashingCells.Add(new Point(c, r));

            SoundManager.PlayLineClear(linesCleared);
            flashScale = 1f;
            pnlBoard.Refresh();
            if (!await DelayAsync(120)) return false;

            if (linesCleared >= 2 && !await ShakeBoardAsync()) return false;

            for (int i = 1; i <= 5; i++)
            {
                flashScale = 1f - i / 5f;
                pnlBoard.Refresh();
                if (!await DelayAsync(18)) return false;
            }

            string comboText = BoardRenderer.BuildComboText(linesCleared, scoreGained);
            PointF textCenter = BoardRenderer.GetCellsCenter(pnlBoard.Size, flashingCells);
            ShowFloatingText(comboText, textCenter);

            gameManager.Board.ClearLines(fullRows, fullCols);
            flashingCells.Clear();
            flashScale = 1f;

            gameManager.AddScore(scoreGained);
            UpdateScoreLabels();
            pnlBoard.Refresh();
            return true;
        }

        private async Task<bool> ShakeBoardAsync()
        {
            Point origin = pnlBoard.Location;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    int dx = fxRandom.Next(3, 6) * (fxRandom.Next(2) == 0 ? -1 : 1);
                    int dy = fxRandom.Next(3, 6) * (fxRandom.Next(2) == 0 ? -1 : 1);
                    pnlBoard.Location = new Point(origin.X + dx, origin.Y + dy);
                    if (!await DelayAsync(25)) return false;
                }
            }
            finally
            {
                if (!pnlBoard.IsDisposed) pnlBoard.Location = origin;
            }
            return true;
        }

        private void ShowFloatingText(string text, PointF center)
        {
            floatingComboText = text;
            floatingTextPos = center;
            floatingTextAlpha = 1.6f;
            floatingTextTimer.Start();
        }

        private void FloatingTextTimer_Tick(object sender, EventArgs e)
        {
            floatingTextPos = new PointF(floatingTextPos.X, floatingTextPos.Y - 1.2f);
            floatingTextAlpha -= 0.03f;
            if (floatingTextAlpha <= 0f) { floatingTextAlpha = 0f; floatingComboText = ""; floatingTextTimer.Stop(); }
            pnlBoard.Invalidate();
        }

        private async Task<bool> DelayAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
            return !IsDisposed && !Disposing;
        }

        // ================================================================
        //  7. XỬ LÝ GAMEPLAY & MENU TẠM DỪNG (PAUSE ACTIONS)
        // ================================================================

        private void LoadPuzzleLevel()
        {
            TutorialStep level = tutorialManager.GetCurrentLevel();
            if (level == null) return;

            if (lblTutorialGuide != null)
            {
                lblTutorialGuide.Text = $"{level.Title}: {level.Instruction}";
                lblTutorialGuide.Visible = true;
            }

            gameManager.Board.SetGrid(level.BoardLayout);
            pnlDockPiece1.Tag = null; pnlDockPiece1.Visible = true; pnlDockPiece1.Invalidate();
            pnlDockPiece3.Tag = null; pnlDockPiece3.Visible = true; pnlDockPiece3.Invalidate();
            pnlDockPiece2.Tag = new BlockPiece(level.DockPieceShape, level.ColorIndex);
            pnlDockPiece2.Visible = true; pnlDockPiece2.Invalidate();
            pnlBoard.Invalidate();
        }

        // Hàm gọi để sinh và hiển thị 3 khối mới lên Dock
        private void GenerateNewPieces()
        {
            // Gọi hàm tối ưu từ GameManager để lấy bộ 3 khối chắc chắn giải được đồng thời
            List<BlockPiece> validPieces = gameManager.GenerateValidPieceSet();

            if (validPieces != null && validPieces.Count >= 3)
            {
                SetupDockPiece(pnlDockPiece1, validPieces[0]);
                SetupDockPiece(pnlDockPiece2, validPieces[1]);
                SetupDockPiece(pnlDockPiece3, validPieces[2]);
            }
        }

        private void SetupDockPiece(Panel dockPanel, BlockPiece piece)
        {
            dockPanel.Tag = piece;
            dockPanel.Visible = true;
            dockPanel.Invalidate();
        }

        private void CheckGameOver()
        {
            List<BlockPiece> remainingPieces = new List<BlockPiece>();
            if (pnlDockPiece1.Tag != null) remainingPieces.Add((BlockPiece)pnlDockPiece1.Tag);
            if (pnlDockPiece2.Tag != null) remainingPieces.Add((BlockPiece)pnlDockPiece2.Tag);
            if (pnlDockPiece3.Tag != null) remainingPieces.Add((BlockPiece)pnlDockPiece3.Tag);

            if (gameManager.CheckGameOver(remainingPieces)) ShowGameOverModal();
        }

        private void ShowGameOverModal()
        {
            CancelDrag();
            isGameOver = true;
            isGameInProgress = false; // Hết ván, đánh dấu là không còn game dang dở

            int current = gameManager.CurrentScore;
            if (current > gameManager.HighScore) gameManager.HighScore = current;
            ScoreManager.SaveHighScoreSilently(gameManager.HighScore);

            // Lưu điểm số vào bảng xếp hạng Top 10
            ScoreManager.SaveLeaderboardScore("Player", current);

            try
            {
                string saveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockBlast", "gamestate.dat");
                if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
            }
            catch { }

            UpdateScoreLabels();
            SoundManager.PlayGameOver();
            if (pnlGameOver != null) pnlGameOver.Visible = false;
            if (btnReplay != null)
            {
                btnReplay.Visible = true;
                btnReplay.BringToFront();
            }
            pnlBoard.Invalidate();
        }

        // Mở Panel Menu Tạm dừng (`pnlPause`) khi đang chơi
        private void btnOpenMenu_Click(object sender, EventArgs e)
        {
            CancelDrag();
            gameManager.IsPaused = true;

            if (pnlPause != null)
            {
                pnlPause.Parent = this;
                pnlPause.Location = new Point(
                    (this.ClientSize.Width - pnlPause.Width) / 2,
                    (this.ClientSize.Height - pnlPause.Height) / 2
                );
                pnlPause.Visible = true;
                pnlPause.BringToFront();
            }
            pnlBoard.Invalidate();
        }

        // Nút "Tiếp tục" trong menu tạm dừng
        private void btnResume_Click(object sender, EventArgs e)
        {
            gameManager.IsPaused = false;
            if (pnlPause != null)
            {
                pnlPause.Visible = false;
            }
            pnlBoard.Invalidate();
        }

        // Nút "Chơi lại từ đầu" (Restart / Replay)
        private void btnRestart_Click(object sender, EventArgs e)
        {
            if (isProcessingMove) return;
            CancelDrag();

            if (pnlPause != null) pnlPause.Visible = false;

            isGameOver = false;
            isGameInProgress = true;
            if (btnReplay != null) btnReplay.Visible = false;

            gameManager.ResetGame();
            gameManager.IsPaused = false;
            sessionGameInitialHighScore = gameManager.HighScore;

            floatingTextTimer.Stop();
            floatingTextAlpha = 0f;
            floatingComboText = "";

            UpdateScoreLabels();
            GenerateNewPieces();
            pnlBoard.Invalidate();
        }

        // Nút "Thoát ứng dụng"
        private void btnExit_Click(object sender, EventArgs e)
        {
            CancelDrag();
            if (MessageBox.Show("Bạn có chắc chắn muốn thoát game không?", "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void fGamePlay_FormClosed(object sender, FormClosedEventArgs e)
        {
            floatingTextTimer.Stop();
            floatingTextTimer.Dispose();
            if (dragGhost != null && !dragGhost.IsDisposed) dragGhost.Dispose();

            if (!isGameOver && isGameInProgress)
            {
                gameManager.SaveGameState(pnlDockPiece1, pnlDockPiece2, pnlDockPiece3);
            }
        }
    }
}