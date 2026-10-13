using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlockBlast
{
    public partial class FormCredits : Form
    {
        public FormCredits()
        {
            InitializeComponent();
            LoadCreditsContent();
        }

        private void LoadCreditsContent()
        {
            lblTeamIntroduction.Text = "Tên nhóm: " + CreditsContent.TeamName +
                "\r\nDự án: " + CreditsContent.ProjectName +
                "\r\n" + CreditsContent.TeamIntroduction;
            dgvMembers.DataSource = CreditsContent.Members;

            FillSections(rtbMotivation, CreditsNarrative.Motivation);
            FillSections(rtbGuide, CreditsNarrative.Guide);
            FillSections(rtbInnovation, CreditsNarrative.Innovation);
        }

        private static void FillSections(RichTextBox box, CreditsSection[] sections)
        {
            box.Clear();
            using (Font headingFont = new Font(box.Font.FontFamily, 13F, FontStyle.Bold))
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
            box.SelectionLength = 0;
            box.ScrollToCaret();
        }

        private void tabCredits_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = e.Index == tabCredits.SelectedIndex;
            Color background = selected
                ? Color.FromArgb(255, 159, 67)
                : Color.FromArgb(42, 48, 70);
            Color foreground = selected
                ? Color.FromArgb(22, 26, 42)
                : Color.FromArgb(232, 236, 244);

            using (SolidBrush brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            TextRenderer.DrawText(e.Graphics, tabCredits.TabPages[e.Index].Text,
                tabCredits.Font, e.Bounds, foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
            if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle();
        }

        // Giới hạn kích thước theo màn hình hiện tại, kể cả khi Windows dùng mức DPI lớn.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int maxWidth = Math.Max(320, area.Width - 32);
            int maxHeight = Math.Max(240, area.Height - 32);
            MinimumSize = new Size(Math.Min(MinimumSize.Width, maxWidth),
                Math.Min(MinimumSize.Height, maxHeight));
            Size = new Size(Math.Min(Width, maxWidth), Math.Min(Height, maxHeight));

            Rectangle reference = Owner != null ? Owner.Bounds : area;
            int x = reference.Left + (reference.Width - Width) / 2;
            int y = reference.Top + (reference.Height - Height) / 2;
            Location = new Point(Math.Max(area.Left, Math.Min(x, area.Right - Width)),
                Math.Max(area.Top, Math.Min(y, area.Bottom - Height)));
        }
    }
}
