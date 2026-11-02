using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float scaleX = bounds.Width / 1024f;
            float scaleY = bounds.Height / 1024f;
            g.TranslateTransform(bounds.Left, bounds.Top);
            g.ScaleTransform(scaleX, scaleY);

            // 1. Outer Chamfered Cyberpunk Keycap / Chassis Shield
            float margin = compactIconMode ? 16f : 32f;
            RectangleF outerFrame = new RectangleF(margin, margin, 1024f - margin * 2f, 1024f - margin * 2f);
            using (GraphicsPath shieldPath = CyberTheme.CreateChamferedRect(outerFrame, compactIconMode ? 120f : 96f))
            using (LinearGradientBrush bgBr = new LinearGradientBrush(
                outerFrame,
                Color.FromArgb(10, 18, 34),
                Color.FromArgb(4, 7, 14),
                135f))
            {
                g.FillPath(bgBr, shieldPath);

                if (!compactIconMode)
                {
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

                    for (int i = 3; i >= 1; i--)
                    {
                        using (Pen glowPen = new Pen(Color.FromArgb(32 * (4 - i), AcCyanBright), i * 6f))
                        {
                            g.DrawPath(glowPen, shieldPath);
                        }
                    }
                }

                using (Pen rimPen = new Pen(AcCyanBright, compactIconMode ? 28f : 6f))
                {
                    g.DrawPath(rimPen, shieldPath);
                }
            }

            if (compactIconMode)
            {
                // Favicon / Window Icon Mode:
                // Super bold keycap inner bevel + stylized bottom key row + large high-contrast AC Monogram
                RectangleF keycapDish = new RectangleF(64f, 64f, 896f, 896f);
                using (GraphicsPath dishPath = CyberTheme.CreateRoundedRect(keycapDish, 90f))
                using (Pen dishPen = new Pen(Color.FromArgb(150, CyberTheme.NeonMagenta), 14f))
                {
                    g.DrawPath(dishPen, dishPath);
                }

                // Bottom mini-keypad row indicator (4 neon keys + 1 yellow rotary knob dot)
                float ky = 735f;
                float kh = 145f;
                float kw = 145f;
                float kxStart = 104f;
                float kGap = 24f;
                for (int i = 0; i < 4; i++)
                {
                    RectangleF kr = new RectangleF(kxStart + i * (kw + kGap), ky, kw, kh);
                    using (GraphicsPath kp = CyberTheme.CreateRoundedRect(kr, 22f))
                    using (SolidBrush kFill = new SolidBrush(i == 0 || i == 3 ? Color.FromArgb(0, 110, 145) : Color.FromArgb(24, 42, 70)))
                    using (Pen kPen = new Pen(AcCyanBright, 12f))
                    {
                        g.FillPath(kFill, kp);
                        g.DrawPath(kPen, kp);
                    }
                }
                // Rotary knob indicator on bottom-right of favicon
                float knobCx = 852f;
                float knobCy = ky + kh * 0.5f;
                float knobR = 72f;
                using (SolidBrush knobFill = new SolidBrush(Color.FromArgb(45, 25, 65)))
                using (Pen knobPen = new Pen(CyberTheme.CyberYellow, 18f))
                {
                    g.FillEllipse(knobFill, knobCx - knobR, knobCy - knobR, knobR * 2f, knobR * 2f);
                    g.DrawEllipse(knobPen, knobCx - knobR, knobCy - knobR, knobR * 2f, knobR * 2f);
                }

                // Bold AC Monogram filling the upper/center area
                Bitmap monoBmp = GetMonogramOnly();
                if (monoBmp != null)
                {
                    RectangleF monoDest = new RectangleF(82f, 108f, 860f, 565f);
                    g.DrawImage(monoBmp, monoDest);
                }
            }
            else
            {
                // Inner chamfered bevel
                RectangleF innerFrame = new RectangleF(62f, 62f, 900f, 900f);
                using (GraphicsPath innerPath = CyberTheme.CreateChamferedRect(innerFrame, 74f))
                using (Pen innerPen = new Pen(Color.FromArgb(110, CyberTheme.NeonMagenta), 3f))
                {
                    g.DrawPath(innerPen, innerPath);
                }

                CyberTheme.DrawHexScrew(g, 106f, 106f, 15f);
                CyberTheme.DrawHexScrew(g, 918f, 106f, 15f);
                CyberTheme.DrawHexScrew(g, 106f, 918f, 15f);
                CyberTheme.DrawHexScrew(g, 918f, 918f, 15f);

                // Full Master Application Logo Mode (1024x1024):
                // 1) Top / Center: Authentic "AC - ALAN CANTO" Brand Logo
                Bitmap fullBrand = GetFullBrandMark();
                if (fullBrand != null)
                {
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
            for (int i = 3; i >= 1; i--)
            {
                RectangleF glowR = RectangleF.Inflate(boardRect, i * 4f, i * 4f);
                using (GraphicsPath gp = CyberTheme.CreateChamferedRect(glowR, 28f))
                using (Pen glowPen = new Pen(Color.FromArgb(26 * (4 - i), CyberTheme.NeonCyan), 3f))
                {
                    g.DrawPath(glowPen, gp);
                }
            }

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
            using (MemoryStream ms = new MemoryStream())
            {
                WriteMultiResolutionIconStream(ms, new int[] { 16, 24, 32, 48, 64 });
                ms.Position = 0;
                return new Icon(ms, 16, 16);
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
                using (FileStream fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
                {
                    WriteMultiResolutionIconStream(fs, new int[] { 256, 64, 48, 32, 24, 16 });
                }
            }
        }

        /// <summary>
        /// Writes a standard Windows .ICO stream using 32-bit BGRA DIB frames for sizes &lt; 256 (for 100% Win32/WinForms titlebar compatibility) and PNG for 256x256.
        /// </summary>
        private static void WriteMultiResolutionIconStream(Stream outStream, int[] sizes)
        {
            byte[][] frameData = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                using (Bitmap frameBmp = RenderMasterLogoBitmap(s, false, true))
                {
                    if (s >= 256)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            frameBmp.Save(ms, ImageFormat.Png);
                            frameData[i] = ms.ToArray();
                        }
                    }
                    else
                    {
                        frameData[i] = ConvertBitmapTo32BppIconDib(frameBmp);
                    }
                }
            }

            BinaryWriter bw = new BinaryWriter(outStream);
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)sizes.Length);

            int dataOffset = 6 + (16 * sizes.Length);
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                byte dimByte = (s >= 256) ? (byte)0 : (byte)s;
                int byteCount = frameData[i].Length;

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
                bw.Write(frameData[i]);
            }
            bw.Flush();
        }

        private static byte[] ConvertBitmapTo32BppIconDib(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            int xorStride = w * 4;
            int andStride = ((w + 31) / 32) * 4;
            int totalBytes = 40 + (xorStride * h) + (andStride * h);

            byte[] dib = new byte[totalBytes];
            using (MemoryStream ms = new MemoryStream(dib))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // BITMAPINFOHEADER (40 bytes)
                bw.Write((uint)40);
                bw.Write((int)w);
                bw.Write((int)(h * 2)); // Combined height of XOR + AND masks
                bw.Write((ushort)1);    // Planes
                bw.Write((ushort)32);   // BitCount
                bw.Write((uint)0);      // BI_RGB
                bw.Write((uint)(xorStride * h + andStride * h));
                bw.Write((int)0);
                bw.Write((int)0);
                bw.Write((uint)0);
                bw.Write((uint)0);

                // Bottom-up BGRA pixels
                for (int y = h - 1; y >= 0; y--)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = bmp.GetPixel(x, y);
                        bw.Write(c.B);
                        bw.Write(c.G);
                        bw.Write(c.R);
                        bw.Write(c.A);
                    }
                }

                // Bottom-up 1-bpp AND mask
                for (int y = h - 1; y >= 0; y--)
                {
                    int rowBytesWritten = 0;
                    byte curByte = 0;
                    int bitIdx = 7;
                    for (int x = 0; x < w; x++)
                    {
                        if (bmp.GetPixel(x, y).A < 16)
                        {
                            curByte |= (byte)(1 << bitIdx);
                        }
                        bitIdx--;
                        if (bitIdx < 0)
                        {
                            bw.Write(curByte);
                            rowBytesWritten++;
                            curByte = 0;
                            bitIdx = 7;
                        }
                    }
                    if (bitIdx != 7)
                    {
                        bw.Write(curByte);
                        rowBytesWritten++;
                    }
                    while (rowBytesWritten < andStride)
                    {
                        bw.Write((byte)0);
                        rowBytesWritten++;
                    }
                }
            }
            return dib;
        }
    }
}
