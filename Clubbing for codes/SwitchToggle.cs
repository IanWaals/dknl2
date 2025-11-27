using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

public class ToggleSwitch : Control
{
    public bool IsOn { get; set; } = false; // start OFF
    private Timer animTimer;
    private float knobX = 2;

    public event EventHandler Toggled;

    public ToggleSwitch()
    {
        Width = 60;
        Height = 30;
        DoubleBuffered = true;
        Cursor = Cursors.Hand;

        animTimer = new Timer();
        animTimer.Interval = 10;
        animTimer.Tick += AnimTick;
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        IsOn = !IsOn;      // flip state
        animTimer.Start(); // animate
        Toggled?.Invoke(this, EventArgs.Empty); // notify parent form
    }

    private void AnimTick(object sender, EventArgs e)
    {
        float target = IsOn ? Width - Height + 2 : 2;

        if (Math.Abs(knobX - target) < 2)
        {
            knobX = target;
            animTimer.Stop();
        }
        else
        {
            knobX += IsOn ? 2 : -2;
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Color trackColor = IsOn ? Color.LightGreen : Color.Gray;

        using (SolidBrush trackBrush = new SolidBrush(trackColor))
        using (SolidBrush knobBrush = new SolidBrush(Color.White))
        {
            e.Graphics.FillRoundedRectangle(trackBrush, new Rectangle(0, 0, Width, Height), Height);
            e.Graphics.FillEllipse(knobBrush, knobX, 2, Height - 5, Height - 5);
        }
    }
}

static class RoundedRectangleExtension
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using (GraphicsPath path = new GraphicsPath())
        {
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
