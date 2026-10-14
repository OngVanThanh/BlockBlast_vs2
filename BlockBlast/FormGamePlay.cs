using BlockBlast;
using BlockBlast.Module;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private DragGhostOverlay dragGhost;
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

        // Biến phục vụ hoạt họa VFX khi nổ Item trên bàn cờ
        private struct VfxParticle
        {
            public float X, Y, Vx, Vy, Size;
            public Color Color;
        }
        private bool isPlayingItemVfx = false;
        private ItemType activeVfxType;
        private float vfxProgress = 0f;
        private PointF vfxCenter = PointF.Empty;
        private readonly List<VfxParticle> vfxParticles = new List<VfxParticle>();
        private readonly List<PointF> vfxLightningTargets = new List<PointF>();

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

        private Button btnResetHistoryMenu;
        private Button btnResetHistoryPause;
        private Button btnMenuLeaderboard;
        private Panel pnlLeaderboard;
        private bool isDialogOpenedFromPause = false;

        private void fGamePlay_Load(object sender, EventArgs e)
        {
            try
            {
                gameManager.HighScore = ScoreManager.LoadSavedHighScore();
                sessionGameInitialHighScore = gameManager.HighScore;

                BoardRenderer.EnableDoubleBuffering(this);
                SoundManager.IsMuted = ScoreManager.LoadAudioSettings();

                if (btnReplay != null && btnReplay.Parent != pnlBoard) pnlBoard.Controls.Add(btnReplay);
                if (btnReplay != null) btnReplay.Visible = false;
                if (pnlGameOver != null) pnlGameOver.Visible = false;
                if (pnlPause != null) pnlPause.Visible = false;
                if (pnlAbout != null) pnlAbout.Visible = false;

                InitAboutDialog();
                InitTutorialLabel();
                InitResetHistoryButtons();
                InitLeaderboardButtons(); 
                InitLeaderboardDialog();
                LayoutGameControls();

                this.Resize += (s, ev) => { if (this.WindowState != FormWindowState.Minimized) LayoutGameControls(); };

                RegisterEventHandlers();
                ApplyModernTheme();
                UpdateScoreLabels();
                InitItemUI();
                LoadGameSessionOrTutorial();
                isGameInProgress = false;
                isGameOver = false;

                // Hiển thị Menu chính ngay khi mở ứng dụng
                ShowMainMenu();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Load Error]: {ex.Message}");
            }
        }

        private void InitResetHistoryButtons()
        {
            if (pnlMainMenu != null && btnResetHistoryMenu == null)
            {
                btnResetHistoryMenu = CreateResetButton(new Point(68, 175));
                pnlMainMenu.Controls.Add(btnResetHistoryMenu);
            }

            if (pnlPause != null && btnResetHistoryPause == null)
            {
                btnResetHistoryPause = CreateResetButton(new Point(68, 273));
                pnlPause.Controls.Add(btnResetHistoryPause);
            }
        }

        private Button CreateResetButton(Point location)
        {
            Button btn = new Button
            {
                Text = "RESET LỊCH SỬ",
                Location = location,
                Size = new Size(225, 37),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 210, 210),
                BackColor = Color.FromArgb(90, 35, 45),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(130, 45, 60);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 50, 70);
            btn.Click += btnResetHistory_Click;
            return btn;
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
            if (btnSound != null) btnSound.Click += btnSound_Click;

            // Nút mở Menu / Tạm dừng trên header trong game
            if (btnPause != null) btnPause.Click += btnOpenMenu_Click;
            // Các nút chức năng nằm trong cụm pnlPause (Menu tạm dừng)
            if (btnNewGameMenu != null) btnNewGameMenu.Click += btnNewGameMenu_Click;
            if (btnResume != null) btnResume.Click += btnResume_Click;
            if (btnRestart != null) btnRestart.Click += btnRestart_Click;
            if (btnExit != null) btnExit.Click += btnExit_Click;
            if (btnBackToMenu != null) btnBackToMenu.Click += btnBackToMenu_Click;

            // Các nút chức năng trên Menu chính (`pnlMainMenu`)
            if (btnMenuPlay != null) btnMenuPlay.Click += btnMenuPlay_Click;
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
            try
            {
                // 1. Nếu có ván chơi dở dang từ trước -> Load lại bàn cờ và dock
                if (gameManager.HasSavedGame && gameManager.LoadGameState(pnlDockPiece1, pnlDockPiece2, pnlDockPiece3))
                {
                    tutorialManager.IsTutorialActive = false;
                    if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;

                    isGameInProgress = true;
                    isGameOver = false;
                    UpdateScoreLabels();
                    pnlBoard.Invalidate();

                    if (pnlDockPiece1.Tag == null && pnlDockPiece2.Tag == null && pnlDockPiece3.Tag == null)
                    {
                        GenerateNewPieces();
                    }
                    return;
                }

                // 2. Kích hoạt Tutorial nếu chưa từng hoàn thành và chưa có điểm
                if (!tutorialManager.IsTutorialCompleted && gameManager.HighScore == 0)
                {
                    tutorialManager.ResetTutorial();
                    LoadPuzzleLevel();
                    return;
                }

                // 3. Đã học xong -> Vào ngay ván chơi thật
                tutorialManager.IsTutorialActive = false;
                if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;

                isGameInProgress = true;
                isGameOver = false;
                GenerateNewPieces();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadGameState Error]: {ex.Message}");
                // Fallback an toàn: nếu file save lỗi thì khởi động bàn cờ mới tinh
                gameManager.ResetGame();
                GenerateNewPieces();
            }
        }

        // ================================================================
        //  2. QUẢN LÝ GIAO DIỆN & MENU (PANEL NAVIGATION)
        // ================================================================



        private bool isMenuPaintRegistered = false;

        private void ShowMainMenu()
        {
            CancelDrag();

            // ẨN CÁC PANEL GAME ĐỂ KHÔNG BỊ VẼ NGẦM GÂY CHỚP MÀN HÌNH
            if (pnlBoard != null) pnlBoard.Visible = false;
            if (pnlDock != null) pnlDock.Visible = false;
            if (pnlHeader != null) pnlHeader.Visible = false;
            if (pnlMenuIteam != null) pnlMenuIteam.Visible = false;
            if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;
            if (pnlPause != null) pnlPause.Visible = false;
            if (pnlGameOver != null) pnlGameOver.Visible = false;
            if (pnlAbout != null) pnlAbout.Visible = false;

            if (pnlMainMenu != null)
            {
                // 1. Kiểm tra có ván chơi dở dang và chưa thua hay không
                bool canContinue = (isGameInProgress && !isGameOver) || gameManager.HasSavedGame;

                if (btnMenuPlay != null)
                {
                    btnMenuPlay.Visible = canContinue;
                    btnMenuPlay.BackColor = Color.FromArgb(41, 128, 185); // Xanh dương cho nút Tiếp tục
                }

                // 2. Danh sách các nút theo thứ tự
                List<Button> activeButtons = new List<Button>();
                if (btnNewGameMenu != null) activeButtons.Add(btnNewGameMenu);
                if (canContinue && btnMenuPlay != null) activeButtons.Add(btnMenuPlay);
                if (btnMenuLeaderboard != null) activeButtons.Add(btnMenuLeaderboard); // Nút Chiến Tích
                if (btnMenuAbout != null) activeButtons.Add(btnMenuAbout);
                if (btnResetHistoryMenu != null) activeButtons.Add(btnResetHistoryMenu);
                if (btnMenuExit != null) activeButtons.Add(btnMenuExit);

                int btnWidth = 280; // Tăng chiều rộng nút một chút cho cân đối với màn hình lớn
                int spacing = 14;
                int totalHeight = activeButtons.Count * 40 + (activeButtons.Count - 1) * spacing; // Tính tổng chiều cao cụm nút

                // 3. ĐẶT `pnlMainMenu` PHỦ KÍN FULL MÀN HÌNH
                pnlMainMenu.Parent = this;
                pnlMainMenu.Location = new Point(0, 0);
                pnlMainMenu.Size = this.ClientSize; // Khớp kích thước với form
                pnlMainMenu.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                pnlMainMenu.BackColor = Color.FromArgb(22, 26, 42);

                // Nếu muốn panel full màn hình không bị bo góc (tràn viền tuyệt đối), bạn có thể bỏ dòng ApplyRoundedRegion của pnlMainMenu đi hoặc giữ nguyên tùy ý
                // BoardRenderer.ApplyRoundedRegion(pnlMainMenu, 20); 

                // 4. Tính toán vị trí bắt đầu theo chiều dọc để toàn bộ cụm nút nằm CHÍNH GIỮA màn hình
                int startY = (pnlMainMenu.Height - totalHeight) / 2 - 30; // Trừ hao một chút để đẹp mắt nếu có logo phía trên
                int currentY = startY;

                foreach (Button btn in activeButtons)
                {
                    btn.Visible = true;
                    int btnHeight = (btn == btnNewGameMenu) ? 44 : 40;
                    btn.Size = new Size(btnWidth, btnHeight);

                    // Căn giữa theo chiều ngang của màn hình full size
                    int centerX = (pnlMainMenu.Width - btnWidth) / 2;
                    btn.Location = new Point(centerX, currentY);

                    BoardRenderer.ApplyRoundedRegion(btn, 12);
                    currentY += btnHeight + spacing;
                }

                if (!isMenuPaintRegistered)
                {
                    pnlMainMenu.Paint += PnlMainMenu_Paint;
                    isMenuPaintRegistered = true;
                }

                // Đưa lên lớp trên cùng để che phủ hoàn toàn giao diện game bên dưới
                pnlMainMenu.Visible = true;
                pnlMainMenu.BringToFront();
                pnlMainMenu.Invalidate();
            }
        }

        private void PnlMainMenu_Paint(object sender, PaintEventArgs e)
        {
            BoardRenderer.DrawMainMenuBackground(e.Graphics, pnlMainMenu.Width, pnlMainMenu.Height, gameManager.HighScore);
        }

        // 1. Nút "Tiếp tục" trên Menu chính
        private void btnMenuPlay_Click(object sender, EventArgs e)
        {
            if (pnlMainMenu != null) pnlMainMenu.Visible = false;

            // BẬT LẠI ĐẦY ĐỦ CÁC PANEL GAME (Tránh lỗi màn hình đen)
            if (pnlBoard != null) pnlBoard.Visible = true;
            if (pnlDock != null) pnlDock.Visible = true;
            if (pnlHeader != null) pnlHeader.Visible = true;
            if (pnlMenuIteam != null) pnlMenuIteam.Visible = true;

            // Nếu ván trước đang tạm dừng thì tiếp tục, ngược lại nạp ván từ file lưu
            if (isGameInProgress && !isGameOver)
            {
                gameManager.IsPaused = false;
                if (tutorialManager.IsTutorialActive && lblTutorialGuide != null) lblTutorialGuide.Visible = true;
                pnlBoard.Invalidate();
            }
            else
            {
                LoadGameSessionOrTutorial();
                isGameInProgress = true;
                isGameOver = false;
            }

            UpdateScoreLabels();
        }

        // 2. Nút "Bảng xếp hạng" trên Menu chính
        //private void btnMenuLeaderboard_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var topScores = ScoreManager.LoadLeaderboardScores();
        //        string leaderboardText = "--- BẢNG XẾP HẠNG TOP CAO THỦ ---\n\n";
        //        int rank = 1;
        //        foreach (var entry in topScores)
        //        {
        //            leaderboardText += $"{rank}. {entry.PlayerName}: {entry.Score} điểm ({entry.Date:dd/MM/yyyy})\n";
        //            rank++;
        //            if (rank > 10) break;
        //        }
        //        if (topScores.Count == 0) leaderboardText += "Chưa có dữ liệu thành tích nào!";

        //        MessageBox.Show(leaderboardText, "Bảng Xếp Hạng", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //    catch { }
        //}



        // 4. Nút "Thoát" trên Menu chính
        private void btnMenuExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn thoát game không?", "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        // 3. Nút "Thông tin nhóm" trên Menu chính
        private void btnMenuAbout_Click(object sender, EventArgs e)
        {
            ShowAboutDialog();
        }

        // Hiển thị Popup Thông tin nhóm dạng hộp thoại nổi giữa màn hình Form
        private void ShowAboutDialog()
        {
            if (pnlAbout == null) return;

            // 1. Tạm ẩn Menu chính hoặc Menu Pause để không bị vẽ đè nút lên nhau
            if (pnlMainMenu != null) pnlMainMenu.Visible = false;
            if (pnlPause != null) pnlPause.Visible = false;

            // 2. Định hình kích thước hộp thoại và căn ra CHÍNH GIỮA màn hình
            pnlAbout.Parent = this;
            int dialogW = Math.Min(680, Math.Max(500, this.ClientSize.Width - 40));
            int dialogH = Math.Min(540, Math.Max(400, this.ClientSize.Height - 40));
            pnlAbout.Size = new Size(dialogW, dialogH);
            pnlAbout.Location = new Point(
                (this.ClientSize.Width - pnlAbout.Width) / 2,
                (this.ClientSize.Height - pnlAbout.Height) / 2
            );

            // 3. Cắt bo góc theo kích thước mới và hiển thị lên trên cùng
            BoardRenderer.ApplyRoundedRegion(pnlAbout, 16);
            pnlAbout.Visible = true;
            pnlAbout.BringToFront();
            pnlAbout.Invalidate();
        }

        // 6. Nút đóng Panel About (`btnCloseAbout`)
        private void btnCloseAbout_Click(object sender, EventArgs e)
        {
            if (pnlAbout != null) pnlAbout.Visible = false;

            // Nếu đang trong game tạm dừng thì quay lại pnlPause, ngược lại về Menu chính
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

            // Căn bảng điểm thẳng mép trái bàn cờ và nút PAUSE luôn nằm gọn bên phải không bị tràn màn hình
            if (pnlHeader != null)
            {
                if (pnlScoreGroup != null)
                    pnlScoreGroup.Location = new Point(Math.Max(20, boardX), (pnlHeader.Height - pnlScoreGroup.Height) / 2);

                if (btnPause != null)
                    btnPause.Location = new Point(Math.Min(pnlHeader.Width - btnPause.Width - 24, pnlBoard.Right + 140 - btnPause.Width), (pnlHeader.Height - btnPause.Height) / 2);
            }

            if (pnlMainMenu != null && pnlMainMenu.Visible)
            {
                pnlMainMenu.Location = new Point((this.ClientSize.Width - pnlMainMenu.Width) / 2, (this.ClientSize.Height - pnlMainMenu.Height) / 2);
            }
        }

        private void ApplyModernTheme()
        {
            BoardRenderer.ApplyModernTheme(
                new Button[] { btnSound, btnResume, btnRestart, btnExit, btnReplay, btnPause, btnBackToMenu, btnNewGameMenu, btnCloseAbout },
                new Panel[] { pnlDockPiece1, pnlDockPiece2, pnlDockPiece3 }
            );

            if (pnlScoreGroup != null)
            {
                pnlScoreGroup.Paint += BoardRenderer.DrawScoreGroupBackground;
            }
            if (btnPause != null)
            {
                btnPause.Text = ""; // Xóa text PAUSE
                btnPause.Size = new Size(42, 42);
                btnPause.Cursor = Cursors.Hand;
                btnPause.Paint -= btnPause_Paint;
                btnPause.Paint += btnPause_Paint;
            }
        }
        private void btnPause_Paint(object sender, PaintEventArgs e)
        {
            BoardRenderer.DrawGearIcon(e.Graphics, btnPause.Width, btnPause.Height, btnPause.BackColor);
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
            // Kiểm tra trạng thái từ bộ quản lý game (được load từ file JSON)
            // Nếu cờ IsNewGame = true và không ở trạng thái game over, bàn cờ sẽ được hiển thị trống

            // Vẽ bàn cờ chính và các khối gạch dựa trên dữ liệu hiện tại của gameManager.Board
            BoardRenderer.RenderBoard(e.Graphics, pnlBoard.Size, gameManager.Board, draggingShape, draggingColorIndex, hoverRow, hoverCol, flashingCells, flashScale);
            if (isPlayingItemVfx) RenderItemExplosionVfx(e.Graphics);
            // Vẽ hiệu ứng chữ bay (Combo text) nếu có
            if (floatingTextAlpha > 0f)
            {
                BoardRenderer.RenderFloatingText(e.Graphics, floatingComboText, floatingTextPos, floatingTextAlpha);
            }

            // Nếu cờ game over kích hoạt, lấy điểm số từ gameManager (đã đồng bộ với JSON) để hiển thị bảng thua
            if (isGameOver)
            {
                int current = gameManager.CurrentScore;
                int highScore = gameManager.HighScore; // Lấy điểm cao nhất đã lưu trong GameManager (từ JSON)
                bool isNewRecord = current > sessionGameInitialHighScore && current > 0;

                BoardRenderer.RenderGameOverOverlay(e.Graphics, pnlBoard.Size, current, highScore, isNewRecord);
            }
        }

        private void pnlDockPiece_Paint(object sender, PaintEventArgs e)
        {
            Panel pnl = sender as Panel;
            if (pnl == null || !pnl.Visible) return;

            // Nếu đang là ván mới hoàn toàn (IsNewGame = true) và game đã kết thúc (isGameOver = true) thì không vẽ
            // (Khi Resume, IsNewGame = false nên điều kiện này sẽ không chặn việc vẽ block cũ)
            if (gameManager.IsNewGame && isGameOver) return;

            // Nếu ô dock đang được người chơi kéo gạch ra ngoài, hiển thị ô trống
            if (pnl == activeDockPanel && draggingShape != null)
            {
                BoardRenderer.RenderEmptyDockSlot(e.Graphics, pnl.ClientRectangle);
                return;
            }

            // Đảm bảo chỉ vẽ khi Panel thực sự đang chứa BlockPiece trong Tag
            if (pnl.Tag is BlockPiece piece && piece != null)
            {
                // Vẽ hình dáng và màu sắc của khối gạch trong ô dock (đã được khôi phục từ JSON)
                BoardRenderer.RenderDockPiece(e.Graphics, pnl);
            }
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
            try
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

                // Khóa vị trí trong Pha 1 của Tutorial
                if (tutorialManager.IsTutorialActive && tutorialManager.CurrentPhase == 1)
                {
                    TutorialStep step = tutorialManager.GetCurrentLevel();
                    if (step != null && step.TargetRow != -1 && step.TargetCol != -1)
                    {
                        if (targetRow != step.TargetRow || targetCol != step.TargetCol)
                        {
                            ShowFloatingText("X_X Hãy đặt đúng vào khe trống hướng dẫn! :\"\"\"", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                            if (currentDock != null) currentDock.Invalidate();
                            pnlBoard.Invalidate();
                            return;
                        }
                    }
                }

                isProcessingMove = true;
                gameManager.Board.PlaceShape(shapeToPlace, colorToPlace, targetRow, targetCol);
                SoundManager.PlayDrop();

                if (currentDock != null) { currentDock.Tag = null; currentDock.Invalidate(); }
                pnlBoard.Refresh();

                if (!await AnimateLineClearAsync()) return;

                HandleGameProgressionAfterMove();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MouseUp Error]: {ex.Message}");
                CancelDrag();
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
                TutorialStep step = tutorialManager.GetCurrentLevel();

                // Nếu vừa hoàn thành Pha 1 (Đặt gạch nổ combo)
                if (step != null && tutorialManager.CurrentPhase == 1)
                {
                    // Cấp Item cho người chơi
                    if (step.RewardedItem.HasValue)
                    {
                        gameManager.ItemsInventory[step.RewardedItem.Value].Count = 1;
                        gameManager.SaveItemsJson();
                        UpdateItemUI();
                        SoundManager.PlayPickup();
                        ShowFloatingText($"★ ĐÃ MỞ KHÓA: {gameManager.ItemsInventory[step.RewardedItem.Value].Name}!",
                            new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                    }

                    // Nếu có bố cục mục tiêu thực hành Pha 2
                    if (step.TargetBoardLayout != null)
                    {
                        tutorialManager.CurrentPhase = 2; // Chuyển sang Pha 2
                        gameManager.Board.SetGrid(step.TargetBoardLayout);

                        // Làm trống khay gạch để người chơi tập trung dùng Item
                        pnlDockPiece1.Tag = null; pnlDockPiece1.Invalidate();
                        pnlDockPiece2.Tag = null; pnlDockPiece2.Invalidate();
                        pnlDockPiece3.Tag = null; pnlDockPiece3.Invalidate();

                        if (lblTutorialGuide != null)
                        {
                            lblTutorialGuide.Text = $"{step.Title}: {step.PracticeInstruction}";
                            lblTutorialGuide.Visible = true;
                        }
                        pnlBoard.Invalidate();
                        return; // Chưa chuyển màn vội, chờ người chơi bắn Item!
                    }
                }

                // Nếu hoàn thành Pha 2 hoặc màn không có Pha 2 -> Chuyển sang màn tiếp theo
                if (tutorialManager.NextLevel())
                {
                    LoadPuzzleLevel();
                }
                else
                {
                    if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;
                    tutorialManager.MarkTutorialCompleted();

                    tutorialManager.IsTutorialActive = false;
                    gameManager.ResetGame();
                    gameManager.ResetAllItems();
                    UpdateItemUI();
                    GenerateNewPieces();
                    pnlBoard.Invalidate();
                    ShowFloatingText("★ XUẤT SẮC! BẠN ĐÃ TỐT NGHIỆP TUTORIAL!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
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
            if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostOverlay(this);
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

            var itemConfig = new ItemType[]
            {
                ItemType.RerollDock,
                ItemType.Bomb3x3,
                ItemType.CrossClear,
                ItemType.ColorLightning,
                ItemType.GravityFall
            };

            int totalHeight = pnlMenuIteam.Height;
            int spacing = 8;
            int slotHeight = Math.Max(55, (totalHeight - (spacing * (itemConfig.Length + 1))) / itemConfig.Length);

            for (int i = 0; i < itemConfig.Length; i++)
            {
                ItemType type = itemConfig[i];
                Item item = gameManager.ItemsInventory[type];

                Panel slot = new Panel
                {
                    Size = new Size(pnlMenuIteam.Width - 16, slotHeight),
                    Location = new Point(8, spacing + i * (slotHeight + spacing)),
                    BackColor = Color.FromArgb(20, 23, 34),
                    Tag = item,
                    Cursor = Cursors.Default
                };

                // Nhãn Tên Item ở phía trên (chiều cao 18px để nhường chỗ cho icon ở giữa)
                Label lblName = new Label
                {
                    Text = item.Name,
                    Dock = DockStyle.Top,
                    Height = 18,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(120, 125, 145),
                    BackColor = Color.Transparent,
                    Tag = item
                };

                // Nhãn Số Lượng ở phía dưới
                Label lblBadge = new Label
                {
                    Text = $"x{item.Count}",
                    Dock = DockStyle.Bottom,
                    Height = 18,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(90, 95, 115),
                    BackColor = Color.Transparent,
                    Tag = item
                };

                slot.Controls.Add(lblName);
                slot.Controls.Add(lblBadge);

                // Gắn sự kiện tự vẽ Icon và Viền Neon
                slot.Paint += Slot_Paint;

                // Gắn sự kiện chuột cho cả slot và các nhãn con
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
                itemBadgeLabels[type] = lblBadge;
            }

            UpdateItemUI();
        }

        private void Slot_Paint(object sender, PaintEventArgs e)
        {
            Panel slot = sender as Panel;
            if (slot == null) return;
            Item item = slot.Tag as Item;
            if (item == null) return;

            BoardRenderer.DrawItemSlot(e.Graphics, slot.Width, slot.Height, item, draggingItem == item);
        }

        private void UpdateItemUI()
        {
            foreach (var kvp in itemBadgeLabels)
            {
                ItemType type = kvp.Key;
                Label lblBadge = kvp.Value;
                if (!gameManager.ItemsInventory.ContainsKey(type)) continue;

                Item item = gameManager.ItemsInventory[type];
                lblBadge.Text = (type == ItemType.GravityFall)
                    ? $"x{item.Count} ({gameManager.BoardShakeCounter}/3)"
                    : $"x{item.Count}";

                Panel slot = lblBadge.Parent as Panel;
                if (slot != null)
                {
                    Label lblName = (slot.Controls.Count > 0) ? slot.Controls[0] as Label : null;

                    if (item.Count <= 0)
                    {
                        // HẾT ĐỒ: Con trỏ thường, chữ xám mờ
                        slot.Cursor = Cursors.Default;
                        if (lblName != null) { lblName.Cursor = Cursors.Default; lblName.ForeColor = Color.FromArgb(100, 105, 125); }
                        lblBadge.Cursor = Cursors.Default;
                        lblBadge.ForeColor = Color.FromArgb(75, 80, 100);
                    }
                    else
                    {
                        // CÒN ĐỒ: Con trỏ Bàn Tay (Hand), chữ sáng nổi bật
                        slot.Cursor = Cursors.Hand;
                        if (lblName != null) { lblName.Cursor = Cursors.Hand; lblName.ForeColor = Color.White; }
                        lblBadge.Cursor = Cursors.Hand;
                        lblBadge.ForeColor = Color.FromArgb(255, 204, 77); // Vàng sáng
                    }

                    // Yêu cầu ô vẽ lại viền Neon và Icon tương ứng
                    slot.Invalidate();
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

            // Ăn cùng lúc từ 4 hàng/cột trở lên -> Mở khóa Huân chương "Siêu Combo Tứ Hàng" (Chỉ tính khi chơi thật)
            if (!tutorialManager.IsTutorialActive && linesCleared >= 4)
            {
                ScoreManager.UnlockMedal("COMBO_MEGA");
            }

            int scoreGained = GameBoard.CalculateLineScore(linesCleared);

            foreach (int r in fullRows) for (int c = 0; c < GameBoard.BOARD_SIZE; c++) flashingCells.Add(new Point(c, r));
            foreach (int c in fullCols) for (int r = 0; r < GameBoard.BOARD_SIZE; r++) flashingCells.Add(new Point(c, r));

            SoundManager.PlayLineClear(linesCleared);
            flashScale = 1f;
            pnlBoard.Refresh();
            if (!await DelayAsync(120)) return false;

            // Chỉ làm rung bàn cờ khi ăn Combo Chữ Thập (1 ngang + 1 dọc) hoặc >= 3 dòng (không tính 2 hàng đôi song song vì dễ)
            bool isCrossCombo = (fullRows.Count >= 1 && fullCols.Count >= 1);
            bool shouldShakeBoard = isCrossCombo || linesCleared >= 3;

            if (shouldShakeBoard && !await ShakeBoardAsync()) return false;

            for (int i = 1; i <= 5; i++)
            {
                flashScale = 1f - i / 5f;
                pnlBoard.Refresh();
                if (!await DelayAsync(18)) return false;
            }

            string comboText = BoardRenderer.BuildComboText(linesCleared, scoreGained);
            PointF textCenter = BoardRenderer.GetCellsCenter(pnlBoard.Size, flashingCells);

            gameManager.Board.ClearLines(fullRows, fullCols);
            flashingCells.Clear();
            flashScale = 1f;

            // Chỉ tính điểm chính thức và thưởng Item khi đang ở màn chơi thật (không phải Tutorial)
            if (!tutorialManager.IsTutorialActive)
            {
                gameManager.AddScore(scoreGained);

                if (linesCleared >= 2)
                {
                    Item rewarded = gameManager.AwardComboItem(fullRows.Count, fullCols.Count, out bool triggeredShake, out bool awardedGravity);
                    if (rewarded != null)
                    {
                        comboText += $"\n+1 {rewarded.Name}";
                    }

                    if (awardedGravity)
                    {
                        comboText += "\nĐỦ 3 RUNG CHẤN: +1 TRỌNG LỰC!";
                    }
                    else if (triggeredShake)
                    {
                        comboText += $"\n(Rung chấn: {gameManager.BoardShakeCounter}/3)";
                    }

                    UpdateItemUI();
                }

                UpdateScoreLabels();
            }

            ShowFloatingText(comboText, textCenter);
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

            // Reset kho đồ về 0 khi bắt đầu màn học (người chơi sẽ tự đặt gạch tạo nổ combo để mở khóa Item)
            gameManager.ResetAllItems();
            UpdateItemUI();

            gameManager.Board.SetGrid(level.BoardLayout);
            pnlDockPiece1.Tag = null; pnlDockPiece1.Visible = true; pnlDockPiece1.Invalidate();
            pnlDockPiece3.Tag = null; pnlDockPiece3.Visible = true; pnlDockPiece3.Invalidate();

            if (level.DockPieceShape != null)
                pnlDockPiece2.Tag = new BlockPiece(level.DockPieceShape, level.ColorIndex);
            else
                pnlDockPiece2.Tag = null;

            pnlDockPiece2.Visible = true;
            pnlDockPiece2.Invalidate();
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

            // Chỉ ghi nhận điểm và huân chương mốc điểm khi chơi thật
            if (!tutorialManager.IsTutorialActive && current > 0)
            {
                ScoreManager.CheckScoreMedals(current);
                ScoreManager.SaveMatchHistory(current);
            }

            try
            {
                string saveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlockBlast", "gamestate.dat");
                if (File.Exists(saveFilePath)) gameManager.DeleteSavedGame();
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
            if (pnlPause != null) pnlPause.Visible = false;
            if (pnlMainMenu != null) pnlMainMenu.Visible = false;

            // Bật lại đầy đủ các Panel game (chống đen màn hình)
            if (pnlBoard != null) pnlBoard.Visible = true;
            if (pnlDock != null) pnlDock.Visible = true;
            if (pnlHeader != null) pnlHeader.Visible = true;
            if (pnlMenuIteam != null) pnlMenuIteam.Visible = true;
            if (tutorialManager.IsTutorialActive && lblTutorialGuide != null) lblTutorialGuide.Visible = true;

            pnlBoard.Invalidate();
            UpdateScoreLabels();
            UpdateItemUI();
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
            gameManager.DeleteSavedGame();
            gameManager.IsPaused = false;
            sessionGameInitialHighScore = gameManager.HighScore;

            floatingTextTimer.Stop();
            floatingTextAlpha = 0f;
            floatingComboText = "";

            UpdateScoreLabels();

            // Quy tắc: Nếu BestScore == 0 -> kích hoạt Tutorial; nếu > 0 -> không kích hoạt
            if (gameManager.HighScore == 0)
            {
                tutorialManager.ResetTutorial();
                LoadPuzzleLevel();
            }
            else
            {
                tutorialManager.IsTutorialActive = false;
                if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;
                GenerateNewPieces();
            }

            pnlBoard.Invalidate();
        }

        // Nút "Thoát ứng dụng"
        private void btnExit_Click(object sender, EventArgs e)
        {
            CancelDrag();
            if (MessageBox.Show("Bạn có chắc chắn muốn thoát game không?", "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Cập nhật trạng thái game trước khi thoát
                gameManager.IsNewGame = false;

                // Nếu bạn có lưu game state tự động, có thể gọi thêm hàm save ở đây nếu cần
                // gameManager.SaveGameSession();

                Application.Exit();
            }
        }

        private void fGamePlay_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                floatingTextTimer.Stop();
                floatingTextTimer.Dispose();
                slingshotTimer.Stop();
                slingshotTimer.Dispose();
                if (dragGhost != null && !dragGhost.IsDisposed) dragGhost.Dispose();

                if (!isGameOver && isGameInProgress)
                {
                    gameManager.IsNewGame = false;
                    gameManager.SaveGameState(pnlDockPiece1, pnlDockPiece2, pnlDockPiece3, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FormClosed Error]: {ex.Message}");
            }
        }


        private void ItemSlot_MouseDown(object sender, MouseEventArgs e)
        {
            if (isGameOver || isProcessingMove || gameManager.IsPaused) return;
            if (e.Button != MouseButtons.Left) return;

            Control ctrl = sender as Control;
            Item item = ctrl?.Tag as Item;
            if (item == null || item.Count <= 0) return;

            // 1. ĐỔI GẠCH: Chỉ sinh đúng 1 gạch mới vào 1 khay Dock đang trống
            if (item.Type == ItemType.RerollDock)
            {
                Panel emptyDock = null;
                if (pnlDockPiece1.Tag == null) emptyDock = pnlDockPiece1;
                else if (pnlDockPiece2.Tag == null) emptyDock = pnlDockPiece2;
                else if (pnlDockPiece3.Tag == null) emptyDock = pnlDockPiece3;

                // Khi cả 3 khay trên Dock đều đầy -> Không trừ Item và báo cho người chơi
                if (emptyDock == null)
                {
                    SoundManager.PlayDrop();
                    ShowFloatingText("CHỈ ĐỔI GẠCH MỚI\nTRONG KHAY TRỐNG!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                    pnlBoard.Invalidate();
                    return;
                }

                item.TryConsume();
                gameManager.SaveItemsJson();

                // Nếu đang ở Tutorial thì sinh đúng viên 1x2 để người chơi lấp khe trống hàng 4
                BlockPiece newPiece = tutorialManager.IsTutorialActive
                    ? new BlockPiece(new int[,] { { 1, 1 } }, 3)
                    : gameManager.CreatePieceForEmptySlot();

                SetupDockPiece(emptyDock, newPiece);

                SoundManager.PlayPickup();
                ShowFloatingText("+1 GẠCH MỚI\nVÀO KHAY TRỐNG!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));

                if (tutorialManager.IsTutorialActive && lblTutorialGuide != null)
                {
                    lblTutorialGuide.Text = "Màn 3/4: Tuyệt vời! Kéo viên gạch 1x2 vừa tạo vào khe trống để làm nổ hàng!";
                }

                UpdateItemUI();
                pnlBoard.Invalidate();
                if (!tutorialManager.IsTutorialActive) CheckGameOver();
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

            int pullDx = slingPullPos.X - slingAnchor.X;
            int pullDy = slingPullPos.Y - slingAnchor.Y;
            double dist = Math.Sqrt(pullDx * pullDx + pullDy * pullDy);

            // 1. Khi kéo căng trên 65%: Tay cầm bắt đầu mỏi và rung nhẹ làm lệch góc ngắm thực tế nếu giữ lâu
            if (dist >= MAX_PULL_DISTANCE * 0.65)
            {
                int tremor = isTensionMax ? 3 : 2;
                slingPullPos.X += fxRandom.Next(-tremor, tremor + 1);
                slingPullPos.Y += fxRandom.Next(-tremor, tremor + 1);
            }

            // 2. Giảm thời gian giữ độ căng tối đa xuống 1.0 giây -> Đứt dây thun!
            if (isTensionMax && tensionMaxStartTime != DateTime.MinValue)
            {
                double elapsedSec = (DateTime.Now - tensionMaxStartTime).TotalSeconds;
                if (elapsedSec >= 1.0)
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

            // 3. Nếu đang trong Tutorial -> Hoàn lại Item để người chơi thử lại, không phạt mất đồ
            if (tutorialManager.IsTutorialActive)
            {
                if (heldItem != null) heldItem.Count = 1;
                UpdateItemUI();
                ShowFloatingText("ĐỨT DÂY THUN!\nĐừng giữ căng quá lâu, thử lại nhé!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                if (pnlMenuIteam != null) await ShakePanelAsync(pnlMenuIteam);

                isSnapping = false;
                isTensionMax = false;
                tensionMaxStartTime = DateTime.MinValue;
                return;
            }

            // Phạt mất Item khi chơi thật:
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
                    ? $"ĐỨT DÂY THUN!\nRƠI {heldItem?.Name} & {droppedExtra.Name}!"
                    : $"ĐỨT DÂY THUN!\nMẤT {heldItem?.Name}!";
                ShowFloatingText(dropText, new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
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

            try
            {
                Item item = draggingItem;
                draggingItem = null;

                int pullDx = slingPullPos.X - slingAnchor.X;
                int pullDy = slingPullPos.Y - slingAnchor.Y;
                double pullDist = Math.Sqrt(pullDx * pullDx + pullDy * pullDy);

                if (pullDist < 20)
                {
                    HideDragGhost();
                    UpdateItemUI();
                    pnlBoard.Invalidate();
                    return;
                }

                const double POWER_MULTIPLIER = 4.2;
                int landScreenX = (int)(slingAnchor.X - pullDx * POWER_MULTIPLIER);
                int landScreenY = (int)(slingAnchor.Y - pullDy * POWER_MULTIPLIER);
                Point targetScreen = new Point(landScreenX, landScreenY);

                Point boardPos = pnlBoard.PointToClient(targetScreen);
                bool isHitBoard = pnlBoard.ClientRectangle.Contains(boardPos);

                item.TryConsume();
                gameManager.SaveItemsJson();
                UpdateItemUI();

                await FlyProjectileAsync(item, slingPullPos, targetScreen, arcHeight: 70);

                int clearedCells = 0;
                bool hitValidBoardCell = false;

                if (isHitBoard)
                {
                    int cellSize = BoardRenderer.CalculateCellSize(pnlBoard.Size);
                    int targetCol = (boardPos.X - BoardRenderer.MARGIN) / (cellSize + BoardRenderer.GAP);
                    int targetRow = (boardPos.Y - BoardRenderer.MARGIN) / (cellSize + BoardRenderer.GAP);

                    if (targetRow >= 0 && targetRow < 8 && targetCol >= 0 && targetCol < 8)
                    {
                        hitValidBoardCell = true;
                        flashingCells.Clear();

                        switch (item.Type)
                        {
                            case ItemType.Bomb3x3:
                                for (int r = targetRow - 1; r <= targetRow + 1; r++)
                                    for (int c = targetCol - 1; c <= targetCol + 1; c++)
                                        if (r >= 0 && r < 8 && c >= 0 && c < 8 && gameManager.Board.GetCell(r, c) != 0)
                                            flashingCells.Add(new Point(c, r));

                                clearedCells = gameManager.Board.ClearArea3x3(targetRow, targetCol);
                                ShowFloatingText("TRÚNG ĐÍCH!", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
                                break;

                            case ItemType.CrossClear:
                                for (int i = 0; i < 8; i++)
                                {
                                    if (gameManager.Board.GetCell(targetRow, i) != 0) flashingCells.Add(new Point(i, targetRow));
                                    if (gameManager.Board.GetCell(i, targetCol) != 0) flashingCells.Add(new Point(targetCol, i));
                                }
                                clearedCells = gameManager.Board.ClearCross(targetRow, targetCol);
                                ShowFloatingText("TRÚNG ĐÍCH!", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
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
                                    ShowFloatingText("SẤM SÉT TRÚNG MÀU!", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
                                }
                                else
                                {
                                    ShowFloatingText("SÉT ĐÁNH VÀO Ô TRỐNG!", BoardRenderer.GetCellRect(targetRow, targetCol, cellSize).Location);
                                }
                                break;

                            case ItemType.GravityFall:
                                gameManager.Board.ApplyGravity();
                                ShowFloatingText("KÍCH HOẠT TRỌNG LỰC!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                                break;
                        }

                        if (!tutorialManager.IsTutorialActive && clearedCells > 0)
                        {
                            int scoreBonus = clearedCells * 5;
                            gameManager.AddScore(scoreBonus);
                            UpdateScoreLabels();
                        }

                        if (item.Type == ItemType.Bomb3x3) SoundManager.PlayBombExplosion();
                        else if (item.Type == ItemType.CrossClear) SoundManager.PlayCrossLaser();
                        else if (item.Type == ItemType.ColorLightning) SoundManager.PlayLightningStrike();
                        else if (item.Type == ItemType.GravityFall) SoundManager.PlayGravitySlam();

                        await PlayItemExplosionVfxAsync(item.Type, targetRow, targetCol, cellSize);
                        flashingCells.Clear();

                        // Chỉ mở khóa Huân chương Vũ khí khi CHƠI THẬT (Bỏ qua Tutorial)
                        if (!tutorialManager.IsTutorialActive)
                        {
                            switch (item.Type)
                            {
                                case ItemType.Bomb3x3: ScoreManager.UnlockMedal("WEAPON_BOMB"); break;
                                case ItemType.CrossClear: ScoreManager.UnlockMedal("WEAPON_CROSS"); break;
                                case ItemType.ColorLightning: ScoreManager.UnlockMedal("WEAPON_LIGHTNING"); break;
                                case ItemType.GravityFall: ScoreManager.UnlockMedal("WEAPON_GRAVITY"); break;
                            }
                            ScoreManager.CheckWeaponMaster();
                        }

                        if (item.Type == ItemType.GravityFall)
                        {
                            await AnimateLineClearAsync();
                        }

                        pnlBoard.Invalidate();
                    }
                }
                else
                {
                    ShowFloatingText("BẮN HỤT RA NGOÀI!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                    SoundManager.PlayDrop();
                }

                if (tutorialManager.IsTutorialActive)
                {
                    bool tutorialSuccess = hitValidBoardCell && (clearedCells > 0 || item.Type == ItemType.GravityFall);
                    if (tutorialSuccess)
                    {
                        ShowFloatingText("★ CHÍNH XÁC! MỤC TIÊU ĐÃ BỊ PHÁ HỦY!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));

                        if (tutorialManager.NextLevel())
                        {
                            LoadPuzzleLevel();
                        }
                        else
                        {
                            if (lblTutorialGuide != null) lblTutorialGuide.Visible = false;
                            tutorialManager.MarkTutorialCompleted();
                            tutorialManager.IsTutorialActive = false;
                            gameManager.ResetGame();
                            gameManager.ResetAllItems();
                            UpdateItemUI();
                            GenerateNewPieces();
                            pnlBoard.Invalidate();
                            ShowFloatingText("★ XUẤT SẮC! BẠN ĐÃ HOÀN THÀNH TUTORIAL!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                        }
                    }
                    else
                    {
                        item.Count = 1;
                        gameManager.SaveItemsJson();
                        UpdateItemUI();
                        ShowFloatingText("CHƯA TRÚNG MỤC TIÊU!\nĐã hoàn lại Item, hãy kéo ná ngắm lại nhé!", new PointF(pnlBoard.Width / 2f, pnlBoard.Height / 2f));
                    }
                    return;
                }
                else
                {
                    CheckGameOver();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleItemDrop Error]: {ex.Message}");
                HideDragGhost();
            }
            finally
            {
                isProcessingMove = false;
                pnlBoard.Invalidate();
                UpdateItemUI();
            }
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

                // 3. Chỉ vẽ 5 chấm đầu tiên (~35% quỹ đạo bay, mờ dần) để ẩn điểm rơi thật -> Đòi hỏi kỹ năng căn lực!
                int pullDx = slingPullPos.X - slingAnchor.X;
                int pullDy = slingPullPos.Y - slingAnchor.Y;
                double dist = Math.Sqrt(pullDx * pullDx + pullDy * pullDy);

                if (dist > 15)
                {
                    const double POWER_MULTIPLIER = 4.2;
                    Point formTarget = new Point((int)(formAnchor.X - pullDx * POWER_MULTIPLIER),
                                                 (int)(formAnchor.Y - pullDy * POWER_MULTIPLIER));

                    int dotCount = 5; // Chỉ vẽ 5 hạt hướng dẫn đoạn đầu
                    for (int i = 1; i <= dotCount; i++)
                    {
                        float t = (i / (float)dotCount) * 0.35f; // Chỉ kéo dài tới 35% quãng đường bay
                        float dotX = formPull.X + (formTarget.X - formPull.X) * t;
                        float dotY = formPull.Y + (formTarget.Y - formPull.Y) * t - (float)(60 * t * (1.0 - t));
                        float dotRadius = Math.Max(2f, 4.8f - i * 0.55f);
                        int fadeAlpha = (int)(240 * (1f - (i - 1) / (float)dotCount));

                        Color dotColor = isTensionMax
                            ? (flashRed ? Color.FromArgb(fadeAlpha, 255, 50, 50) : Color.FromArgb(fadeAlpha, 255, 180, 0))
                            : Color.FromArgb(fadeAlpha, 255, 215, 0);

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

                // Vẽ biểu tượng quả đạn vector GDI+ sắc nét không phụ thuộc emoji hệ điều hành
                BoardRenderer.DrawItemProjectileIcon(g, draggingItem.Type, formPull.X, formPull.Y, 15f);

                // 5. Cảnh báo độ căng tối đa
                if (isTensionMax && tensionMaxStartTime != DateTime.MinValue)
                {
                    double remain = Math.Max(0, 1.0 - (DateTime.Now - tensionMaxStartTime).TotalSeconds);
                    string warnText = $"QUÁ CĂNG! {remain:F1}s";
                    using (Font warnFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                    using (SolidBrush warnBrush = new SolidBrush(Color.FromArgb(255, 60, 60)))
                    {
                        StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(warnText, warnFont, warnBrush, formPull.X, formPull.Y - 45, sf);
                    }
                }

                if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostOverlay(this);
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
                    g.DrawString("ĐỨT!", f, b, mid.X, mid.Y - 15);
                }

                if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostOverlay(this);
                dragGhost.SetBitmap(bmp, formTopLeft);
            }
            if (!dragGhost.Visible) dragGhost.Show();
        }

        // Hoạt họa quả bom thực sự bay theo đường cong Parabol từ ná đến điểm rơi trên bàn cờ
        private async Task FlyProjectileAsync(Item item, Point startScreen, Point targetScreen, int arcHeight = 70)
        {
            if (dragGhost == null || dragGhost.IsDisposed) dragGhost = new DragGhostOverlay(this);

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

                    BoardRenderer.DrawItemProjectileIcon(g, item.Type, cx, cy, 14f);
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

        private async Task PlayItemExplosionVfxAsync(ItemType type, int targetRow, int targetCol, int cellSize)
        {
            Rectangle centerRect = BoardRenderer.GetCellRect(targetRow, targetCol, cellSize);
            vfxCenter = new PointF(centerRect.X + centerRect.Width / 2f, centerRect.Y + centerRect.Height / 2f);
            activeVfxType = type;
            vfxParticles.Clear();
            vfxLightningTargets.Clear();

            // Lưu tâm các ô bị trúng đòn để vẽ tia sét / mảnh vỡ
            foreach (Point pt in flashingCells)
            {
                Rectangle r = BoardRenderer.GetCellRect(pt.Y, pt.X, cellSize);
                vfxLightningTargets.Add(new PointF(r.X + r.Width / 2f, r.Y + r.Height / 2f));
            }

            // Sinh 26 hạt mảnh vỡ / tia lửa văng 360 độ
            Color[] sparkColors = type == ItemType.Bomb3x3
                ? new[] { Color.FromArgb(255, 90, 30), Color.FromArgb(255, 190, 40), Color.FromArgb(255, 240, 150) }
                : type == ItemType.CrossClear
                ? new[] { Color.FromArgb(255, 230, 80), Color.White, Color.FromArgb(255, 160, 30) }
                : type == ItemType.ColorLightning
                ? new[] { Color.FromArgb(190, 120, 255), Color.FromArgb(100, 220, 255), Color.White }
                : new[] { Color.FromArgb(46, 204, 113), Color.FromArgb(120, 255, 180), Color.White };

            for (int i = 0; i < 26; i++)
            {
                double angle = fxRandom.NextDouble() * Math.PI * 2;
                double speed = 3.5 + fxRandom.NextDouble() * 9.5;
                vfxParticles.Add(new VfxParticle
                {
                    X = vfxCenter.X,
                    Y = vfxCenter.Y,
                    Vx = (float)(Math.Cos(angle) * speed),
                    Vy = (float)(Math.Sin(angle) * speed),
                    Size = fxRandom.Next(5, 12),
                    Color = sparkColors[fxRandom.Next(sparkColors.Length)]
                });
            }

            isPlayingItemVfx = true;
            const int frames = 14;
            for (int f = 1; f <= frames; f++)
            {
                vfxProgress = (float)f / frames;
                flashScale = 1f - vfxProgress * 0.7f;

                for (int i = 0; i < vfxParticles.Count; i++)
                {
                    var p = vfxParticles[i];
                    p.X += p.Vx;
                    p.Y += p.Vy;
                    p.Vy += 0.45f; // Trọng lực kéo hạt rơi xuống nhẹ
                    p.Size = Math.Max(1f, p.Size * 0.92f);
                    vfxParticles[i] = p;
                }

                // Rung bàn cờ đồng bộ ngay trong lúc nổ
                if (f % 2 == 0)
                {
                    pnlBoard.Location = new Point(
                        (this.ClientSize.Width - pnlBoard.Width) / 2 + fxRandom.Next(-6, 7),
                        pnlHeader.Height + (this.ClientSize.Height - pnlHeader.Height - pnlDock.Height - pnlBoard.Height) / 2 + fxRandom.Next(-5, 6)
                    );
                }

                pnlBoard.Refresh();
                await Task.Delay(18);
            }

            isPlayingItemVfx = false;
            flashScale = 1f;
            LayoutGameControls(); // Trả pnlBoard về đúng tâm chuẩn
        }

        private void RenderItemExplosionVfx(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int alpha = (int)(255 * (1f - vfxProgress));
            if (alpha <= 0) return;

            if (activeVfxType == ItemType.Bomb3x3)
            {
                // 1. Quả cầu lửa bung nở + Vòng sóng xung kích (Shockwave Ring)
                float fireRadius = 20f + vfxProgress * 95f;
                float ringRadius = 15f + vfxProgress * 145f;

                using (SolidBrush fireBrush = new SolidBrush(Color.FromArgb(alpha * 3 / 4, 255, 110, 20)))
                {
                    g.FillEllipse(fireBrush, vfxCenter.X - fireRadius, vfxCenter.Y - fireRadius, fireRadius * 2, fireRadius * 2);
                }
                using (Pen shockPen = new Pen(Color.FromArgb(alpha, 255, 230, 130), Math.Max(2f, 8f * (1f - vfxProgress))))
                {
                    g.DrawEllipse(shockPen, vfxCenter.X - ringRadius, vfxCenter.Y - ringRadius, ringRadius * 2, ringRadius * 2);
                }
            }
            else if (activeVfxType == ItemType.CrossClear)
            {
                // 2. Hai chùm tia Laser Chữ Thập quét ngang và dọc toàn bàn cờ
                float beamThick = Math.Max(4f, 34f * (1f - vfxProgress * 0.7f));
                using (SolidBrush outerBeam = new SolidBrush(Color.FromArgb(alpha * 2 / 3, 255, 200, 40)))
                using (SolidBrush coreBeam = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                {
                    g.FillRectangle(outerBeam, 0, vfxCenter.Y - beamThick / 2, pnlBoard.Width, beamThick);
                    g.FillRectangle(outerBeam, vfxCenter.X - beamThick / 2, 0, beamThick, pnlBoard.Height);

                    float coreThick = beamThick * 0.4f;
                    g.FillRectangle(coreBeam, 0, vfxCenter.Y - coreThick / 2, pnlBoard.Width, coreThick);
                    g.FillRectangle(coreBeam, vfxCenter.X - coreThick / 2, 0, coreThick, pnlBoard.Height);
                }
            }
            else if (activeVfxType == ItemType.ColorLightning)
            {
                // 3. Chớp sáng màn hình (Screen Flash) + Tia sét ziczac đánh xuống từng ô cùng màu
                if (vfxProgress < 0.35f)
                {
                    using (SolidBrush flash = new SolidBrush(Color.FromArgb(110, 200, 230, 255)))
                    {
                        g.FillRectangle(flash, 0, 0, pnlBoard.Width, pnlBoard.Height);
                    }
                }

                using (Pen glowPen = new Pen(Color.FromArgb(alpha, 170, 100, 255), 6f))
                using (Pen boltPen = new Pen(Color.FromArgb(alpha, 255, 255, 255), 2.5f))
                {
                    var targets = vfxLightningTargets.Count > 0 ? vfxLightningTargets : new List<PointF> { vfxCenter };
                    foreach (PointF dest in targets)
                    {
                        PointF prev = new PointF(dest.X + fxRandom.Next(-30, 31), 0);
                        int segments = 5;
                        for (int s = 1; s <= segments; s++)
                        {
                            float ratio = (float)s / segments;
                            PointF next = s == segments
                                ? dest
                                : new PointF(dest.X + fxRandom.Next(-18, 19), dest.Y * ratio);
                            g.DrawLine(glowPen, prev, next);
                            g.DrawLine(boltPen, prev, next);
                            prev = next;
                        }
                    }
                }
            }
            else if (activeVfxType == ItemType.GravityFall)
            {
                // 4. Sóng ép trọng lực xanh ngọc trút từ đỉnh xuống đáy bàn cờ
                float waveY = vfxProgress * pnlBoard.Height;
                using (Pen wavePen = new Pen(Color.FromArgb(alpha, 46, 204, 113), 6f))
                {
                    g.DrawLine(wavePen, 10, waveY, pnlBoard.Width - 10, waveY);
                }
            }

            // Vẽ các hạt tia lửa / mảnh vỡ văng tung tóe
            foreach (var p in vfxParticles)
            {
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(alpha, p.Color)))
                {
                    g.FillEllipse(sb, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
                }
            }
        }

        private void btnNewGameMenu_Click(object sender, EventArgs e)
        {
            if (pnlMainMenu != null) pnlMainMenu.Visible = false;
            if (pnlBoard != null) pnlBoard.Visible = true;
            if (pnlDock != null) pnlDock.Visible = true;
            if (pnlHeader != null) pnlHeader.Visible = true;
            if (pnlMenuIteam != null) pnlMenuIteam.Visible = true;

            btnRestart_Click(sender, e);
        }

        private void btnResetHistory_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn RESET TOÀN BỘ lịch sử chơi không?\n\n",
                "Xác nhận Reset Lịch Sử",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            CancelDrag();
            floatingTextTimer.Stop();

            ScoreManager.ResetAllHistory();
            gameManager.ResetGame();
            gameManager.DeleteSavedGame();
            gameManager.ResetAllItems();
            gameManager.HighScore = 0;
            sessionGameInitialHighScore = 0;
            tutorialManager.ResetTutorial();

            isGameOver = false;
            isGameInProgress = false;
            gameManager.IsPaused = false;
            pnlPause.Visible = false;

            UpdateScoreLabels();
            UpdateItemUI();

            ShowMainMenu();
            MessageBox.Show("Đã xóa toàn bộ lịch sử chơi! Nhấn CHƠI NGAY để bắt đầu lại từ màn Hướng Dẫn (Tutorial).", "Hoàn tất Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnBackToMenu_Click(object sender, EventArgs e)
        {
            CancelDrag();

            if (pnlPause != null) pnlPause.Visible = false;
            if (isGameInProgress && !isGameOver)
            {
                gameManager.IsPaused = true;
                // Lưu ván chơi ngay khi thoát ra Menu chính
                gameManager.SaveGameState(pnlDockPiece1, pnlDockPiece2, pnlDockPiece3, false);
            }

            floatingTextTimer.Stop();
            ShowMainMenu();
        }

        private void btnSound_Click(object sender, EventArgs e)
        {
            SoundManager.IsMuted = !SoundManager.IsMuted;
            ScoreManager.SaveAudioSettings(SoundManager.IsMuted);
            btnSound.Text = SoundManager.IsMuted ? "LOA: TẮT" : "LOA: BẬT";
        }

        // Kích hoạt cờ WS_EX_COMPOSITED (0x02000000) giúp Windows vẽ mượt mà toàn bộ các lớp control từ dưới lên trên
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
        // ================================================================
        //  KHỞI TẠO POPUP THÔNG TIN NHÓM (CHUYỂN TỪ FORMCREDITS VÀO 1 FORM)
        // ================================================================
        private void InitAboutDialog()
        {
            if (pnlAbout == null) return;

            pnlAbout.Controls.Clear();
            pnlAbout.Parent = this;
            pnlAbout.Visible = false;
            pnlAbout.BackColor = Color.FromArgb(22, 26, 42);
            pnlAbout.Size = new Size(680, 540);

            // 1. Tiêu đề chính và phụ
            Label lblTitle = new Label
            {
                Text = "BLOCK BLAST",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 159, 67),
                Location = new Point(24, 16),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Giới thiệu dự án & nhóm phát triển (Nhóm: " + CreditsContent.TeamName + ")",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(174, 184, 207),
                Location = new Point(26, 52),
                AutoSize = true
            };

            // 2. TabControl với 4 Tab thông tin (chia đều 100% chiều ngang)
            int tabAreaWidth = pnlAbout.Width - 48;
            int tabItemWidth = (tabAreaWidth - 4) / 4; // Chia đều cho 4 Tab không để khe trắng

            TabControl tabCredits = new TabControl
            {
                Location = new Point(24, 82),
                Size = new Size(tabAreaWidth, pnlAbout.Height - 146),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(tabItemWidth, 34)
            };

            // Thứ tự hiển thị: 1. Luật chơi -> 2. Sáng tạo -> 3. Thành viên -> 4. Động lực
            TabPage tabGuide = new TabPage("Luật chơi") { BackColor = Color.FromArgb(29, 34, 52) };
            TabPage tabInnovation = new TabPage("Sáng tạo") { BackColor = Color.FromArgb(29, 34, 52) };
            TabPage tabTeam = new TabPage("Thành viên") { BackColor = Color.FromArgb(29, 34, 52) };
            TabPage tabMotivation = new TabPage("Động lực") { BackColor = Color.FromArgb(29, 34, 52) };

            // Nội dung Tab 1: Luật chơi (có đệm viền lùi vào trong)
            tabGuide.Controls.Add(CreateCreditsContentPanel(CreditsNarrative.Guide));

            // Nội dung Tab 2: Sáng tạo (có đệm viền lùi vào trong)
            tabInnovation.Controls.Add(CreateCreditsContentPanel(CreditsNarrative.Innovation));

            // Nội dung Tab 3: Bảng Thành viên nhóm (căn chỉnh chuẩn, khóa kéo lệch ô)
            Panel pnlTeamWrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 14, 16, 14),
                BackColor = Color.FromArgb(29, 34, 52)
            };

            DataGridView dgvMembers = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false, // KHÓA KÉO LỆCH CỘT
                AllowUserToResizeRows = false,    // KHÓA KÉO LỆCH DÒNG
                RowHeadersVisible = false,
                BackgroundColor = Color.FromArgb(29, 34, 52),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(60, 69, 93),
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 42,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvMembers.RowTemplate.Height = 44;

            dgvMembers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(42, 48, 70);
            dgvMembers.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(255, 180, 80);
            dgvMembers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvMembers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvMembers.DefaultCellStyle.BackColor = Color.FromArgb(29, 34, 52);
            dgvMembers.DefaultCellStyle.ForeColor = Color.FromArgb(232, 236, 244);
            dgvMembers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 62, 88);
            dgvMembers.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvMembers.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);

            // Cột 1: Họ tên (40% - căn trái)
            var colName = new DataGridViewTextBoxColumn
            {
                HeaderText = "Họ tên",
                DataPropertyName = "FullName",
                FillWeight = 40F
            };
            colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colName.DefaultCellStyle.Padding = new Padding(14, 0, 0, 0);

            // Cột 2: MSSV (30% - căn giữa)
            var colId = new DataGridViewTextBoxColumn
            {
                HeaderText = "MSSV",
                DataPropertyName = "StudentId",
                FillWeight = 30F
            };
            colId.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Cột 3: Phụ trách (30% - căn giữa)
            var colRole = new DataGridViewTextBoxColumn
            {
                HeaderText = "Phụ trách",
                DataPropertyName = "Role",
                FillWeight = 30F
            };
            colRole.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvMembers.Columns.AddRange(new DataGridViewColumn[] { colName, colId, colRole });
            dgvMembers.DataSource = CreditsContent.Members;
            pnlTeamWrap.Controls.Add(dgvMembers);
            tabTeam.Controls.Add(pnlTeamWrap);

            // Nội dung Tab 4: Động lực (có đệm viền lùi vào trong)
            tabMotivation.Controls.Add(CreateCreditsContentPanel(CreditsNarrative.Motivation));

            // Thêm các Tab vào TabControl theo thứ tự mới
            tabCredits.TabPages.Add(tabGuide);
            tabCredits.TabPages.Add(tabInnovation);
            tabCredits.TabPages.Add(tabTeam);
            tabCredits.TabPages.Add(tabMotivation);

            // Tự vẽ Tab cho đồng bộ Dark Neon Theme
            tabCredits.DrawItem += (s, ev) =>
            {
                if (ev.Index < 0) return;
                bool selected = ev.Index == tabCredits.SelectedIndex;
                Color bg = selected ? Color.FromArgb(255, 159, 67) : Color.FromArgb(42, 48, 70);
                Color fg = selected ? Color.FromArgb(22, 26, 42) : Color.FromArgb(232, 236, 244);
                using (SolidBrush brush = new SolidBrush(bg)) ev.Graphics.FillRectangle(brush, ev.Bounds);
                TextRenderer.DrawText(ev.Graphics, tabCredits.TabPages[ev.Index].Text, tabCredits.Font, ev.Bounds, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // 3. Nút Đóng
            Button btnClose = new Button
            {
                Text = "ĐÓNG",
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(pnlAbout.Width - 134, pnlAbout.Height - 48),
                BackColor = Color.FromArgb(255, 159, 67),
                ForeColor = Color.FromArgb(22, 26, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += btnCloseAbout_Click;
            BoardRenderer.ApplyRoundedRegion(btnClose, 10);

            // Vẽ viền bo góc phát sáng cho pnlAbout
            pnlAbout.Paint += (s, ev) =>
            {
                using (var path = BoardRenderer.CreateRoundedPath(new Rectangle(1, 1, pnlAbout.Width - 3, pnlAbout.Height - 3), 16))
                using (Pen borderPen = new Pen(Color.FromArgb(140, 255, 159, 67), 2f))
                {
                    ev.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    ev.Graphics.DrawPath(borderPen, path);
                }
            };

            pnlAbout.Controls.Add(lblTitle);
            pnlAbout.Controls.Add(lblSubtitle);
            pnlAbout.Controls.Add(tabCredits);
            pnlAbout.Controls.Add(btnClose);
            BoardRenderer.ApplyRoundedRegion(pnlAbout, 16);
        }

        // Tạo khung chứa RichTextBox có khoảng đệm Padding không bị sát viền
        private Control CreateCreditsContentPanel(CreditsSection[] sections)
        {
            Panel pnlWrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 14),
                BackColor = Color.FromArgb(29, 34, 52)
            };

            RichTextBox box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(29, 34, 52),
                ForeColor = Color.FromArgb(232, 236, 244),
                Font = new Font("Segoe UI", 9.5F),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            FillCreditsSections(box, sections);
            pnlWrap.Controls.Add(box);
            return pnlWrap;
        }

        private static void FillCreditsSections(RichTextBox box, CreditsSection[] sections)
        {
            box.Clear();
            using (Font headingFont = new Font(box.Font.FontFamily, 12F, FontStyle.Bold))
            {
                foreach (CreditsSection section in sections)
                {
                    box.SelectionStart = box.TextLength;
                    box.SelectionLength = 0;
                    box.SelectionFont = headingFont;
                    box.SelectionColor = Color.FromArgb(255, 159, 67);
                    box.AppendText(section.Title + "\r\n");

                    box.SelectionFont = box.Font;
                    box.SelectionColor = Color.FromArgb(232, 236, 244);
                    box.AppendText(section.Body + "\r\n\r\n");
                }
            }
            box.SelectionStart = 0;
            box.ScrollToCaret();
        }

        private RichTextBox CreateCreditsTextBox()
        {
            return new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(29, 34, 52),
                ForeColor = Color.FromArgb(232, 236, 244),
                Font = new Font("Segoe UI", 9.5F)
            };
        }
        // ================================================================
        //  KHỞI TẠO POPUP NHẬT KÝ CHIẾN TÍCH & BỘ SƯU TẬP HUÂN CHƯƠNG
        // ================================================================

        private void InitLeaderboardButtons()
        {
            if (pnlMainMenu != null && btnMenuLeaderboard == null)
            {
                btnMenuLeaderboard = new Button
                {
                    Text = "CHIẾN TÍCH",
                    Size = new Size(280, 40),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(40, 45, 66),
                    Cursor = Cursors.Hand
                };
                btnMenuLeaderboard.FlatAppearance.BorderSize = 0;
                btnMenuLeaderboard.Click += (s, ev) => ShowLeaderboardDialog();
                pnlMainMenu.Controls.Add(btnMenuLeaderboard);
            }
        }

        private void ShowLeaderboardDialog()
        {
            if (pnlLeaderboard == null) InitLeaderboardDialog();
            // Ghi nhớ: Dialog này được mở từ Menu Tạm dừng (Pause) hay từ Menu chính
            isDialogOpenedFromPause = (pnlPause != null && pnlPause.Visible);
            if (pnlMainMenu != null) pnlMainMenu.Visible = false;
            if (pnlPause != null) pnlPause.Visible = false;
            pnlLeaderboard.Parent = this;
            int dialogW = Math.Min(920, Math.Max(600, this.ClientSize.Width - 40));
            int dialogH = Math.Min(620, Math.Max(500, this.ClientSize.Height - 40));
            pnlLeaderboard.Size = new Size(dialogW, dialogH);
            pnlLeaderboard.Location = new Point(
                (this.ClientSize.Width - pnlLeaderboard.Width) / 2,
                (this.ClientSize.Height - pnlLeaderboard.Height) / 2
            );
            RefreshLeaderboardData();
            BoardRenderer.ApplyRoundedRegion(pnlLeaderboard, 16);
            pnlLeaderboard.Visible = true;
            pnlLeaderboard.BringToFront();
            pnlLeaderboard.Invalidate();
        }

        private DataGridView dgvMatchHistory;
        private TableLayoutPanel pnlMedalsGrid;
        private Label lblStatBestScore;
        private Label lblStatTotalMatches;
        private Label lblStatMedalCount;

        private void InitLeaderboardDialog()
        {
            if (pnlLeaderboard != null) return;

            pnlLeaderboard = new Panel
            {
                Parent = this,
                Visible = false,
                BackColor = Color.FromArgb(22, 26, 42),
                Size = new Size(920, 620)
            };

            // 1. Tiêu đề
            Label lblTitle = new Label
            {
                Text = "NHẬT KÝ CHIẾN TÍCH",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 204, 77),
                Location = new Point(24, 16),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Hồ sơ thành tích cá nhân & Tủ trưng bày huân chương danh dự",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(174, 184, 207),
                Location = new Point(26, 52),
                AutoSize = true
            };

            // 2. TabControl với 2 Tab (Chia đều 50% - 50%)
            int tabAreaWidth = pnlLeaderboard.Width - 48;
            int tabItemWidth = (tabAreaWidth - 4) / 2;

            TabControl tabRecords = new TabControl
            {
                Location = new Point(24, 82),
                Size = new Size(tabAreaWidth, pnlLeaderboard.Height - 146),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(tabItemWidth, 34)
            };

            TabPage tabProfile = new TabPage("Hồ Sơ Cá Nhân & Lịch Sử Đấu") { BackColor = Color.FromArgb(29, 34, 52) };
            TabPage tabMedals = new TabPage("Tủ Trưng Bày 12 Huân Chương") { BackColor = Color.FromArgb(29, 34, 52) };

            // === TAB 1: HỒ SƠ & LỊCH SỬ ===
            Panel pnlProfileWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10), BackColor = Color.FromArgb(29, 34, 52) };

            // 3 Thẻ chỉ số cá nhân ở phía trên
            Panel pnlStatsBar = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.Transparent };
            int cardW = (tabAreaWidth - 28 - 24) / 3;

            Panel card1 = CreateStatCard("🏆 KỶ LỤC CAO NHẤT", "0 điểm", out lblStatBestScore, new Point(0, 0), cardW);
            Panel card2 = CreateStatCard("🎮 TỔNG SỐ VÁN ĐẤU", "0 ván", out lblStatTotalMatches, new Point(cardW + 12, 0), cardW);
            Panel card3 = CreateStatCard("🏅 HUÂN CHƯƠNG ĐÃ ĐẠT", "0 / 12", out lblStatMedalCount, new Point((cardW + 12) * 2, 0), cardW);

            pnlStatsBar.Controls.Add(card1);
            pnlStatsBar.Controls.Add(card2);
            pnlStatsBar.Controls.Add(card3);

            // Bảng lịch sử ván đấu ở phía dưới
            dgvMatchHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.FromArgb(29, 34, 52),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(60, 69, 93),
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvMatchHistory.RowTemplate.Height = 36;
            dgvMatchHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(42, 48, 70);
            dgvMatchHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(255, 204, 77);
            dgvMatchHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvMatchHistory.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvMatchHistory.DefaultCellStyle.BackColor = Color.FromArgb(29, 34, 52);
            dgvMatchHistory.DefaultCellStyle.ForeColor = Color.FromArgb(232, 236, 244);
            dgvMatchHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 62, 88);
            dgvMatchHistory.DefaultCellStyle.SelectionForeColor = Color.White;

            var colMatch = new DataGridViewTextBoxColumn { HeaderText = "Ván Đấu", FillWeight = 16F };
            colMatch.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var colScore = new DataGridViewTextBoxColumn { HeaderText = "Điểm Đạt Được", FillWeight = 26F };
            colScore.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            colScore.DefaultCellStyle.ForeColor = Color.FromArgb(255, 204, 77);

            var colTier = new DataGridViewTextBoxColumn { HeaderText = "Xếp Loại Cấp Độ", FillWeight = 32F };
            colTier.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            var colDate = new DataGridViewTextBoxColumn { HeaderText = "Thời Gian Chơi", FillWeight = 26F };
            colDate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvMatchHistory.Columns.AddRange(new DataGridViewColumn[] { colMatch, colScore, colTier, colDate });

            Panel pnlTableContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
            pnlTableContainer.Controls.Add(dgvMatchHistory);

            pnlProfileWrap.Controls.Add(pnlTableContainer);
            pnlProfileWrap.Controls.Add(pnlStatsBar);
            tabProfile.Controls.Add(pnlProfileWrap);

            // === TAB 2: TỦ TRƯNG BÀY HUÂN CHƯƠNG (LƯỚI 4 CỘT x 3 DÒNG KHÔNG CUỘN) ===
            Panel pnlMedalsWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 10), BackColor = Color.FromArgb(29, 34, 52) };

            pnlMedalsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++) pnlMedalsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            for (int i = 0; i < 3; i++) pnlMedalsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

            pnlMedalsWrap.Controls.Add(pnlMedalsGrid);
            tabMedals.Controls.Add(pnlMedalsWrap);

            tabRecords.TabPages.Add(tabProfile);
            tabRecords.TabPages.Add(tabMedals);

            tabRecords.DrawItem += (s, ev) =>
            {
                if (ev.Index < 0) return;
                bool selected = ev.Index == tabRecords.SelectedIndex;
                Color bg = selected ? Color.FromArgb(255, 204, 77) : Color.FromArgb(42, 48, 70);
                Color fg = selected ? Color.FromArgb(22, 26, 42) : Color.FromArgb(232, 236, 244);
                using (SolidBrush brush = new SolidBrush(bg)) ev.Graphics.FillRectangle(brush, ev.Bounds);
                TextRenderer.DrawText(ev.Graphics, tabRecords.TabPages[ev.Index].Text, tabRecords.Font, ev.Bounds, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // 3. Nút Đóng
            Button btnClose = new Button
            {
                Text = "ĐÓNG",
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(pnlLeaderboard.Width - 134, pnlLeaderboard.Height - 48),
                BackColor = Color.FromArgb(255, 204, 77),
                ForeColor = Color.FromArgb(22, 26, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += btnCloseLeaderboard_Click;
            BoardRenderer.ApplyRoundedRegion(btnClose, 10);

            pnlLeaderboard.Paint += (s, ev) =>
            {
                using (var path = BoardRenderer.CreateRoundedPath(new Rectangle(1, 1, pnlLeaderboard.Width - 3, pnlLeaderboard.Height - 3), 16))
                using (Pen borderPen = new Pen(Color.FromArgb(140, 255, 204, 77), 2f))
                {
                    ev.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    ev.Graphics.DrawPath(borderPen, path);
                }
            };

            pnlLeaderboard.Controls.Add(lblTitle);
            pnlLeaderboard.Controls.Add(lblSubtitle);
            pnlLeaderboard.Controls.Add(tabRecords);
            pnlLeaderboard.Controls.Add(btnClose);
            BoardRenderer.ApplyRoundedRegion(pnlLeaderboard, 16);
        }

        private Panel CreateStatCard(string header, string value, out Label lblVal, Point loc, int width)
        {
            Panel card = new Panel
            {
                Size = new Size(width, 70),
                Location = loc,
                BackColor = Color.FromArgb(22, 26, 42)
            };
            Label lblHead = new Label
            {
                Text = header,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(174, 184, 207),
                Location = new Point(8, 8),
                AutoSize = true
            };
            lblVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 204, 77),
                Location = new Point(8, 30),
                AutoSize = true
            };
            card.Controls.Add(lblHead);
            card.Controls.Add(lblVal);
            BoardRenderer.ApplyRoundedRegion(card, 10);
            return card;
        }

        private void RefreshLeaderboardData()
        {
            var matches = ScoreManager.LoadMatchHistory();
            var medals = ScoreManager.LoadMedals();
            int bestScore = ScoreManager.LoadSavedHighScore();

            // 1. Cập nhật 3 Thẻ thống kê
            if (lblStatBestScore != null) lblStatBestScore.Text = $"{bestScore:N0} điểm";
            if (lblStatTotalMatches != null) lblStatTotalMatches.Text = $"{matches.Count} ván đấu";
            if (lblStatMedalCount != null)
            {
                int unlockedCount = medals.Count(m => m.IsUnlocked);
                lblStatMedalCount.Text = $"{unlockedCount} / {medals.Count} Huân chương";
            }

            // 2. Cập nhật bảng lịch sử ván đấu
            if (dgvMatchHistory != null)
            {
                dgvMatchHistory.Rows.Clear();
                foreach (var m in matches.Take(15))
                {
                    dgvMatchHistory.Rows.Add($"Ván #{m.MatchIndex}", $"{m.Score:N0}", m.RankTier, m.Date.ToString("dd/MM/yyyy HH:mm"));
                }
            }

            // 3. Cập nhật Tủ trưng bày 12 Huân chương (Lưới 4x3 vừa vặn)
            if (pnlMedalsGrid != null)
            {
                pnlMedalsGrid.Controls.Clear();
                foreach (var medal in medals)
                {
                    Panel card = new Panel
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(4),
                        BackColor = medal.IsUnlocked ? Color.FromArgb(36, 44, 70) : Color.FromArgb(20, 23, 34)
                    };

                    Label lblIcon = new Label
                    {
                        Text = medal.Icon,
                        Font = new Font("Segoe UI", 20F),
                        Location = new Point(4, 8),
                        Size = new Size(42, 42),
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    Label lblName = new Label
                    {
                        Text = medal.Title,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = medal.IsUnlocked ? Color.FromArgb(255, 204, 77) : Color.FromArgb(120, 125, 145),
                        Location = new Point(48, 6),
                        Size = new Size(150, 18)
                    };

                    Label lblDesc = new Label
                    {
                        Text = medal.Description,
                        Font = new Font("Segoe UI", 7.5F),
                        ForeColor = medal.IsUnlocked ? Color.FromArgb(190, 200, 225) : Color.FromArgb(80, 85, 105),
                        Location = new Point(48, 24),
                        Size = new Size(150, 28)
                    };

                    Label lblStatus = new Label
                    {
                        Text = medal.IsUnlocked ? $"★ {medal.UnlockedDate:dd/MM}" : "🔒 CHƯA ĐẠT",
                        Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                        ForeColor = medal.IsUnlocked ? Color.FromArgb(46, 204, 113) : Color.FromArgb(100, 105, 125),
                        Location = new Point(48, 52),
                        AutoSize = true
                    };

                    card.Controls.Add(lblIcon);
                    card.Controls.Add(lblName);
                    card.Controls.Add(lblDesc);
                    card.Controls.Add(lblStatus);
                    BoardRenderer.ApplyRoundedRegion(card, 8);
                    pnlMedalsGrid.Controls.Add(card);
                }
            }
        }

        private void btnCloseLeaderboard_Click(object sender, EventArgs e)
        {
            if (pnlLeaderboard != null) pnlLeaderboard.Visible = false;
            // Nếu mở từ Menu Tạm dừng thì quay lại Pause; nếu mở từ Menu chính thì quay về Menu chính
            if (isDialogOpenedFromPause)
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
    }
}