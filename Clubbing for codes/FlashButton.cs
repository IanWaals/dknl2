using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class FlashButton : Button
{
    private bool isHovering = false;
    private bool isActive = false;

    public Color BaseColor { get; set; } = Color.FromArgb(255, 75, 0); // dark red-orange
    public Color ActiveColor { get; set; } = Color.FromArgb(255, 180, 50); // bright flashing accent
    public Color HoverColor { get; set; } = Color.FromArgb(255, 120, 0);

    public FlashButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10, FontStyle.Bold);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;

        MouseEnter += (s, e) => { isHovering = true; Invalidate(); };
        MouseLeave += (s, e) => { isHovering = false; Invalidate(); };
    }

    // Call this from your flashing toggle logic
    public void SetActive(bool active)
    {
        isActive = active;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(Parent.BackColor);

        // Determine current color
        Color currentColor = isActive ? ActiveColor : BaseColor;
        if (isHovering) currentColor = BlendColors(currentColor, HoverColor, 0.5f);

        // Draw gradient background
        using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle,
            ControlPaint.Light(currentColor),
            ControlPaint.Dark(currentColor),
            LinearGradientMode.Vertical))
        {
            GraphicsPath path = RoundedRect(ClientRectangle, 10);
            pevent.Graphics.FillPath(brush, path);

            // Optional: subtle glow
            using (Pen glowPen = new Pen(Color.FromArgb(80, currentColor), 3))
            {
                pevent.Graphics.DrawPath(glowPen, path);
            }
        }

        // Draw text
        TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle,
            ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private Color BlendColors(Color c1, Color c2, float ratio)
    {
        int r = (int)(c1.R + (c2.R - c1.R) * ratio);
        int g = (int)(c1.G + (c2.G - c1.G) * ratio);
        int b = (int)(c1.B + (c2.B - c1.B) * ratio);
        return Color.FromArgb(r, g, b);
    }
}
