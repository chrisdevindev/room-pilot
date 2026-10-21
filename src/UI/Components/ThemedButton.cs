using System.Drawing;
using System.Windows.Forms;

namespace RoomPilot
{
    sealed class ThemedButton : Button
    {
        public ThemedButton()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var brush = new SolidBrush(Enabled ? BackColor : Theme.Surface))
                e.Graphics.FillRectangle(brush, ClientRectangle);
            if (FlatAppearance.BorderSize > 0 || !Enabled)
                using (var pen = new Pen(Theme.Border))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? ForeColor : Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4));
        }
    }
}
