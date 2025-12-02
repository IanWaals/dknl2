using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DMXController
{
    public class DMXFader : UserControl
    {
        private int _value = 0;
        private int _minimum = 0;
        private int _maximum = 255;
        private bool _isDragging = false;
        private Color _faderColor = Color.FromArgb(70, 130, 180);
        private Color _faderCapColor = Color.FromArgb(120, 120, 120);
        private bool _showValue = true;
        private string _channelLabel = "";
        private string[] _scaleLabels = null;
        private bool _showScale = true;

        public DMXFader()
        {
            this.DoubleBuffered = true;
            this.Size = new Size(60, 300);
            this.MinimumSize = new Size(40, 150);
            this.BackColor = Color.Black;
        }

        [Category("DMX")]
        [Description("Current value of the fader")]
        public int Value
        {
            get => _value;
            set
            {
                if (value < _minimum) value = _minimum;
                if (value > _maximum) value = _maximum;
                if (_value != value)
                {
                    _value = value;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("DMX")]
        public int Minimum
        {
            get => _minimum;
            set { _minimum = value; Invalidate(); }
        }

        [Category("DMX")]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = value; Invalidate(); }
        }

        [Category("DMX")]
        public Color FaderColor
        {
            get => _faderColor;
            set { _faderColor = value; Invalidate(); }
        }

        [Category("DMX")]
        public Color FaderCapColor
        {
            get => _faderCapColor;
            set { _faderCapColor = value; Invalidate(); }
        }

        [Category("DMX")]
        public bool ShowValue
        {
            get => _showValue;
            set { _showValue = value; Invalidate(); }
        }

        [Category("DMX")]
        public string ChannelLabel
        {
            get => _channelLabel;
            set { _channelLabel = value; Invalidate(); }
        }

        [Category("DMX")]
        [Description("Custom labels for scale marks (top to bottom). If null, shows numeric values.")]
        public string[] ScaleLabels
        {
            get => _scaleLabels;
            set { _scaleLabels = value; Invalidate(); }
        }

        [Category("DMX")]
        [Description("Show or hide the scale marks and labels")]
        public bool ShowScale
        {
            get => _showScale;
            set { _showScale = value; Invalidate(); }
        }

        public event EventHandler ValueChanged;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Calculate dimensions
            int trackWidth = 8;
            int faderWidth = 40;
            int faderHeight = 20;
            int trackX = (Width - trackWidth) / 2;
            int padding = 30;
            int trackTop = padding;
            int trackBottom = Height - padding - 30;
            int trackHeight = trackBottom - trackTop;

            // Draw channel label
            if (!string.IsNullOrEmpty(_channelLabel))
            {
                using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(180, 180, 180)))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    g.DrawString(_channelLabel, font, brush, Width / 2, 5, sf);
                }
            }

            // Draw scale marks
            if (_showScale)
            {
                using (Pen scalePen = new Pen(Color.FromArgb(80, 80, 80), 1))
                using (Font font = new Font("Segoe UI", 7))
                using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
                {
                    if (_scaleLabels != null && _scaleLabels.Length > 0)
                    {
                        // Use custom labels - only show the ones provided
                        for (int i = 0; i < _scaleLabels.Length; i++)
                        {
                            // Position labels evenly from top to bottom
                            float percent = _scaleLabels.Length > 1 ? (float)i / (_scaleLabels.Length - 1) : 0;
                            int y = trackTop + (int)(trackHeight * percent);

                            string labelText = _scaleLabels[i];

                            // Draw tick mark
                            g.DrawLine(scalePen, trackX - 8, y, trackX - 3, y);

                            // Draw label
                            if (!string.IsNullOrEmpty(labelText))
                            {
                                StringFormat sf = new StringFormat();
                                sf.Alignment = StringAlignment.Far;
                                sf.LineAlignment = StringAlignment.Center;
                                g.DrawString(labelText, font, textBrush, trackX - 10, y, sf);
                            }
                        }
                    }
                    else
                    {
                        // Use numeric values (default) - show 11 marks
                        int numMarks = 11;
                        for (int i = 0; i < numMarks; i++)
                        {
                            int y = trackTop + (int)(trackHeight * i / (numMarks - 1.0));
                            int scaleValue = _maximum - (_maximum - _minimum) * i / (numMarks - 1);

                            // Draw tick mark and label every other mark
                            if (i % 2 == 0)
                            {
                                g.DrawLine(scalePen, trackX - 8, y, trackX - 3, y);

                                StringFormat sf = new StringFormat();
                                sf.Alignment = StringAlignment.Far;
                                sf.LineAlignment = StringAlignment.Center;
                                g.DrawString(scaleValue.ToString(), font, textBrush, trackX - 10, y, sf);
                            }
                            else
                            {
                                g.DrawLine(scalePen, trackX - 5, y, trackX - 3, y);
                            }
                        }
                    }
                }
            }

            // Draw track groove
            Rectangle trackRect = new Rectangle(trackX, trackTop, trackWidth, trackHeight);
            using (LinearGradientBrush brush = new LinearGradientBrush(
                trackRect, Color.FromArgb(20, 20, 20), Color.FromArgb(40, 40, 40), 90f))
            {
                g.FillRectangle(brush, trackRect);
            }

            // Draw track border
            using (Pen pen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                g.DrawRectangle(pen, trackRect);
            }

            // Draw filled portion (value indicator)
            float valuePercent = (float)(_value - _minimum) / (_maximum - _minimum);
            int filledHeight = (int)(trackHeight * valuePercent);
            if (filledHeight > 0)
            {
                Rectangle fillRect = new Rectangle(
                    trackX + 1,
                    trackBottom - filledHeight,
                    trackWidth - 2,
                    filledHeight);

                using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                    fillRect,
                    Color.FromArgb(100, _faderColor),
                    _faderColor,
                    90f))
                {
                    g.FillRectangle(fillBrush, fillRect);
                }
            }

            // Calculate fader position
            int faderY = trackBottom - (int)(trackHeight * valuePercent) - faderHeight / 2;

            // Draw fader cap shadow
            Rectangle shadowRect = new Rectangle(
                (Width - faderWidth) / 2 + 2,
                faderY + 2,
                faderWidth,
                faderHeight);
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, shadowRect);
            }

            // Draw fader cap
            Rectangle faderRect = new Rectangle(
                (Width - faderWidth) / 2,
                faderY,
                faderWidth,
                faderHeight);

            // Create lighter and darker versions of the fader cap color
            Color lightColor = ControlPaint.Light(_faderCapColor);
            Color darkColor = ControlPaint.Dark(_faderCapColor);

            using (LinearGradientBrush faderBrush = new LinearGradientBrush(
                faderRect,
                lightColor,
                darkColor,
                90f))
            {
                g.FillRectangle(faderBrush, faderRect);
            }

            // Draw fader border
            using (Pen faderPen = new Pen(ControlPaint.LightLight(_faderCapColor), 1))
            {
                g.DrawRectangle(faderPen, faderRect);
            }

            // Draw fader grip lines
            using (Pen gripPen = new Pen(ControlPaint.Dark(darkColor), 1))
            {
                int centerY = faderY + faderHeight / 2;
                for (int i = -2; i <= 2; i++)
                {
                    int lineY = centerY + i * 3;
                    g.DrawLine(gripPen,
                        faderRect.Left + 8, lineY,
                        faderRect.Right - 8, lineY);
                }
            }

            // Draw value display
            if (_showValue)
            {
                using (Font valueFont = new Font("Segoe UI", 9, FontStyle.Bold))
                using (SolidBrush valueBrush = new SolidBrush(Color.FromArgb(200, 200, 200)))
                {
                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Center;
                    string valueText = _value.ToString();
                    g.DrawString(valueText, valueFont, valueBrush, Width / 2, Height - 20, sf);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                UpdateValueFromMouse(e.Y);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                UpdateValueFromMouse(e.Y);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int delta = e.Delta > 0 ? 1 : -1;
            Value += delta * ((_maximum - _minimum) / 100);
        }

        private void UpdateValueFromMouse(int mouseY)
        {
            int padding = 30;
            int trackTop = padding;
            int trackBottom = Height - padding - 30;
            int trackHeight = trackBottom - trackTop;

            if (mouseY < trackTop) mouseY = trackTop;
            if (mouseY > trackBottom) mouseY = trackBottom;

            float percent = 1.0f - (float)(mouseY - trackTop) / trackHeight;
            Value = _minimum + (int)((_maximum - _minimum) * percent);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        private void DrawNumericScale(Graphics g, Pen scalePen, Font font, SolidBrush textBrush, int trackX, int trackTop, int trackHeight)
        {
            // Use numeric values (default) - show 11 marks
            int numMarks = 11;
            for (int i = 0; i < numMarks; i++)
            {
                int y = trackTop + (int)(trackHeight * i / (numMarks - 1.0));
                int scaleValue = _maximum - (_maximum - _minimum) * i / (numMarks - 1);

                // Draw tick mark and label every other mark
                if (i % 2 == 0)
                {
                    g.DrawLine(scalePen, trackX - 8, y, trackX - 3, y);

                    StringFormat sf = new StringFormat();
                    sf.Alignment = StringAlignment.Far;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString(scaleValue.ToString(), font, textBrush, trackX - 10, y, sf);
                }
                else
                {
                    g.DrawLine(scalePen, trackX - 5, y, trackX - 3, y);
                }
            }
        }
    }
}