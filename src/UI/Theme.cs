using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RoomPilot
{
    public static class Theme
    {
        public static readonly Color Background = Color.FromArgb(17, 22, 29);
        public static readonly Color Surface = Color.FromArgb(27, 34, 44);
        public static readonly Color Border = Color.FromArgb(47, 58, 72);
        public static readonly Color Text = Color.FromArgb(238, 242, 247);
        public static readonly Color Muted = Color.FromArgb(160, 174, 192);
        public static readonly Color Accent = Color.FromArgb(108, 225, 186);
        public static readonly Color Ink = Color.FromArgb(15, 38, 33);
        public static Font Font(float size, FontStyle style)
        {
            return new Font("Segoe UI", size, style);
        }

        public static Label Label(string text, int x, int y, int width, int height, float size, Color color)
        {
            return new Label
            {
                Text = text,
                Bounds = new Rectangle(x, y, width, height),
                ForeColor = color,
                BackColor = Color.Transparent,
                Font = Font(size, FontStyle.Regular),
                AutoEllipsis = true
            };
        }

        public static Button Button(string text, int x, int y, int width, int height, bool primary)
        {
            var button = new ThemedButton
            {
                Text = text,
                Bounds = new Rectangle(x, y, width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? Accent : Surface,
                ForeColor = primary ? Ink : Text,
                Font = Font(10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Border;
            return button;
        }

        public static void Form(Form f, string title, int width, int height)
        {
            f.Text = title;
            f.ClientSize = new Size(width, height);
            f.BackColor = Background;
            f.ForeColor = Text;
            f.Font = Font(10, FontStyle.Regular);
            f.AutoScaleMode = AutoScaleMode.Dpi;
            f.AutoScaleDimensions = new SizeF(96f, 96f);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.FormBorderStyle = FormBorderStyle.FixedSingle;
            f.MaximizeBox = false;
        }

        public static Icon CreateIcon()
        {
            using (var bitmap = new Bitmap(64, 64))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (var brush = new SolidBrush(Accent))
                        g.FillEllipse(brush, 1, 1, 62, 62);
                    using (var pen = new Pen(Ink, 4))
                    {
                        pen.StartCap = pen.EndCap = LineCap.Round;
                        g.DrawLines(pen, new[] { new Point(15, 30), new Point(32, 15), new Point(49, 30) });
                        g.DrawLines(pen, new[] { new Point(20, 29), new Point(20, 47), new Point(44, 47), new Point(44, 29) });
                        g.DrawLine(pen, 32, 34, 32, 46);
                    }
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(handle).Clone();
                }
                finally
                {
                    Native.DestroyIcon(handle);
                }
            }
        }
    }
}
