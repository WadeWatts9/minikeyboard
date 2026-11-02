using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;

namespace MiniKeyboardCyber
{
    public static class BrandLogoRenderer
    {
        public static readonly Color AcCyanBright = Color.FromArgb(0, 225, 245);
        public static readonly Color AcTeal = Color.FromArgb(0, 172, 188);
        public static readonly Color AcNavyDark = Color.FromArgb(16, 36, 68);

        private static Bitmap _cachedFullBrandMark;
        private static Bitmap _cachedMonogramOnly;

        private static Bitmap GetFullBrandMark()
        {
            if (_cachedFullBrandMark != null) return _cachedFullBrandMark;
            _cachedFullBrandMark = LoadEmbeddedOrDiskBitmap("ac_brand_mark.png", Path.Combine("assets", "ac_brand_mark.png"));
            return _cachedFullBrandMark;
        }

        private static Bitmap GetMonogramOnly()
        {
            if (_cachedMonogramOnly != null) return _cachedMonogramOnly;
            _cachedMonogramOnly = LoadEmbeddedOrDiskBitmap("ac_monogram_only.png", Path.Combine("assets", "ac_monogram_only.png"));
            return _cachedMonogramOnly;
        }

        private static Bitmap LoadEmbeddedOrDiskBitmap(string resourceName, string fallbackRelativePath)
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream stream = asm.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using (stream)
                    {
                        return new Bitmap(stream);
                    }
                }
            }
            catch
            {
            }

            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fallbackRelativePath);
                if (File.Exists(fullPath))
                {
                    return new Bitmap(fullPath);
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Draws the authentic "AC - ALAN CANTO" logo fused with the 12-key + 2-knob Cyberpunk Mini-Keyboard.
        /// </summary>
        public static void DrawAcKeyboardEmblem(Graphics g, RectangleF bounds, bool includeTypography, bool compactIconMode)
        {
            GraphicsState state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float scaleX = bounds.Width / 1024f;
            float scaleY = bounds.Height / 1024f;
            g.TranslateTransform(bounds.Left, bounds.Top);
            g.ScaleTransform(scaleX, scaleY);

            // 1. Outer Chamfered Cyberpunk Keycap / Chassis Shield
            RectangleF outerFrame = new RectangleF(32f, 32f, 960f, 960f);
            using (GraphicsPath shieldPath = CyberTheme.CreateChamferedRect(outerFrame, 96f))
            using (LinearGradientBrush bgBr = new LinearGradientBrush(
                outerFrame,
                Color.FromArgb(8, 13, 24),
                Color.FromArgb(4, 6, 12),
                135f))
            {
                g.FillPath(bgBr, shieldPath);

                // Tech grid inside shield
                g.SetClip(shieldPath);
                using (Pen gridPen = new Pen(Color.FromArgb(18, 0, 225, 255), 2f))
                {
                    for (int i = 64; i < 1024; i += 64)
                    {
                        g.DrawLine(gridPen, i, 0, i, 1024);
                        g.DrawLine(gridPen, 0, i, 1024, i);
                    }
                }
                g.ResetClip();

                // Outer Neon Glow & Border
                for (int i = 3; i >= 1; i--)
                {
                    using (Pen glowPen = new Pen(Color.FromArgb(32 * (4 - i), AcCyanBright), i * 6f))
                    {
                        g.DrawPath(glowPen, shieldPath);
                    }
                }
                using (Pen rimPen = new Pen(AcCyanBright, compactIconMode ? 12f : 6f))
                {
                    g.DrawPath(rimPen, shieldPath);
                }
            }

            // Inner chamfered bevel
            RectangleF innerFrame = new RectangleF(62f, 62f, 900f, 900f);
            using (GraphicsPath innerPath = CyberTheme.CreateChamferedRect(innerFrame, 74f))
            using (Pen innerPen = new Pen(Color.FromArgb(110, CyberTheme.NeonMagenta), compactIconMode ? 5f : 3f))
            {
                g.DrawPath(innerPen, innerPath);
            }

            if (!compactIconMode)
            {
                CyberTheme.DrawHexScrew(g, 106f, 106f, 15f);
                CyberTheme.DrawHexScrew(g, 918f, 106f, 15f);
                CyberTheme.DrawHexScrew(g, 106f, 918f, 15f);
                CyberTheme.DrawHexScrew(g, 918f, 918f, 15f);
            }

            if (compactIconMode)
            {
                // Compact Icon Mode (for .ICO, Window Titlebar & Header Badge):
                // Draw stylized mini-keyboard 3x4 + 2 knobs at bottom half and large AC Monogram at top/center
                DrawMiniKeyboardIllustration(g, new RectangleF(130f, 575f, 764f, 325f), true);

                Bitmap monoBmp = GetMonogramOnly();
                if (monoBmp != null)
                {
                    RectangleF monoDest = new RectangleF(112f, 105f, 800f, 485f);
                    g.DrawImage(monoBmp, monoDest);
                }
            }
            else
            {
                // Full Master Application Logo Mode (1024x1024):
                // 1) Top / Center: Authentic "AC - ALAN CANTO" Brand Logo
                Bitmap fullBrand = GetFullBrandMark();
                if (fullBrand != null)
                {
                    // Subtle ambient cyan glow behind AC logo
                    using (GraphicsPath glowEllipse = new GraphicsPath())
                    {
                        glowEllipse.AddEllipse(200f, 60f, 624f, 480f);
                        using (PathGradientBrush pgb = new PathGradientBrush(glowEllipse))
                        {
                            pgb.CenterColor = Color.FromArgb(42, 0, 225, 255);
                            pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 225, 255) };
                            g.FillPath(pgb, glowEllipse);
                        }
                    }

                    RectangleF brandDest = new RectangleF(172f, -45f, 680f, 680f);
                    g.DrawImage(fullBrand, brandDest);
                }

                // 2) Lower Hero Element: Stylized 12-Key + 2-Knob Cyberpunk Mini-Keyboard
                DrawMiniKeyboardIllustration(g, new RectangleF(142f, 565f, 740f, 285f), false);

                // 3) Bottom Product Title Banner
                if (includeTypography)
                {
                    using (Font subFont = new Font("Consolas", 26f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush cyanBr = new SolidBrush(CyberTheme.NeonCyan))
                    using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString("⚡ MINI KEYBOARD // CYBERPUNK STUDIO ⚡", subFont, cyanBr, new RectangleF(80f, 875f, 864f, 44f), sf);
                    }
                }
            }

            g.Restore(state);
        }

        private static void DrawMiniKeyboardIllustration(Graphics g, RectangleF boardRect, bool simplified)
        {
            // Neon underglow around the mini-keyboard chassis
            for (int i = 3; i >= 1; i--)
            {
                RectangleF glowR = RectangleF.Inflate(boardRect, i * 4f, i * 4f);
                using (GraphicsPath gp = CyberTheme.CreateChamferedRect(glowR, 28f))
                using (Pen glowPen = new Pen(Color.FromArgb(26 * (4 - i), CyberTheme.NeonCyan), 3f))
                {
                    g.DrawPath(glowPen, gp);
                }
            }

            // Keyboard Acrylic Chassis
            using (GraphicsPath boardPath = CyberTheme.CreateChamferedRect(boardRect, 26f))
            using (LinearGradientBrush boardFill = new LinearGradientBrush(
                boardRect,
                Color.FromArgb(18, 28, 46),
                Color.FromArgb(10, 15, 26),
                90f))
            using (Pen boardBorder = new Pen(AcCyanBright, simplified ? 5f : 3.5f))
            {
                g.FillPath(boardFill, boardPath);
                g.DrawPath(boardBorder, boardPath);
            }

            if (!simplified)
            {
                CyberTheme.DrawHexScrew(g, boardRect.Left + 18f, boardRect.Top + 18f, 7f);
                CyberTheme.DrawHexScrew(g, boardRect.Right - 18f, boardRect.Top + 18f, 7f);
                CyberTheme.DrawHexScrew(g, boardRect.Left + 18f, boardRect.Bottom - 18f, 7f);
                CyberTheme.DrawHexScrew(g, boardRect.Right - 18f, boardRect.Bottom - 18f, 7f);
            }

            float padX = simplified ? 24f : 32f;
            float padY = simplified ? 22f : 26f;
            RectangleF inner = new RectangleF(
                boardRect.Left + padX,
                boardRect.Top + padY,
                boardRect.Width - padX * 2f,
                boardRect.Height - padY * 2f);

            // Left 66%: 3 rows x 4 columns of mechanical keys (12 keys)
            float keysW = inner.Width * 0.65f;
            float knobsW = inner.Width - keysW - 22f;

            int cols = 4;
            int rows = 3;
            float gap = simplified ? 10f : 12f;
            float kw = (keysW - gap * (cols - 1)) / cols;
            float kh = (inner.Height - gap * (rows - 1)) / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int keyNum = r * cols + c + 1;
                    RectangleF kr = new RectangleF(
                        inner.Left + c * (kw + gap),
                        inner.Top + r * (kh + gap),
                        kw,
                        kh);

                    bool highlightKey = (keyNum == 1 || keyNum == 4 || keyNum == 8 || keyNum == 9);
                    Color keyAccent = highlightKey ? AcCyanBright : Color.FromArgb(140, 0, 200, 235);

                    using (GraphicsPath kp = CyberTheme.CreateRoundedRect(kr, 8f))
                    using (LinearGradientBrush kFill = new LinearGradientBrush(
                        kr,
                        highlightKey ? Color.FromArgb(0, 75, 105) : Color.FromArgb(22, 34, 56),
                        Color.FromArgb(12, 20, 34),
                        90f))
                    using (Pen kPen = new Pen(keyAccent, simplified ? 3f : 2.2f))
                    {
                        g.FillPath(kFill, kp);
                        g.DrawPath(kPen, kp);
                    }

                    if (!simplified)
                    {
                        RectangleF capTop = RectangleF.Inflate(kr, -7f, -7f);
                        using (GraphicsPath cp = CyberTheme.CreateRoundedRect(capTop, 5f))
                        using (Pen capPen = new Pen(Color.FromArgb(55, 255, 255, 255), 1.2f))
                        {
                            g.DrawPath(capPen, cp);
                        }

                        using (Font kf = new Font("Consolas", 13f, FontStyle.Bold, GraphicsUnit.Pixel))
                        using (SolidBrush kb = new SolidBrush(highlightKey ? CyberTheme.CyberYellow : Color.FromArgb(200, 235, 255)))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(string.Format("{0:00}", keyNum), kf, kb, kr, sf);
                        }
                    }
                }
            }

            // Right 34%: 2 Rotary Encoders (Knob 1 & Knob 2)
            float knobsLeft = inner.Left + keysW + 22f;
            float knobCx = knobsLeft + knobsW * 0.5f;
            float knobR = Math.Min(knobsW * 0.38f, inner.Height * 0.21f);

            float knob1Cy = inner.Top + inner.Height * 0.26f;
            float knob2Cy = inner.Top + inner.Height * 0.74f;

            CyberTheme.DrawRotaryEncoderDial(g, knobCx, knob1Cy, knobR, CyberTheme.NeonMagenta, true);
            CyberTheme.DrawRotaryEncoderDial(g, knobCx, knob2Cy, knobR, CyberTheme.CyberYellow, true);

            if (!simplified)
            {
                using (Font nf = new Font("Consolas", 13f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (SolidBrush wBr = new SolidBrush(Color.White))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("K1", nf, wBr, new RectangleF(knobCx - knobR, knob1Cy - knobR, knobR * 2f, knobR * 2f), sf);
                    g.DrawString("K2", nf, wBr, new RectangleF(knobCx - knobR, knob2Cy - knobR, knobR * 2f, knobR * 2f), sf);
                }
            }
        }

        public static Bitmap RenderMasterLogoBitmap(int size = 1024, bool includeTypography = true, bool compactIconMode = false)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                DrawAcKeyboardEmblem(g, new RectangleF(0, 0, size, size), includeTypography, compactIconMode);
            }
            return bmp;
        }

        public static Icon CreateRuntimeWindowIcon()
        {
            using (Bitmap bmp = RenderMasterLogoBitmap(64, false, true))
            {
                IntPtr hIcon = bmp.GetHicon();
                Icon tempIcon = Icon.FromHandle(hIcon);
                return (Icon)tempIcon.Clone();
            }
        }

        public static void SaveApplicationLogoAssets(string logoPngPath, string icoPath)
        {
            string pngDir = Path.GetDirectoryName(logoPngPath);
            if (!string.IsNullOrEmpty(pngDir)) Directory.CreateDirectory(pngDir);

            using (Bitmap masterBmp = RenderMasterLogoBitmap(1024, true, false))
            {
                masterBmp.Save(logoPngPath, ImageFormat.Png);
            }

            if (!string.IsNullOrEmpty(icoPath))
            {
                string icoDir = Path.GetDirectoryName(icoPath);
                if (!string.IsNullOrEmpty(icoDir)) Directory.CreateDirectory(icoDir);
                WriteMultiResolutionIconFile(icoPath, new int[] { 256, 64, 48, 32, 16 });
            }
        }

        private static void WriteMultiResolutionIconFile(string icoPath, int[] sizes)
        {
            byte[][] pngFrames = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
            {
                using (Bitmap frameBmp = RenderMasterLogoBitmap(sizes[i], false, true))
                using (MemoryStream ms = new MemoryStream())
                {
                    frameBmp.Save(ms, ImageFormat.Png);
                    pngFrames[i] = ms.ToArray();
                }
            }

            using (FileStream fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0);
                bw.Write((ushort)1);
                bw.Write((ushort)sizes.Length);

                int dataOffset = 6 + (16 * sizes.Length);
                for (int i = 0; i < sizes.Length; i++)
                {
                    int s = sizes[i];
                    byte dimByte = (s >= 256) ? (byte)0 : (byte)s;
                    int byteCount = pngFrames[i].Length;

                    bw.Write(dimByte);
                    bw.Write(dimByte);
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((ushort)1);
                    bw.Write((ushort)32);
                    bw.Write((uint)byteCount);
                    bw.Write((uint)dataOffset);

                    dataOffset += byteCount;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngFrames[i]);
                }
            }
        }
    }
}
