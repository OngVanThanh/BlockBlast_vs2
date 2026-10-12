using BlockBlast;
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
        private Item draggingItem = null; // Theo dõi Item nào đang được kéo
        private readonly Dictionary<ItemType, Label> itemBadgeLabels = new Dictionary<ItemType, Label>();
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

        private Point slingAnchor = Point.Empty;     // Điểm neo của ná (tâm slot)
        private Point slingPullPos = Point.Empty;    // Vị trí chuột đang kéo ná
        private const int MAX_PULL_DISTANCE = 140;   // Khoảng cách kéo ná tối đa
        private readonly Timer slingshotTimer;
        private DateTime tensionMaxStartTime = DateTime.MinValue;
        private bool isTensionMax = false;
        private bool isSnapping = false;
        private Control activeItemSlotCtrl = null;

        public fGamePlay()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            floatingTextTimer = new Timer { Interval = 16 };
            floatingTextTimer.Tick += FloatingTextTimer_Tick;

            slingshotTimer = new Timer { Interval = 16 };
            slingshotTimer.Tick += SlingshotTimer_Tick;
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
            InitItemUI();
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

            // Tự động căn chỉnh Panel Item bám ngay cạnh phải bàn cờ, cao bằng bàn cờ
            if (pnlMenuIteam != null)
            {
                int panelWidth = 120;
                pnlMenuIteam.Size = new Size(panelWidth, pnlBoard.Height);
                pnlMenuIteam.Location = new Point(pnlBoard.Right + 20, pnlBoard.Top);
                pnlMenuIteam.BringToFront();
            }

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
            if (draggingItem != null)
            {
                HandleItemDrop();
                return;
            }
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

        private void InitItemUI()
        {
            if (pnlMenuIteam == null) return;

            pnlMenuIteam.Controls.Clear();
            pnlMenuIteam.BackColor = Color.FromArgb(18, 21, 32);

            // 5 Item mới chuẩn xác 100%
            var itemConfig = new (ItemType Type, string Icon)[]
            {
        (ItemType.RerollDock, "🔄"),
        (ItemType.Bomb3x3, "💣"),
        (ItemType.CrossClear, "➕"),
        (ItemType.ColorLightning, "⚡"),
        (ItemType.GravityFall, "⬇️")
            };

            int totalHeight = pnlMenuIteam.Height;
            int spacing = 8;
            int slotHeight = Math.Max(50, (totalHeight - (spacing * (itemConfig.Length + 1))) / itemConfig.Length);

            for (int i = 0; i < itemConfig.Length; i++)
            {
                var config = itemConfig[i];
                Item item = gameManager.ItemsInventory[config.Type];

                Panel slot = new Panel
                {
                    Size = new Size(pnlMenuIteam.Width - 16, slotHeight),
                    Location = new Point(8, spacing + i * (slotHeight + spacing)),
                    BackColor = Color.FromArgb(22, 25, 38),
                    Tag = item,
                    Cursor = Cursors.Default
                };

                Label lblName = new Label
                {
                    Text = $"{config.Icon} {item.Name}",
                    Dock = DockStyle.Top,
                    Height = slotHeight - 24,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(90, 95, 115),
                    Tag = item
                };

                Label lblBadge = new Label
                {
                    Text = $"x{item.Count}",
                    Dock = DockStyle.Bottom,
                    Height = 22,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 75, 95),
                    Tag = item
                };

                slot.Controls.Add(lblName);
                slot.Controls.Add(lblBadge);

                // Gắn sự kiện chuột
                slot.MouseDown += ItemSlot_MouseDown;
                lblName.MouseDown += ItemSlot_MouseDown;
                lblBadge.MouseDown += ItemSlot_MouseDown;

                slot.MouseMove += ItemSlot_MouseMove;
                lblName.MouseMove += ItemSlot_MouseMove;
                lblBadge.MouseMove += ItemSlot_MouseMove;

                slot.MouseUp += ItemSlot_MouseUp;
                lblName.MouseUp += ItemSlot_MouseUp;
                lblBadge.MouseUp += ItemSlot_MouseUp;

                pnlMenuIteam.Controls.Add(slot);
                itemBadgeLabels[config.Type] = lblBadge;
            }

            UpdateItemUI();
        }

        private void UpdateItemUI()
        {
            foreach (var kvp in itemBadgeLabels)
            {
                ItemType type = kvp.Key;
                Label lblBadge = kvp.Value;
                if (!gameManager.ItemsInventory.ContainsKey(type)) continue;

                Item item = gameManager.ItemsInventory[type];
                lblBadge.Text = $"x{item.Count}";

                Panel slot = lblBadge.Parent as Panel;
                if (slot != null)
                {
                    if (item.Count <= 0)
                    {
                        // HẾT ITEM -> TẮT PHÁT SÁNG, NỀN TỐI CHÌM, CHUỘT THƯỜNG
                        slot.BackColor = Color.FromArgb(22, 25, 38);
                        slot.Cursor = Cursors.Default;
                        lblBadge.ForeColor = Color.FromArgb(80, 85, 105);
                        if (slot.Controls.Count > 0) slot.Controls[0].ForeColor = Color.FromArgb(90, 95, 115);
                    }
                    else
                    {
                        // CÒN ITEM -> NỀN SÁNG VỪA, CHUỘT BÀN TAY
                        bool isSelected = (draggingItem == item);
                        slot.BackColor = isSelected ? Color.FromArgb(60, 85, 150) : Color.FromArgb(30, 35, 54);
                        slot.Cursor = Cursors.Hand;
                        lblBadge.ForeColor = Color.FromArgb(255, 196, 77);
                        if (slot.Controls.Count > 0) slot.Controls[0].ForeColor = Color.White;
                    }
                }
            }
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
            // Thưởng Item khi ăn Combo n >= 2
            if (linesCleared >= 2)
            {
                Item rewarded = gameManager.AwardComboItem(linesCleared);
                if (rewarded != null)
                {
                    UpdateItemUI(); // Cập nhật số lượng trên giao diện ngay
                }
            }
            UpdateScoreLabels();
            pnlBoard.Refresh();
            return true;
        }

        private async Task<bool> ShakePanelAsync(Control ctrl)
        {
            if (ctrl == null) return true;
            Point origin = ctrl.Location;
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    int dx = fxRandom.Next(4, 7) * (fxRandom.Next(2) == 0 ? -1 : 1);
                    int dy = fxRandom.Next(2, 5) * (fxRandom.Next(2) == 0 ? -1 : 1);
                    ctrl.Location = new Point(origin.X + dx, origin.Y + dy);
                    if (!await DelayAsync(25)) return false;
                }
            }
            finally
            {
                if (!ctrl.IsDisposed) ctrl.Location = origin;
            }
            return true;
        }

        private async Task<bool> ShakeBoardAsync()
        {
            return await ShakePanelAsync(pnlBoard);
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

            if (gameManager.CheckGameOverWithItems(remainingPieces))
            {
                ShowGameOverModal();
            }
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
            slingshotTimer.Stop();
            slingshotTimer.Dispose();
            if (dragGhost != null && !dragGhost.IsDisposed) dragGhost.Dispose();

            if (!isGameOver && isGameInProgress)
            {
                gameManager.SaveGameState(pnlDockPiece1, pnlDockPiece2, pnlDockPiece3);
            }
        }

        private void ItemSlot_MouseDown(object sender, MouseEventArgs e)
        {
            if (isGameOver || isProcessingMove || gameManager.IsPaused) return;
            if (e.Button != MouseButtons.Left) return;

            Control ctrl = sender as Control;
            Item item = ctrl?.Tag as Item;
            if (item == null || item.Count <= 0) return;

            // 1. ĐỔI GẠCH: Bấm phát ăn ngay
            if (item.Type == ItemType.RerollDock)
            {
                item.TryConsume();
                gameManager.SaveItemsJson();
                GenerateNewPieces();
                SoundManager.PlayPickup();
                ShowFloatingText("🔄 ĐỔI GẠCH!", new PointF(pnlDock.Left + pnlDock.Width / 2, pnlDock.Top + 20));
                UpdateItemUI();
                pnlBoard.Invalidate();
                return;
            }

            // 2. KÉO NÁ CAO SU VẬT LÝ
            draggingItem = item;
            activeItemSlotCtrl = ctrl;
            slingAnchor = ctrl.PointToScreen(new Point(ctrl.Width / 2, ctrl.Height / 2));
            slingPullPos = Cursor.Position;
            ctrl.Capture = true;

            tensionMaxStartTime = DateTime.MinValue;
            isTensionMax = false;
            isSnapping = false;

            RenderSlingshotGhost();
            slingshotTimer.Start();
            UpdateItemUI();
        }

        private void ItemSlot_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggingItem != null && !isSnapping)
            {
                Point mouse = Cursor.Position;
                int dx = mouse.X - slingAnchor.X;
                int dy = mouse.Y - slingAnchor.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist > MAX_PULL_DISTANCE)
                {
                    dx = (int)(dx * MAX_PULL_DISTANCE / dist);
                    dy = (int)(dy * MAX_PULL_DISTANCE / dist);
                    dist = MAX_PULL_DISTANCE;
                }

                slingPullPos = new Point(slingAnchor.X + dx, slingAnchor.Y + dy);

                if (dist >= MAX_PULL_DISTANCE * 0.95)
                {
                    if (!isTensionMax)
                    {
                        isTensionMax = true;
                        tensionMaxStartTime = DateTime.Now;
                    }
                }
                else
                {
                    isTensionMax = false;
                    tensionMaxStartTime = DateTime.MinValue;
                }

                RenderSlingshotGhost();
            }
        }

        private void ItemSlot_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSnapping) return;
            slingshotTimer.Stop();

            Control ctrl = sender as Control;
            if (ctrl != null) ctrl.Capture = false;
            if (activeItemSlotCtrl != null)
            {
                activeItemSlotCtrl.Capture = false;
                activeItemSlotCtrl = null;
            }

            if (draggingItem != null)
            {
                HandleItemDrop();
            }
        }

        private void SlingshotTimer_Tick(object sender, EventArgs e)
        {
            if (draggingItem == null || isSnapping) return;

            // Kiểm tra giữ độ căng tối đa quá 1.5 giây -> Đứt dây thun!
            if (isTensionMax && tensionMaxStartTime != DateTime.MinValue)
            {
                double elapsedSec = (DateTime.Now - tensionMaxStartTime).TotalSeconds;
                if (elapsedSec >= 1.5)
                {
                    _ = HandleSlingshotSnapAsync();
                    return;
                }
            }

            RenderSlingshotGhost();
        }

        // Xử lý sự cố: Dây thun bị đứt do kéo quá căng và giữ quá lâu
        private async Task HandleSlingshotSnapAsync()
        {
            isSnapping = true;
            slingshotTimer.Stop();

            if (activeItemSlotCtrl != null)
            {
                activeItemSlotCtrl.Capture = false;
                activeItemSlotCtrl = null;
            }

            Item heldItem = draggingItem;
            draggingItem = null;

            // 1. Âm thanh PẰNG! đứt dây ná
            SoundManager.PlaySnap();

            // 2. Hiệu ứng thị giác dây thun đứt giật ngược
            RenderBrokenSnapGhost();
            await Task.Delay(180);
            HideDragGhost();

            // 3. Phạt mất Item:
            // - Mất item đang cầm trên tay
            if (heldItem != null)
            {
                heldItem.TryConsume();
            }

            // - Rơi mất thêm 1 item bất kỳ còn tồn kho (quét ưu tiên từ trên xuống dưới)
            ItemType[] priorityOrder = {
                ItemType.RerollDock,
                ItemType.Bomb3x3,
                ItemType.CrossClear,
                ItemType.ColorLightning,
                ItemType.GravityFall
            };

            Item droppedExtra = null;
            foreach (var t in priorityOrder)
            {
                if (gameManager.ItemsInventory.TryGetValue(t, out var itm) && itm.Count > 0)
                {
                    itm.TryConsume();
                    droppedExtra = itm;
                    break;
                }
            }

            gameManager.SaveItemsJson();
            UpdateItemUI();

            // 4. Rung chấn Panel Items và hiển thị chữ nổi thông báo
            if (pnlMenuIteam != null)
            {
                string dropText = droppedExtra != null
                    ? $"💥 ĐỨT DÂY THUN! RƠI {heldItem?.Name} & {droppedExtra.Name}!"
                    : $"💥 ĐỨT DÂY THUN! MẤT {heldItem?.Name}!";
                ShowFloatingText(dropText, new PointF(pnlMenuIteam.Left - 50, pnlMenuIteam.Top + pnlMenuIteam.Height / 2));
                await ShakePanelAsync(pnlMenuIteam);
            }

            isSnapping = false;
            isTensionMax = false;
            tensionMaxStartTime = DateTime.MinValue;
            CheckGameOver();
        }

        private async void HandleItemDrop()
        {
            if (draggingItem == null || isProcessingMove || isSnapping) return;
            isProcessingMove = true;

            Item item = draggingItem;
            draggingItem = null;

            int pullDx = slingPullPos.X - slingAnchor.X;
            int pullDy = slingPullPos.Y - slingAnchor.Y;
            double pullDist = Math.Sqrt(pullDx * pullDx + pullDy * pullDy);

            // Kéo nhẹ quá (< 20px) thì coi như hủy bắn, không mất đồ
            if (pullDist < 20)
            {
                HideDragGhost();
                isProcessingMove = false;
                UpdateItemUI();
                pnlBoard.Invalidate();
                return;
            }

            // TÍNH TOÁN ĐIỂM RƠI VẬT LÝ (BẮN NGƯỢC CHIỀU KÉO)
            const double POWER_MULTIPLIER = 4.2;
            int landScreenX = (int)(slingAnchor.X - pullDx * POWER_MULTIPLIER);
            int landScreenY = (int)(slingAnchor.Y - pullDy * POWER_MULTIPLIER);
            Point targetScreen = new Point(landScreenX, landScreenY);

            Point boardPos = pnlBoard.PointToClient(targetScreen);
            bool isHitBoard = pnlBoard.ClientRectangle.Contains(boardPos);

            // Tiêu hao item & lưu
            item.TryConsume();
            gameManager.SaveItemsJson();
            UpdateItemUI();

            // 1. HOẠT HỌA QUẢ BOM THỰC SỰ BAY QUA MÀN HÌNH THEO ĐƯỜNG CONG PARABOL
            await FlyProjectileAsync(item, slingPullPos, targetScreen, arcHeight: 70);

            // TRƯỜNG HỢP 1: BẮN TRÚNG BÀN CỜ
            if (isHitBoard)
            {
                int cellSize = BoardRenderer.CalculateCellSize(pnlBoard.Size);
                int targetCol = (boardPos.X - BoardRenderer.MARGIN) / (cellSize + BoardRenderer.GAP);
                int targetRow = (boardPos.Y - BoardRenderer.MARGIN) / (cellSize + BoardRenderer.GAP);

                if (targetRow >= 0 && targetRow < 8 && targetCol >= 0 && targetCol < 8)
                {
                    flashingCells.Clear();
                    int clearedCells = 0;

                    switch (item.Type)
                    {
                        case ItemType.Bomb3x3:
                            for (int r = targetRow - 1; r <= targetRow + 1; r++)
                                for (int c = targetCol - 1; c <= targetCol + 1; c++)
                                    if (r >= 0 && r < 8 && c >= 0 && c < 8 && gameManager.Board.GetCell(r, c) != 0)
                                        flashingCells.Add(new Point(c, r));

                            clearedCells = gameManager.Board.ClearArea3x3(targetRow, targetCol);
                            ShowFloatingText("💥 TRÚNG ĐÍCH! BOM 3X3", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
                            break;

                        case ItemType.CrossClear:
                            for (int i = 0; i < 8; i++)
                            {
                                if (gameManager.Board.GetCell(targetRow, i) != 0) flashingCells.Add(new Point(i, targetRow));
                                if (gameManager.Board.GetCell(i, targetCol) != 0) flashingCells.Add(new Point(targetCol, i));
                            }
                            clearedCells = gameManager.Board.ClearCross(targetRow, targetCol);
                            ShowFloatingText("⚡ TRÚNG ĐÍCH! CHỮ THẬP", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
                            break;

                        case ItemType.ColorLightning:
                            int color = gameManager.Board.GetCell(targetRow, targetCol);
                            if (color > 0)
                            {
                                for (int r = 0; r < 8; r++)
                                    for (int c = 0; c < 8; c++)
                                        if (gameManager.Board.GetCell(r, c) == color)
                                            flashingCells.Add(new Point(c, r));

                                clearedCells = gameManager.Board.ClearColor(color);
                                ShowFloatingText("⚡ SẤM SÉT TRÚNG MÀU!", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
                            }
                            else
                            {
                                ShowFloatingText("⚡ SÉT ĐÁNH VÀO Ô TRỐNG!", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
                            }
                            break;

                        case ItemType.GravityFall:
                            gameManager.Board.ApplyGravity();
                            ShowFloatingText("⬇️ TRỌNG LỰC!", new PointF(pnlBoard.Width / 2, pnlBoard.Height / 2));
                            break;
                    }

                    // TÍNH ĐIỂM: 5 điểm cho mỗi ô gạch bị nổ (tương đương 2/5 mức điểm so với xếp gạch)
                    if (clearedCells > 0)
                    {
                        int scoreBonus = clearedCells * 5;
                        gameManager.AddScore(scoreBonus);
                        UpdateScoreLabels();
                    }

                    SoundManager.PlayLineClear(3);
                    pnlBoard.Refresh();
                    await ShakeBoardAsync();
                    flashingCells.Clear();

                    // NẾU LÀ TRỌNG LỰC: KIỂM TRA VÀ TỰ ĐỘNG NỔ CÁC DÒNG ĐẦY VỪA HÌNH THÀNH
                    if (item.Type == ItemType.GravityFall)
                    {
                        await AnimateLineClearAsync();
                    }

                    pnlBoard.Invalidate();
                    CheckGameOver();
                }
            }
            // TRƯỜNG HỢP 2: BẮN HỤT (BAY RA NGOÀI MÉP BÀN CỜ)
            else
            {
                PointF failPos = new PointF(Math.Max(10, Math.Min(pnlBoard.Width - 100, boardPos.X)),
                                            Math.Max(10, Math.Min(pnlBoard.Height - 30, boardPos.Y)));
                ShowFloatingText("💨 BẮN HỤT RA NGOÀI!", failPos);
                SoundManager.PlayDrop();
                CheckGameOver();
            }

            isProcessingMove = false;
            pnlBoard.Invalidate();
            UpdateItemUI();
        }

        // Vẽ toàn bộ ná cao su, 2 sợi dây thun, quả đạn và chuỗi hạt đạn đạo trên toàn màn hình không bị clip
        private void RenderSlingshotGhost()
        {
            if (draggingItem == null || isSnapping) return;

            Size formSize = this.ClientSize;
            if (formSize.Width <= 0 || formSize.Height <= 0) return;

            Point formTopLeft = this.PointToScreen(Point.Empty);
            Point formAnchor = this.PointToClient(slingAnchor);
            Point formPull = this.PointToClient(slingPullPos);

            // Hiệu ứng rung tay khi kéo quá căng
            bool flashRed = false;
            if (isTensionMax && tensionMaxStartTime != DateTime.MinValue)
            {
                double elapsed = (DateTime.Now - tensionMaxStartTime).TotalSeconds;
                flashRed = ((int)(elapsed * 8) % 2 == 0); // Nhấp nháy đỏ 4Hz
                formPull.X += fxRandom.Next(-2, 3);
                formPull.Y += fxRandom.Next(-2, 3);
            }

            using (Bitmap bmp = new Bitmap(formSize.Width, formSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // 1. Vẽ chạc ná cố định tại slot
                using (Pen forkPen = new Pen(Color.FromArgb(180, 120, 50), 4f))
                {
                    g.DrawLine(forkPen, formAnchor.X, formAnchor.Y - 18, formAnchor.X, formAnchor.Y + 18);
                }

                // 2. Vẽ 2 sợi dây thun ná cao su co giãn
                Color bandColor = isTensionMax
                    ? (flashRed ? Color.FromArgb(255, 45, 45) : Color.FromArgb(255, 180, 50))
                    : Color.FromArgb(220, 255, 180, 50);
                float bandWidth = isTensionMax ? 4f : 3f;

                using (Pen bandPen = new Pen(bandColor, bandWidth))
                {
                    g.DrawLine(bandPen, formAnchor.X, formAnchor.Y - 14, formPull.X, formPull.Y);
                    g.DrawLine(bandPen, formAnchor.X, formAnchor.Y + 14, formPull.X, formPull.Y);
                }

                // 3. Vẽ chuỗi chấm đạn đạo Parabol dài chỉ hướng ngược lại (hướng về bàn cờ)
                int pullDx = slingPullPos.X - slingAnchor.X;
                int pullDy = slingPullPos.Y - slingAnchor.Y;
                double dist = Math.Sqrt(pullDx * pullDx + pullDy * pullDy);

                if (dist > 15)
                {
                    const double POWER_MULTIPLIER = 4.2;
                    Point formTarget = new Point((int)(formAnchor.X - pullDx * POWER_MULTIPLIER),
                                                 (int)(formAnchor.Y - pullDy * POWER_MULTIPLIER));

                    int dotCount = 14;
                    for (int i = 1; i <= dotCount; i++)
                    {
                        float t = (float)i / dotCount;
                        float dotX = formPull.X + (formTarget.X - formPull.X) * t;
                        float dotY = formPull.Y + (formTarget.Y - formPull.Y) * t - (float)(60 * t * (1.0 - t));
                        float dotRadius = Math.Max(2f, 4.5f - t * 2f);

                        Color dotColor = isTensionMax
                            ? (flashRed ? Color.FromArgb(255, 255, 50, 50) : Color.FromArgb(230, 255, 180, 0))
                            : Color.FromArgb((int)(255 * (1f - t * 0.35f)), 255, 215, 0);

                        using (SolidBrush dotBrush = new SolidBrush(dotColor))
                        {
                            g.FillEllipse(dotBrush, dotX - dotRadius, dotY - dotRadius, dotRadius * 2, dotRadius * 2);
                        }
                    }
                }

                // 4. Vẽ quầng phát sáng và quả đạn ngay tại đầu tay kéo
                Color glowColor = isTensionMax ? Color.FromArgb(190, 230, 30, 30) : Color.FromArgb(170, 30, 40, 70);
                Color borderCol = isTensionMax ? Color.FromArgb(255, 50, 50) : Color.FromArgb(255, 204, 77);

                using (SolidBrush glow = new SolidBrush(glowColor))
                {
                    g.FillEllipse(glow, formPull.X - 25, formPull.Y - 25, 50, 50);
                }
                using (Pen borderPen = new Pen(borderCol, 2.5f))
                {
                    g.DrawEllipse(borderPen, formPull.X - 25, formPull.Y - 25, 50, 50);
                }

                string icon = draggingItem.Type == ItemType.Bomb3x3 ? "💣" :
                              draggingItem.Type == ItemType.CrossClear ? "➕" :
                              draggingItem.Type == ItemType.ColorLightning ? "⚡" : "⬇️";

                using (Font font = new Font("Segoe UI Emoji", 20F))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(icon, font, textBrush, formPull.X, formPull.Y, sf);
                }

                // 5. Cảnh báo độ căng tối đa
                if (isTensionMax && tensionMaxStartTime != DateTime.MinValue)
                {
                    double remain = Math.Max(0, 1.5 - (DateTime.Now - tensionMaxStartTime).TotalSeconds);
                    string warnText = $"⚠️ QUÁ CĂNG! {remain:F1}s";
                    using (Font warnFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                    using (SolidBrush warnBrush = new SolidBrush(Color.FromArgb(255, 60, 60)))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(warnText, warnFont, warnBrush, formPull.X, formPull.Y - 45, sf);
                    }
                }

                if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostWindow { Owner = this };
                dragGhost.SetBitmap(bmp, formTopLeft);
            }

            if (!dragGhost.Visible) dragGhost.Show();
        }

        // Vẽ dây thun đứt đôi giật ngược
        private void RenderBrokenSnapGhost()
        {
            Size formSize = this.ClientSize;
            if (formSize.Width <= 0 || formSize.Height <= 0) return;

            Point formTopLeft = this.PointToScreen(Point.Empty);
            Point formAnchor = this.PointToClient(slingAnchor);
            Point formPull = this.PointToClient(slingPullPos);
            Point mid = new Point((formAnchor.X + formPull.X) / 2, (formAnchor.Y + formPull.Y) / 2);

            using (Bitmap bmp = new Bitmap(formSize.Width, formSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Pen brokenPen = new Pen(Color.FromArgb(255, 50, 50), 3.5f))
                {
                    g.DrawLine(brokenPen, formAnchor.X, formAnchor.Y - 14, mid.X - 15, mid.Y - 25);
                    g.DrawLine(brokenPen, formAnchor.X, formAnchor.Y + 14, mid.X - 15, mid.Y + 25);
                }

                using (Font f = new Font("Segoe UI", 16F, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 40, 40)))
                {
                    g.DrawString("💥 ĐỨT!", f, b, mid.X, mid.Y - 15);
                }

                if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostWindow { Owner = this };
                dragGhost.SetBitmap(bmp, formTopLeft);
            }
            if (!dragGhost.Visible) dragGhost.Show();
        }

        // Hoạt họa quả bom thực sự bay theo đường cong Parabol từ ná đến điểm rơi trên bàn cờ
        private async Task FlyProjectileAsync(Item item, Point startScreen, Point targetScreen, int arcHeight = 70)
        {
            if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostWindow { Owner = this };

            int pSize = 80;
            using (Bitmap bombBmp = new Bitmap(pSize, pSize, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            {
                using (Graphics g = Graphics.FromImage(bombBmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    int cx = pSize / 2;
                    int cy = pSize / 2;

                    using (SolidBrush glow = new SolidBrush(Color.FromArgb(190, 255, 120, 30)))
                    {
                        g.FillEllipse(glow, cx - 22, cy - 22, 44, 44);
                    }
                    using (Pen borderPen = new Pen(Color.FromArgb(255, 230, 100), 2.5f))
                    {
                        g.DrawEllipse(borderPen, cx - 22, cy - 22, 44, 44);
                    }

                    string icon = item.Type == ItemType.Bomb3x3 ? "💣" :
                                  item.Type == ItemType.CrossClear ? "➕" :
                                  item.Type == ItemType.ColorLightning ? "⚡" : "⬇️";

                    using (Font font = new Font("Segoe UI Emoji", 18F))
                    using (SolidBrush textBrush = new SolidBrush(Color.White))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(icon, font, textBrush, cx, cy, sf);
                    }
                }

                const int steps = 15; // 15 khung hình ~ 240ms mượt mà
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    float curX = startScreen.X + (targetScreen.X - startScreen.X) * t;
                    float curY = startScreen.Y + (targetScreen.Y - startScreen.Y) * t - (float)(4 * arcHeight * t * (1.0 - t));

                    dragGhost.SetBitmap(bombBmp, new Point((int)curX - pSize / 2, (int)curY - pSize / 2));
                    if (!dragGhost.Visible) dragGhost.Show();
                    await Task.Delay(16); // 60 FPS
                }
            }

            HideDragGhost();
        }
    }
}