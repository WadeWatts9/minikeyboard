using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MiniKeyboardCyber
{
    public static class CyberTheme
    {
        // Core Cyberpunk Palette
        public static readonly Color BgDeep = Color.FromArgb(8, 10, 16);
        public static readonly Color BgPanel = Color.FromArgb(14, 18, 28);
        public static readonly Color BgCard = Color.FromArgb(20, 26, 40);
        public static readonly Color BgCardHover = Color.FromArgb(28, 38, 58);
        public static readonly Color BgKeyCap = Color.FromArgb(18, 22, 34);
        public static readonly Color BgKeyCapActive = Color.FromArgb(22, 42, 62);

        public static readonly Color NeonCyan = Color.FromArgb(0, 240, 255);
        public static readonly Color NeonMagenta = Color.FromArgb(255, 0, 95);
        public static readonly Color CyberYellow = Color.FromArgb(252, 238, 10);
        public static readonly Color NeonGreen = Color.FromArgb(0, 255, 128);
        public static readonly Color NeonPurple = Color.FromArgb(185, 75, 255);
        public static readonly Color NeonOrange = Color.FromArgb(255, 135, 0);

        public static readonly Color TextPrimary = Color.FromArgb(235, 245, 255);
        public static readonly Color TextSecondary = Color.FromArgb(140, 165, 195);
        public static readonly Color TextMuted = Color.FromArgb(85, 105, 130);
        public static readonly Color GridLine = Color.FromArgb(18, 0, 240, 255);

        public static Color GetCategoryColor(ActionCategory category)
        {
            switch (category)
            {
                case ActionCategory.Keyboard: return NeonCyan;
                case ActionCategory.Multimedia: return NeonMagenta;
                case ActionCategory.Mouse: return CyberYellow;
                case ActionCategory.Led: return NeonPurple;
                default: return TextMuted;
            }
        }

        public static GraphicsPath CreateChamferedRect(RectangleF rect, float cut)
        {
            GraphicsPath path = new GraphicsPath();
            float l = rect.Left;
            float t = rect.Top;
            float r = rect.Right;
            float b = rect.Bottom;
            float c = Math.Min(cut, Math.Min(rect.Width, rect.Height) * 0.35f);

            path.AddPolygon(new PointF[]
            {
                new PointF(l + c, t),
                new PointF(r - c * 0.3f, t),
                new PointF(r, t + c * 0.3f),
                new PointF(r, b - c),
                new PointF(r - c, b),
                new PointF(l + c * 0.3f, b),
                new PointF(l, b - c * 0.3f),
                new PointF(l, t + c)
            });
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d <= 0f)
            {
                path.AddRectangle(rect);
                return path;
            }
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawTechGrid(Graphics g, Rectangle bounds, int spacing)
        {
            using (Pen gridPen = new Pen(GridLine, 1f))
            {
                for (int x = bounds.Left; x < bounds.Right; x += spacing)
                {
                    g.DrawLine(gridPen, x, bounds.Top, x, bounds.Bottom);
                }
                for (int y = bounds.Top; y < bounds.Bottom; y += spacing)
                {
                    g.DrawLine(gridPen, bounds.Left, y, bounds.Right, y);
                }
            }
        }

        public static void DrawHexScrew(Graphics g, float cx, float cy, float radius)
        {
            RectangleF outer = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);
            using (LinearGradientBrush br = new LinearGradientBrush(outer, Color.FromArgb(65, 75, 95), Color.FromArgb(25, 30, 42), 45f))
            using (Pen borderPen = new Pen(Color.FromArgb(110, 0, 240, 255), 1.2f))
            {
                g.FillEllipse(br, outer);
                g.DrawEllipse(borderPen, outer);
            }

            PointF[] hex = new PointF[6];
            float innerR = radius * 0.55f;
            for (int i = 0; i < 6; i++)
            {
                double angle = i * Math.PI / 3.0;
                hex[i] = new PointF(
                    cx + (float)(innerR * Math.Cos(angle)),
                    cy + (float)(innerR * Math.Sin(angle))
                );
            }
            using (SolidBrush holeBr = new SolidBrush(Color.FromArgb(12, 15, 22)))
            using (Pen hexPen = new Pen(Color.FromArgb(90, 130, 160), 1f))
            {
                g.FillPolygon(holeBr, hex);
                g.DrawPolygon(hexPen, hex);
            }
        }

        public static void DrawRotaryEncoderDial(Graphics g, float cx, float cy, float radius, Color accentColor, bool isSelected)
        {
            // Outer neon halo
            for (int i = 3; i >= 1; i--)
            {
                float haloR = radius + i * 3.5f;
                using (Pen glowPen = new Pen(Color.FromArgb(isSelected ? 48 : 22, accentColor), 2.5f))
                {
                    g.DrawEllipse(glowPen, cx - haloR, cy - haloR, haloR * 2, haloR * 2);
                }
            }

            // Knurled metallic teeth around circumference
            int teeth = 28;
            using (Pen toothPen = new Pen(Color.FromArgb(160, accentColor), 1.6f))
            {
                for (int i = 0; i < teeth; i++)
                {
                    double a = (i * 2.0 * Math.PI) / teeth;
                    float x1 = cx + (float)((radius - 3f) * Math.Cos(a));
                    float y1 = cy + (float)((radius - 3f) * Math.Sin(a));
                    float x2 = cx + (float)((radius + 1.5f) * Math.Cos(a));
                    float y2 = cy + (float)((radius + 1.5f) * Math.Sin(a));
                    g.DrawLine(toothPen, x1, y1, x2, y2);
                }
            }

            // Main metallic body
            RectangleF bodyRect = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);
            using (LinearGradientBrush metalBrush = new LinearGradientBrush(
                bodyRect,
                Color.FromArgb(50, 62, 85),
                Color.FromArgb(16, 20, 30),
                135f))
            using (Pen rimPen = new Pen(accentColor, isSelected ? 2.4f : 1.5f))
            {
                g.FillEllipse(metalBrush, bodyRect);
                g.DrawEllipse(rimPen, bodyRect);
            }

            // Inner brushed cap
            float innerR = radius * 0.72f;
            RectangleF innerRect = new RectangleF(cx - innerR, cy - innerR, innerR * 2, innerR * 2);
            using (LinearGradientBrush capBrush = new LinearGradientBrush(
                innerRect,
                Color.FromArgb(24, 30, 44),
                Color.FromArgb(38, 48, 68),
                45f))
            using (Pen innerPen = new Pen(Color.FromArgb(120, accentColor), 1f))
            {
                g.FillEllipse(capBrush, innerRect);
                g.DrawEllipse(innerPen, innerRect);
            }
        }
    }
}
