namespace BlockBlast
{
    partial class FormCredits
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.tabCredits = new System.Windows.Forms.TabControl();
            this.tabTeam = new System.Windows.Forms.TabPage();
            this.tabMotivation = new System.Windows.Forms.TabPage();
            this.tabGuide = new System.Windows.Forms.TabPage();
            this.tabInnovation = new System.Windows.Forms.TabPage();
            this.teamLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTeamIntroduction = new System.Windows.Forms.Label();
            this.dgvMembers = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStudentId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRole = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblTeamThanks = new System.Windows.Forms.Label();
            this.rtbMotivation = new System.Windows.Forms.RichTextBox();
            this.rtbGuide = new System.Windows.Forms.RichTextBox();
            this.rtbInnovation = new System.Windows.Forms.RichTextBox();
            this.footerPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnClose = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            this.tabCredits.SuspendLayout();
            this.tabTeam.SuspendLayout();
            this.tabMotivation.SuspendLayout();
            this.tabGuide.SuspendLayout();
            this.tabInnovation.SuspendLayout();
            this.teamLayout.SuspendLayout();
            this.footerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMembers)).BeginInit();
            this.SuspendLayout();

            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowCount = 4;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Padding = new System.Windows.Forms.Padding(20);
            this.mainLayout.Controls.Add(this.lblTitle, 0, 0);
            this.mainLayout.Controls.Add(this.lblSubtitle, 0, 1);
            this.mainLayout.Controls.Add(this.tabCredits, 0, 2);
            this.mainLayout.Controls.Add(this.footerPanel, 0, 3);
            this.mainLayout.Name = "mainLayout";

            this.lblTitle.Text = "BLOCK BLAST";
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 25F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(255, 159, 67);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTitle.Name = "lblTitle";

            this.lblSubtitle.Text = "Giới thiệu dự án & nhóm phát triển";
            this.lblSubtitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(174, 184, 207);
            this.lblSubtitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblSubtitle.Name = "lblSubtitle";

            this.tabCredits.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabCredits.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.tabCredits.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabCredits.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabCredits.ItemSize = new System.Drawing.Size(160, 38);
            this.tabCredits.Controls.Add(this.tabTeam);
            this.tabCredits.Controls.Add(this.tabMotivation);
            this.tabCredits.Controls.Add(this.tabGuide);
            this.tabCredits.Controls.Add(this.tabInnovation);
            this.tabCredits.Name = "tabCredits";
            this.tabCredits.SelectedIndex = 0;
            this.tabCredits.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.tabCredits_DrawItem);

            this.tabTeam.Text = "Giới thiệu nhóm";
            this.tabTeam.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.tabTeam.Padding = new System.Windows.Forms.Padding(16);
            this.tabTeam.Controls.Add(this.teamLayout);
            this.tabTeam.Name = "tabTeam";

            this.teamLayout.ColumnCount = 1;
            this.teamLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.teamLayout.RowCount = 3;
            this.teamLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.teamLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.teamLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.teamLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.teamLayout.Controls.Add(this.lblTeamIntroduction, 0, 0);
            this.teamLayout.Controls.Add(this.dgvMembers, 0, 1);
            this.teamLayout.Controls.Add(this.lblTeamThanks, 0, 2);
            this.teamLayout.Name = "teamLayout";

            this.lblTeamIntroduction.AutoSize = true;
            this.lblTeamIntroduction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTeamIntroduction.Margin = new System.Windows.Forms.Padding(0, 0, 0, 14);
            this.lblTeamIntroduction.Name = "lblTeamIntroduction";

            this.dgvMembers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMembers.AutoGenerateColumns = false;
            this.dgvMembers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colName, this.colStudentId, this.colRole});
            this.dgvMembers.ReadOnly = true;
            this.dgvMembers.AllowUserToAddRows = false;
            this.dgvMembers.AllowUserToDeleteRows = false;
            this.dgvMembers.AllowUserToResizeRows = false;
            this.dgvMembers.MultiSelect = false;
            this.dgvMembers.RowHeadersVisible = false;
            this.dgvMembers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvMembers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvMembers.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvMembers.RowTemplate.MinimumHeight = 38;
            this.dgvMembers.BackgroundColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.dgvMembers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvMembers.GridColor = System.Drawing.Color.FromArgb(60, 69, 93);
            this.dgvMembers.EnableHeadersVisualStyles = false;
            this.dgvMembers.ColumnHeadersHeight = 40;
            this.dgvMembers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvMembers.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(42, 48, 70);
            this.dgvMembers.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvMembers.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(42, 48, 70);
            this.dgvMembers.ColumnHeadersDefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvMembers.ColumnHeadersDefaultCellStyle.Padding = new System.Windows.Forms.Padding(7);
            this.dgvMembers.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.dgvMembers.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(232, 236, 244);
            this.dgvMembers.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(60, 74, 105);
            this.dgvMembers.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            this.dgvMembers.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(7);
            this.dgvMembers.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvMembers.Margin = new System.Windows.Forms.Padding(0);
            this.dgvMembers.Name = "dgvMembers";

            this.colName.Name = "colName";
            this.colName.HeaderText = "Họ tên";
            this.colName.DataPropertyName = "FullName";
            this.colName.FillWeight = 32F;
            this.colStudentId.Name = "colStudentId";
            this.colStudentId.HeaderText = "MSSV";
            this.colStudentId.DataPropertyName = "StudentId";
            this.colStudentId.FillWeight = 30F;
            this.colRole.Name = "colRole";
            this.colRole.HeaderText = "Vai trò";
            this.colRole.DataPropertyName = "Role";
            this.colRole.FillWeight = 38F;

            this.lblTeamThanks.Text = "Cảm ơn bạn đã trải nghiệm sản phẩm của nhóm!";
            this.lblTeamThanks.AutoSize = true;
            this.lblTeamThanks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTeamThanks.Margin = new System.Windows.Forms.Padding(0, 14, 0, 0);
            this.lblTeamThanks.ForeColor = System.Drawing.Color.FromArgb(174, 184, 207);
            this.lblTeamThanks.Name = "lblTeamThanks";

            this.tabMotivation.Text = "Động lực phát triển";
            this.tabMotivation.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.tabMotivation.Padding = new System.Windows.Forms.Padding(16);
            this.tabMotivation.Controls.Add(this.rtbMotivation);
            this.tabMotivation.Name = "tabMotivation";
            this.tabGuide.Text = "Hướng dẫn game";
            this.tabGuide.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.tabGuide.Padding = new System.Windows.Forms.Padding(16);
            this.tabGuide.Controls.Add(this.rtbGuide);
            this.tabGuide.Name = "tabGuide";
            this.tabInnovation.Text = "Tính mới của game";
            this.tabInnovation.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.tabInnovation.Padding = new System.Windows.Forms.Padding(16);
            this.tabInnovation.Controls.Add(this.rtbInnovation);
            this.tabInnovation.Name = "tabInnovation";

            this.rtbMotivation.Name = "rtbMotivation";
            this.rtbMotivation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbMotivation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbMotivation.ReadOnly = true;
            this.rtbMotivation.WordWrap = true;
            this.rtbMotivation.DetectUrls = false;
            this.rtbMotivation.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbMotivation.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.rtbMotivation.ForeColor = System.Drawing.Color.FromArgb(232, 236, 244);
            this.rtbGuide.Name = "rtbGuide";
            this.rtbGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbGuide.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbGuide.ReadOnly = true;
            this.rtbGuide.WordWrap = true;
            this.rtbGuide.DetectUrls = false;
            this.rtbGuide.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbGuide.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.rtbGuide.ForeColor = System.Drawing.Color.FromArgb(232, 236, 244);
            this.rtbInnovation.Name = "rtbInnovation";
            this.rtbInnovation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbInnovation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbInnovation.ReadOnly = true;
            this.rtbInnovation.WordWrap = true;
            this.rtbInnovation.DetectUrls = false;
            this.rtbInnovation.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbInnovation.BackColor = System.Drawing.Color.FromArgb(29, 34, 52);
            this.rtbInnovation.ForeColor = System.Drawing.Color.FromArgb(232, 236, 244);

            this.footerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.footerPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.footerPanel.WrapContents = false;
            this.footerPanel.Margin = new System.Windows.Forms.Padding(0);
            this.footerPanel.Padding = new System.Windows.Forms.Padding(0, 14, 0, 0);
            this.footerPanel.Controls.Add(this.btnClose);
            this.footerPanel.Name = "footerPanel";
            this.btnClose.Text = "ĐÓNG";
            this.btnClose.Size = new System.Drawing.Size(120, 40);
            this.btnClose.Margin = new System.Windows.Forms.Padding(0);
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(255, 159, 67);
            this.btnClose.ForeColor = System.Drawing.Color.FromArgb(22, 26, 42);
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Name = "btnClose";

            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(860, 580);
            this.MinimumSize = new System.Drawing.Size(740, 460);
            this.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.BackColor = System.Drawing.Color.FromArgb(22, 26, 42);
            this.ForeColor = System.Drawing.Color.FromArgb(232, 236, 244);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.CancelButton = this.btnClose;
            this.Controls.Add(this.mainLayout);
            this.Text = "Block Blast - Giới thiệu & Credits";
            this.Name = "FormCredits";

            this.mainLayout.ResumeLayout(false);
            this.tabCredits.ResumeLayout(false);
            this.tabTeam.ResumeLayout(false);
            this.tabMotivation.ResumeLayout(false);
            this.tabGuide.ResumeLayout(false);
            this.tabInnovation.ResumeLayout(false);
            this.teamLayout.ResumeLayout(false);
            this.teamLayout.PerformLayout();
            this.footerPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvMembers)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.TabControl tabCredits;
        private System.Windows.Forms.TabPage tabTeam;
        private System.Windows.Forms.TabPage tabMotivation;
        private System.Windows.Forms.TabPage tabGuide;
        private System.Windows.Forms.TabPage tabInnovation;
        private System.Windows.Forms.TableLayoutPanel teamLayout;
        private System.Windows.Forms.Label lblTeamIntroduction;
        private System.Windows.Forms.DataGridView dgvMembers;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStudentId;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRole;
        private System.Windows.Forms.Label lblTeamThanks;
        private System.Windows.Forms.RichTextBox rtbMotivation;
        private System.Windows.Forms.RichTextBox rtbGuide;
        private System.Windows.Forms.RichTextBox rtbInnovation;
        private System.Windows.Forms.FlowLayoutPanel footerPanel;
        private System.Windows.Forms.Button btnClose;
    }
}
