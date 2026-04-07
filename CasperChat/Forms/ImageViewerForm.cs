using System;
using System.Drawing;
using System.Windows.Forms;
using CasperChat.Client.Services;

namespace CasperChat.Client.Forms
{
    public class ImageViewerForm : Form
    {
        private readonly Color appBackColor = Color.FromArgb(28, 32, 38);
        private readonly Color sidebarColor = Color.FromArgb(36, 41, 48);
        private readonly Color textPrimary = Color.FromArgb(230, 232, 235);

        public ImageViewerForm(string fileName, string imageUrl)
        {
            Text = fileName;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 700);
            MinimumSize = new Size(700, 500);
            BackColor = appBackColor;
            ForeColor = textPrimary;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = sidebarColor,
                Padding = new Padding(16, 12, 16, 12)
            };

            var titleLabel = new Label
            {
                Text = fileName,
                Dock = DockStyle.Fill,
                ForeColor = textPrimary,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                AutoEllipsis = true
            };

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = appBackColor
            };

            try
            {
                pictureBox.LoadAsync(imageUrl);
            }
            catch
            {
            }

            topPanel.Controls.Add(titleLabel);
            Controls.Add(pictureBox);
            Controls.Add(topPanel);

            Shown += (_, _) => CaptureProtectionService.Apply(this);
        }
    }
}