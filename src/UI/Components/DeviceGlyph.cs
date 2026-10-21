using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RoomPilot
{
    public class DeviceGlyph : Control
    {
        public int Kind { get; set; }

        public DeviceGlyph(int kind)
        {
            Kind = kind;
            DoubleBuffered = true;
            BackColor = Theme.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(Width / 56f, Height / 56f);
            using (var brush = new SolidBrush(Color.FromArgb(37, 53, 56)))
                g.FillEllipse(brush, 0, 0, 55, 55);
            using (var pen = new Pen(Theme.Accent, 2.2f))
            {
                if (Kind == 0)
                {
                    g.DrawEllipse(pen, 18, 12, 20, 22);
                    g.DrawLine(pen, 22, 35, 34, 35);
                    g.DrawLine(pen, 23, 40, 33, 40);
                    g.DrawLine(pen, 25, 44, 31, 44);
                }
                else if (Kind == 1)
                {
                    g.DrawRectangle(pen, 15, 13, 26, 32);
                    g.DrawEllipse(pen, 20, 21, 3, 3);
                    g.DrawEllipse(pen, 32, 21, 3, 3);
                    g.DrawEllipse(pen, 26, 32, 3, 3);
                }
                else
                {
                    g.DrawLine(pen, 13, 37, 43, 18);
                    g.DrawLine(pen, 14, 43, 44, 24);
                    g.DrawLine(pen, 17, 27, 20, 32);
                    g.DrawLine(pen, 26, 21, 29, 26);
                    g.DrawLine(pen, 35, 15, 38, 20);
                }
            }
        }
    }
}
