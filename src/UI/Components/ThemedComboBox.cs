using System.Drawing;
using System.Windows.Forms;

namespace RoomPilot
{
    sealed class ThemedComboBox : ComboBox
    {
        public ThemedComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 22;
            FlatStyle = FlatStyle.Flat;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            DropDownStyle = ComboBoxStyle.DropDownList;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Bounds.Width <= 0)
                return;
            Color background = (e.State & DrawItemState.Selected) != 0 ? Color.FromArgb(44, 66, 70) : BackColor;
            using (var brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);
            object item = e.Index >= 0 && e.Index < Items.Count ? Items[e.Index] : SelectedItem;
            var bounds = e.Bounds;
            bounds.X += 5;
            bounds.Width -= 8;
            TextRenderer.DrawText(e.Graphics, item == null ? "" : GetItemText(item), Font, bounds, Enabled ? ForeColor : Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if ((e.State & DrawItemState.Focus) != 0)
                e.DrawFocusRectangle();
        }
    }
}
