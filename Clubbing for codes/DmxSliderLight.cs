using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Clubbing_for_coders
{
    public class DmxSliderLight : Control
    {
        [Category("DMX")]
        public int Segments { get; set; } = 12;

        [Category("DMX")]
        public bool RoundLeds { get; set; } = true;

        [Category("DMX")]
        public bool Glow { get; set; } = true;

        [Category("DMX")]
        public Color FixedColor { get; set; } = Color.Red;

        [Category("DMX")]
        public Color OffColor { get; set; } = Color.FromArgb(100, 100, 100);

        private float[] animLevel;
        private Timer animTimer;
        private int _value = 0;

        [Category("DMX")]
        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0, Math.Min(Segments, value));
                ValueChanged?.Invoke(_value);
                Invalidate();
            }
        }

        public event Action<int> ValueChanged;

        public DmxSliderLight()
        {
            // ENABLE TRUE TRANSPARENCY
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            DoubleBuffered = true;
            animLevel = new float[200];
        for (int i = 0; i < animLevel.Length; i++)
            animLevel[i] = 0.3f;   // Start with slight brightness

            animTimer = new Timer();
            animTimer.Interval = 40;
            animTimer.Tick += (s, e) =>
            {
                for (int i = 0; i < Segments; i++)
                {
                    float target = i < _value ? 1f : 0f;
                    animLevel[i] += (target - animLevel[i]) * 0.25f;
                }
                Invalidate();
            };
            animTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e) => HandleClick(e.X);
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                HandleClick(e.X);
        }

        private void HandleClick(int mouseX)
        {
            float segWidth = (float)Width / Segments;
            int v = (int)(mouseX / segWidth);
            Value = v;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width - Padding.Left - Padding.Right;   // entire width for LEDs
            int h = Height - Padding.Top - Padding.Bottom;   // entire height for LEDs

            float segWidth = (float)w / Segments;

            // Draw label
            using (var lblBrush = new SolidBrush(Color.White))
            using (var fmt = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Far
            })

            for (int i = 0; i < Segments; i++)
            {
                RectangleF rect = new RectangleF(
                    Padding.Left + i * segWidth + 4,
                    Padding.Top + 4,
                    segWidth - 8,
                    h - 8
                );

                float brightness = animLevel[i];

                // Base stage-light brightness from animation
                float animExpo = (float)Math.Pow(brightness, 1.8f);

                // Additional brightness depending on segment index (left = dim, right = bright)
                float position = (float)i / (Segments - 1);   // 0 → 1 across the slider
                float posExpo = (float)Math.Pow(position, 2.2f); // curve for dramatic ramp

                // Final brightness combines segment position + animation fade
                float expo = animExpo * (0.3f + 0.7f * posExpo);


                // Smooth color blend
                Color final = InterpolateColor(OffColor, FixedColor, expo);

                // Glow
                if (Glow && expo > 0.05f)
                {
                    RectangleF glowRect = rect;
                    glowRect.Inflate(6, 6);
                    using (Brush gb = new SolidBrush(Color.FromArgb((int)(expo * 220), FixedColor)))
                        e.Graphics.FillEllipse(gb, glowRect);
                }

                // LED fill
                using (Brush b = new SolidBrush(final))
                {
                    if (RoundLeds)
                        e.Graphics.FillEllipse(b, rect);
                    else
                        e.Graphics.FillRectangle(b, rect);
                }

                // Reflection highlight
                if (expo > 0.15f)
                {
                    RectangleF highlight = rect;
                    highlight.Inflate(-4, -4);
                    highlight.Height /= 2f;

                    using (var lg = new LinearGradientBrush(
                        highlight,
                        Color.FromArgb((int)(expo * 120), Color.White),
                        Color.FromArgb(0, Color.White),
                        90f))
                    {
                        if (RoundLeds)
                            e.Graphics.FillEllipse(lg, highlight);
                        else
                            e.Graphics.FillRectangle(lg, highlight);
                    }
                }

                // LED border
                using (Pen p = new Pen(Color.FromArgb(60, 60, 60)))
                {
                    if (RoundLeds)
                        e.Graphics.DrawEllipse(p, rect);
                    else
                        e.Graphics.DrawRectangle(p, Rectangle.Round(rect));
                }
            }
        }

        private Color InterpolateColor(Color off, Color on, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));

            int r = (int)(off.R + (on.R - off.R) * t);
            int g = (int)(off.G + (on.G - off.G) * t);
            int b = (int)(off.B + (on.B - off.B) * t);

            return Color.FromArgb(r, g, b);
        }
    }
}
